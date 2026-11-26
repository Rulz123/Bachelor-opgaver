using Microsoft.EntityFrameworkCore;
using MobilityTicketing.Lecture07.Contracts;
using MobilityTicketing.Lecture07.Data;

namespace MobilityTicketing.Lecture07.Queries;

public static class LiveRouteOverviewQuery
{
    public static Task<List<RouteOverviewDto>> ReadAsync(LectureDbContext db, TimeWindow window, CancellationToken ct) =>
        db.Routes.OrderBy(r => r.Id).Select(r => new RouteOverviewDto(
            r.Id, r.Name,
            r.Trips.Count(t => t.Status == "Scheduled" && t.DepartureUtc >= window.FromUtc && t.DepartureUtc < window.ToUtc),
            r.Trips.Where(t => t.Status == "Scheduled" && t.DepartureUtc >= window.FromUtc && t.DepartureUtc < window.ToUtc)
                .Min(t => (DateTime?)t.DepartureUtc)
        )).ToListAsync(ct);
}
