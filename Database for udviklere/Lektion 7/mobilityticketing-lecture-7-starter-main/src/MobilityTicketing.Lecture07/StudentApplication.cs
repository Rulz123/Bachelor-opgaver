using System.Diagnostics.Eventing.Reader;
using System.Runtime.Intrinsics.X86;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using MobilityTicketing.Lecture07.Contracts;
using MobilityTicketing.Lecture07.Data;
using MobilityTicketing.Lecture07.Diagnostics;

namespace MobilityTicketing.Lecture07;

public sealed class StudentApplication(
    string connection,
    TimeProvider? clock = null,
    ApplicationHooks? hooks = null,
    RunMeasurement? measurement = null) : ILectureApplication
{
    public string Connection { get; } = connection;
    public TimeProvider Clock { get; } = clock ?? TimeProvider.System;
    public ApplicationHooks Hooks { get; } = hooks ?? new ApplicationHooks();
    public RunMeasurement Measurement { get; } = measurement ?? new RunMeasurement();

    private static bool IsTransientConcurrencyException(Exception ex)
    {
        for (var current = ex; current != null; current = current.InnerException)
        {
            var sqlState = current.GetType()
                .GetProperty("SqlState")?
                .GetValue(current) as string;

            if (sqlState is "40001" or "40P01")
                return true;
        }

        return false;
    }
    // Connect your command and query classes here.
    public async Task<List<RouteOverviewDto>> ReadOverviewAsync(
    TimeWindow window,
    CancellationToken ct = default)
    {
        if (window.FromUtc > window.ToUtc)
            throw new ArgumentException("The start of the time window must not be after its end.");

        using var db = CreateContext();

        return await db.Routes
            .OrderBy(r => r.Id)
            .Select(r => new RouteOverviewDto(
                r.Id,
                r.Name,
                r.Trips.Count(t =>
                    t.Status != "Cancelled" &&
                    t.DepartureUtc >= window.FromUtc &&
                    t.DepartureUtc < window.ToUtc),
                r.Trips
                    .Where(t =>
                        t.Status != "Cancelled" &&
                        t.DepartureUtc >= window.FromUtc &&
                        t.DepartureUtc < window.ToUtc)
                    .OrderBy(t => t.DepartureUtc)
                    .Select(t => (DateTime?)t.DepartureUtc)
                    .FirstOrDefault()
            ))
            .ToListAsync(ct);
    }

    public async Task<CancelResult> CancelTripAsync(
    string tripId,
    CancellationToken ct = default)
    {

        if (string.IsNullOrWhiteSpace(tripId))
            throw new ArgumentException("Trip ID cannot be empty.", nameof(tripId));

        const int maxAttempts = 5;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            await using var db = CreateContext();

            await using var transaction = await db.Database
                .BeginTransactionAsync(
                    System.Data.IsolationLevel.ReadCommitted, ct);

            try
            {
                var locked = await db.Trips
                    .Where(t => t.Id == tripId)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(
                            t => t.Status,
                            t => t.Status),
                        ct);

                if (locked == 0)
                    return CancelResult.NotFound;

                var trip = await db.Trips
                    .FirstAsync(t => t.Id == tripId, ct);

                if (trip.Status == "Cancelled")
                    return CancelResult.AlreadyCancelled;

                trip.Status = "Cancelled";

                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                return CancelResult.Cancelled;
            }
            catch (Exception ex) when (
                IsTransientConcurrencyException(ex) &&
                attempt < maxAttempts - 1)
            {
                await transaction.RollbackAsync(CancellationToken.None);

                await Task.Delay(
                    TimeSpan.FromMilliseconds(10 * (attempt + 1)),
                    ct);
            }
        }

        throw new InvalidOperationException(
            "Cancellation could not be completed after multiple attempts.");
    }

    public async Task<RenameResult> RenameRouteAsync(
    string routeId,
    string name,
    CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(routeId))
            throw new ArgumentException("Route ID cannot be empty.", nameof(routeId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Route name cannot be empty.", nameof(name));

        using var db = CreateContext();

        var route = await db.Routes.FindAsync([routeId], ct);

        if (route == null)
            return RenameResult.NotFound;

        route.Name = name.Trim();

        await db.SaveChangesAsync(ct);

        return RenameResult.Renamed;
    }

    public async Task<BookingResult> BookTripAsync(
    string tripId,
    string requestId,
    CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(requestId))
            throw new ArgumentException(
                "Request ID cannot be empty.",
                nameof(requestId));

        if (string.IsNullOrWhiteSpace(tripId))
            return new BookingResult(BookingStatus.NotFound);

        const int maxAttempts = 5;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            await using var db = CreateContext();

            await using var transaction = await db.Database
                .BeginTransactionAsync(
                    System.Data.IsolationLevel.ReadCommitted, ct);

            try
            {
                // Acquire a write lock on the trip row.
                var locked = await db.Trips
                    .Where(t => t.Id == tripId)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(
                            t => t.Status,
                            t => t.Status),
                        ct);

                if (locked == 0)
                    return new BookingResult(BookingStatus.NotFound);

                // Reload the trip after acquiring the lock.
                var trip = await db.Trips
                    .FirstAsync(t => t.Id == tripId, ct);

                // Idempotency check.
                var existing = await db.Bookings
                    .FindAsync(new object[] { requestId }, ct);

                if (existing != null)
                {
                    await transaction.CommitAsync(ct);

                    if (existing.TripId != tripId)
                        return new BookingResult(
                            BookingStatus.RequestConflict);

                    return new BookingResult(
                        BookingStatus.AlreadyBooked,
                        new BookingReceipt(
                            existing.RequestId,
                            existing.TripId,
                            existing.BookedAtUtc));
                }

                if (trip.Status == "Cancelled")
                    return new BookingResult(BookingStatus.Cancelled);

                var bookingTime = Clock.GetUtcNow().UtcDateTime;

                if (bookingTime > trip.DepartureUtc)
                    return new BookingResult(BookingStatus.NotFound);

                var booking = new Booking
                {
                    RequestId = requestId,
                    TripId = tripId,
                    BookedAtUtc = bookingTime
                };

                db.Bookings.Add(booking);

                await db.SaveChangesAsync(ct);

                // Use the persisted timestamp in the receipt.
                await db.Entry(booking).ReloadAsync(ct);

                var receipt = new BookingReceipt(
                    booking.RequestId,
                    booking.TripId,
                    booking.BookedAtUtc);

                await transaction.CommitAsync(ct);

                return new BookingResult(
                    BookingStatus.Booked,
                    receipt);
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(CancellationToken.None);

                // Check whether another request inserted the same ID.
                await using var retryDb = CreateContext();

                var existing = await retryDb.Bookings
                    .FindAsync(new object[] { requestId }, ct);

                if (existing != null)
                {
                    if (existing.TripId != tripId)
                        return new BookingResult(
                            BookingStatus.RequestConflict);

                    return new BookingResult(
                        BookingStatus.AlreadyBooked,
                        new BookingReceipt(
                            existing.RequestId,
                            existing.TripId,
                            existing.BookedAtUtc));
                }

                throw;
            }
            catch (Exception ex) when (
            IsTransientConcurrencyException(ex) &&
            attempt < maxAttempts - 1)
            {
                await transaction.RollbackAsync(CancellationToken.None);

                // Retry using a completely new DbContext.
                await Task.Delay(
                    TimeSpan.FromMilliseconds(10 * (attempt + 1)),
                    ct);
            }
        }

        throw new InvalidOperationException(
            "Booking could not be completed after multiple attempts.");
    }

    public async Task<PublishResult> PublishSnapshotAsync(string snapshotId, TimeWindow window,
        CancellationToken ct = default)
    {
        using var db = CreateContext();
        var routes = await ReadOverviewAsync(window, ct);

        var verification = db.PublishedSnapshots.Where(ps => ps.Id == snapshotId
        && ps.Window == window);

        var snapshot = new PublishedSnapshot(
            snapshotId,
            window,
            DateTime.UtcNow,
            routes);


        try
        {
            if (verification != null)
            {
                db.Add(snapshot);
                await db.SaveChangesAsync(ct);

                return await Task.FromResult(PublishResult.Published);
            }
            else
                return await Task.FromResult(PublishResult.AlreadyPublished);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ArgumentException($"Error happened during insert into DB for {snapshotId}");
        }

    }

    public async Task<PublishedSnapshot> ReadSnapshotAsync(string snapshotId, CancellationToken ct = default)
    {
        using var db = CreateContext();

        var snapshot = await db.FindAsync<PublishedSnapshot>(new object[] { snapshotId }, ct);

        if (snapshot != null)
            return snapshot;

        throw new ArgumentException($"There is no snapshot for {snapshotId}.");
    }

    public LectureDbContext CreateContext()
    {
        return LectureDbContext.Create(Connection, measurement: Measurement);
    }
}