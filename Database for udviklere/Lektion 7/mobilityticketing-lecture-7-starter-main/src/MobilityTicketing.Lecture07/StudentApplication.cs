using System.Text.Json;
using Microsoft.EntityFrameworkCore;
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

    public async Task<List<RouteOverviewDto>> ReadOverviewAsync(TimeWindow window, CancellationToken ct = default)
    {
        window.Validate();
        await using var db = CreateContext();
        return await Queries.LiveRouteOverviewQuery.ReadAsync(db, window, ct);
    }

    public Task<CancelResult> CancelTripAsync(string tripId, CancellationToken ct = default)
    {
        DatabaseSession.RequireText(tripId, nameof(tripId));
        return DatabaseSession.WriteAsync(Connection, "trip:" + tripId, async db =>
        {
            var trip = await DatabaseSession.LockTripAsync(db, tripId, ct);
            if (trip is null) return CancelResult.NotFound;
            if (trip.Status == "Cancelled") return CancelResult.AlreadyCancelled;
            trip.Status = "Cancelled";
            return CancelResult.Cancelled;
        }, ct);
    }

    public Task<RenameResult> RenameRouteAsync(string routeId, string name, CancellationToken ct = default)
    {
        DatabaseSession.RequireText(routeId, nameof(routeId));
        DatabaseSession.RequireText(name, nameof(name));
        return DatabaseSession.WriteAsync(Connection, "route:" + routeId, async db =>
        {
            var route = await db.Routes.FindAsync([routeId], ct);
            if (route is null) return RenameResult.NotFound;
            route.Name = name.Trim();
            return RenameResult.Renamed;
        }, ct);
    }

    public Task<BookingResult> BookTripAsync(string tripId, string requestId, CancellationToken ct = default)
    {
        DatabaseSession.RequireText(tripId, nameof(tripId));
        DatabaseSession.RequireText(requestId, nameof(requestId));
        return DatabaseSession.WriteAsync(Connection, "booking:" + requestId, async db =>
        {
            // Resolve retries before checking the current trip: a receipt remains valid after cancellation.
            var existing = await db.Bookings.FindAsync([requestId], ct);
            if (existing is not null)
                return existing.TripId != tripId
                    ? new BookingResult(BookingStatus.RequestConflict)
                    : new BookingResult(BookingStatus.AlreadyBooked,
                        new BookingReceipt(existing.RequestId, existing.TripId, existing.BookedAtUtc));

            var trip = await DatabaseSession.LockTripAsync(db, tripId, ct);
            if (trip is null) return new BookingResult(BookingStatus.NotFound);
            if (trip.Status == "Cancelled") return new BookingResult(BookingStatus.Cancelled);
            var now = Clock.GetUtcNow().UtcDateTime;
            now = new DateTime(now.Ticks - now.Ticks % 10, DateTimeKind.Utc);
            db.Bookings.Add(new Booking { RequestId = requestId, TripId = tripId, BookedAtUtc = now });
            return new BookingResult(BookingStatus.Booked, new BookingReceipt(requestId, tripId, now));
        }, ct);
    }

    public Task<PublishResult> PublishSnapshotAsync(string snapshotId, TimeWindow window,
        CancellationToken ct = default)
    {
        DatabaseSession.RequireText(snapshotId, nameof(snapshotId));
        window.Validate();
        return DatabaseSession.WriteAsync(Connection, "snapshot:" + snapshotId, async db =>
        {
            var existing = await db.Set<SnapshotDocument>().FindAsync([snapshotId], ct);
            if (existing is not null)
            {
                if (existing.FromUtc != window.FromUtc || existing.ToUtc != window.ToUtc)
                    throw new SnapshotConflictException(snapshotId);
                return PublishResult.AlreadyPublished;
            }

            var routes = await Queries.LiveRouteOverviewQuery.ReadAsync(db, window, ct);
            var now = Clock.GetUtcNow().UtcDateTime;
            now = new DateTime(now.Ticks - now.Ticks % 10, DateTimeKind.Utc);
            var snapshot = new PublishedSnapshot(snapshotId, window, now, routes);
            db.Set<SnapshotDocument>().Add(new SnapshotDocument
            {
                Id = snapshotId,
                FromUtc = window.FromUtc,
                ToUtc = window.ToUtc,
                Payload = JsonSerializer.Serialize(snapshot)
            });
            return PublishResult.Published;
        }, ct, Hooks.BeforePublishCommit);
    }

    public async Task<PublishedSnapshot> ReadSnapshotAsync(string snapshotId, CancellationToken ct = default)
    {
        DatabaseSession.RequireText(snapshotId, nameof(snapshotId));
        await using var db = CreateContext();
        var payload = await db.Set<SnapshotDocument>().AsNoTracking()
            .Where(snapshot => snapshot.Id == snapshotId)
            .Select(snapshot => snapshot.Payload).SingleOrDefaultAsync(ct);
        if (payload is null) throw new SnapshotNotFoundException(snapshotId);
        return JsonSerializer.Deserialize<PublishedSnapshot>(payload)!;
    }

    public LectureDbContext CreateContext() => LectureDbContext.Create(Connection, measurement: Measurement);
}
