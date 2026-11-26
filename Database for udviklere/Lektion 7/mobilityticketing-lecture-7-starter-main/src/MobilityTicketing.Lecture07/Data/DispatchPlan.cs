public class DispatchPlan
{
    public string Id { get; set; }="";
    public string RouteId { get; set; }="";
    public string Status { get; set; }="";
    public DateTime PublishedAtUtc { get; set; }
    public DateTime DepartureUtc { get; set; }
    public DateTime ScheduledDepartureUtc { get; set; }
    public int NumberOfStops { get; set; }
}