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
    public Task<List<RouteOverviewDto>> ReadOverviewAsync(TimeWindow window, CancellationToken ct = default) =>
        LectureDbContext.WhereAsync<RouteOverviewDto>(Connection, window, ct).ToListAsync(ct);
    public Task<CancelResult> CancelTripAsync(string tripId, CancellationToken ct = default) =>
        LectureDbContext.DeleteAsync<CancelResult>(Connection, tripId, ct);
    public Task<RenameResult> RenameRouteAsync(string routeId, string name, CancellationToken ct = default) =>
        LectureDbContext.UpdateAsync<RenameResult>(Connection, routeId, name, ct);
    public Task<BookingResult> BookTripAsync(string tripId, string requestId, CancellationToken ct = default) =>
        LectureDbContext.InsertAsync<BookingResult>(Connection, tripId, requestId, ct);
    public Task<PublishResult> PublishSnapshotAsync(string snapshotId, TimeWindow window, CancellationToken ct = default) =>
        LectureDbContext.InsertAsync<PublishResult>(Connection, snapshotId, window, ct);
    public Task<PublishedSnapshot> ReadSnapshotAsync(string snapshotId, CancellationToken ct = default) =>
        LectureDbContext.WhereAsync<PublishedSnapshot>(Connection, snapshotId, ct).FirstOrDefaultAsync(ct);
}
