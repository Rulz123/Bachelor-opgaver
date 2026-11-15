using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using MobilityTicketing.Lecture07.Contracts;
using MobilityTicketing.Lecture07.Data;
using MobilityTicketing.Lecture07.Diagnostics;

namespace MobilityTicketing.Lecture07;

public sealed class StudentApplication(string connection, TimeProvider? clock = null,
    ApplicationHooks? hooks = null, RunMeasurement? measurement = null) : ILectureApplication
{
    public string Connection { get; } = connection;
    public TimeProvider Clock { get; } = clock ?? TimeProvider.System;
    public ApplicationHooks Hooks { get; } = hooks ?? new();
    public RunMeasurement Measurement { get; } = measurement ?? new();
    public LectureDbContext CreateContext() => LectureDbContext.Create(Connection, measurement: Measurement);

    // Connect your command and query classes here.
    public Task<List<RouteOverviewDto>> ReadOverviewAsync(TimeWindow window, CancellationToken ct = default)
    {
        using var db = CreateContext();

        var query = Queryable.Select(
            db.Routes.AsQueryable(),
            (Route r) => new RouteOverviewDto(
                r.Id,
                r.Name,
                r.Trips.Count(t =>
                    t.DepartureUtc >= window.FromUtc &&
                    t.DepartureUtc <= window.ToUtc),
                window.FromUtc
            )).ToListAsync(ct);

        return query;
    }

    public async Task<CancelResult> CancelTripAsync(string tripId, CancellationToken ct = default)
    {
        using var db = CreateContext();

       
        var trip = await db.Trips
            .Where(t => t.Id == tripId)
            .Select(t => new { t.Status })
            .FirstOrDefaultAsync(ct);

        if (trip == null)
            return await Task.FromResult(CancelResult.NotFound);

        if (trip.Status == "Cancelled")
            return await Task.FromResult(CancelResult.AlreadyCancelled);

        await db.Trips
            .Where(t => t.Id == tripId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    t => t.Status,
                    "Cancelled"),
                ct);

        return await Task.FromResult(CancelResult.Cancelled);
    }
       
        //LectureDbContext.DeleteAsync<CancelResult>(Connection, tripId, ct);
        public async Task<RenameResult> RenameRouteAsync(string routeId, string name, CancellationToken ct = default)
        {
            using var db =  CreateContext();
            
            var route = db.Routes.Find(routeId);
            
            if (route == null)
                return await Task.FromResult(RenameResult.NotFound);
            else
            {
                route.Name = name;
                db.Routes.Update(route);
                return await Task.FromResult(RenameResult.Renamed);
            }
        }

        public async Task<BookingResult> BookTripAsync(string tripId, string requestId, CancellationToken ct = default)
        {
            using var db = CreateContext();

            BookingResult bookingResult;
            
            var trip = db.Trips.Find(tripId);
            var booking = new Booking();
            
            if (trip == null)
            {
                bookingResult = new BookingResult(BookingStatus.NotFound, null);
                return await Task.FromResult(bookingResult);
            }
            else
            {
                BookingReceipt receipt = new BookingReceipt(requestId, tripId, DateTime.UtcNow);
                bookingResult = new BookingResult(BookingStatus.Booked, receipt);
                booking.RequestId = requestId;
                booking.TripId = tripId;
                booking.BookedAtUtc = receipt.BookedAtUtc;
                db.Bookings.Add(booking);
                return await Task.FromResult(bookingResult);
            }
        }

        public async Task<PublishResult> PublishSnapshotAsync(string snapshotId, TimeWindow window,
            CancellationToken ct = default)
        {
            using var db = CreateContext();

            var routes = await ReadOverviewAsync(window, ct);

            var snapshot = new PublishedSnapshot(
                snapshotId,
                window,
                DateTime.UtcNow,
                routes);

            db.Set<PublishedSnapshot>().Add(snapshot);

            await db.SaveChangesAsync(ct);

            return PublishResult.Published;
        }

        public async Task<PublishedSnapshot> ReadSnapshotAsync(string snapshotId, CancellationToken ct = default)
        {
            using var db = CreateContext();

            return await db.Set<PublishedSnapshot>()
                .FirstAsync(s => s.Id == snapshotId, ct);
        }
}
