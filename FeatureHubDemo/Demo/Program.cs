using FeatureHubSDK;

var danishTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");

if (args.Contains("--self-test"))
{
    if (new FeatureHubRepository().IsEnabled("TimelyGreetings"))
        throw new InvalidOperationException("Missing SDK flag must be disabled.");
    foreach (var (hour, minute, expected) in new[]
    {
        (4, 59, "Good Evening, World"), (5, 0, "Good Morning, World"),
        (11, 59, "Good Morning, World"), (12, 0, "Good Day, World"),
        (17, 59, "Good Day, World"), (18, 0, "Good Evening, World")
    })
    {
        var instant = new DateTimeOffset(2026, 1, 15, hour, minute, 0, TimeSpan.FromHours(1));
        if (Greeting(true, instant) != expected || Greeting(false, instant) != "Hello World"
            || Greeting(null, instant) != "Hello World")
            throw new InvalidOperationException($"Greeting failed at {hour:D2}:{minute:D2}.");
    }
    foreach (var month in new[] { 1, 7 })
    {
        var instant = new DateTimeOffset(2026, month, 15, month == 1 ? 4 : 3, 0, 0, TimeSpan.Zero);
        if (Greeting(true, instant) != "Good Morning, World")
            throw new InvalidOperationException("Danish time zone conversion failed.");
    }
    Console.WriteLine("PASS: boundaries, disabled/missing state, missing SDK flag, Danish winter/summer time.");
    return;
}

IClientContext? context = null;
var edgeUrl = Environment.GetEnvironmentVariable("FEATUREHUB_EDGE_URL");
var apiKey = Environment.GetEnvironmentVariable("FEATUREHUB_API_KEY");
if (!string.IsNullOrWhiteSpace(edgeUrl) && !string.IsNullOrWhiteSpace(apiKey))
{
    try
    {
        var config = new EdgeFeatureHubConfig(edgeUrl, apiKey);
        var initialization = config.Init();
        context = config.NewContext();
        await Task.WhenAll(initialization, context.Build()).WaitAsync(TimeSpan.FromSeconds(10));
    }
    catch (Exception)
    {
        Console.Error.WriteLine("FeatureHub initialization unavailable; using available state or Hello World.");
    }
}

try
{
    while (true)
    {
        Console.WriteLine(Greeting(context?.IsEnabled("TimelyGreetings"), DateTimeOffset.UtcNow));
        await Task.Delay(TimeSpan.FromSeconds(5));
    }
}
finally
{
    context?.EdgeService?.Close();
}

string Greeting(bool? enabled, DateTimeOffset instant)
{
    if (enabled != true) return "Hello World";
    var hour = TimeZoneInfo.ConvertTime(instant, danishTimeZone).Hour;
    return hour is >= 5 and < 12 ? "Good Morning, World"
        : hour is >= 12 and < 18 ? "Good Day, World" : "Good Evening, World";
}