/*using MobilityTicketing.Lecture07.Contracts;
using MobilityTicketing.Lecture07.Data;

public class PublishedSnapshot
{

     public PublishedSnapshot(string id, TimeWindow window, DateTime publishedAtUtc, List<RouteOverviewDto> routes)
{
    Id = id;
    RouteId = routes.First().RouteId;
    RouteName = routes.First().RouteName;
    DepartureTime = window.FromUtc;
    ArrivalTime = window.ToUtc;
    Status = "Scheduled";
    NumberOfStops = routes.First().ScheduledTripCount;
}
    public string Id { get; set; } = null!;
    public string RouteId { get; set; } = null!;
    public string RouteName { get; set; } = null!;
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public string Status { get; set; } = null!;
    public int NumberOfStops { get; set; }
}*/