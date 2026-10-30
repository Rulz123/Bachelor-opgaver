using System.Globalization;
using FeatureHubSDK;

var danishTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");

if (args.Contains("--self-test"))
{
    if (CountryValue("DK") != "denmark" || CountryValue("dk") != "denmark"
        || CountryValue("SE") != "sweden" || CountryValue(null) != null)
        throw new InvalidOperationException("Country conversion failed.");
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
    Console.WriteLine("PASS: countries, boundaries, disabled/missing state, SDK fallback, Danish winter/summer time.");
    return;
}

var countryIndex = Array.IndexOf(args, "--country");
var country = CountryValue(countryIndex < 0 ? null : args.ElementAtOrDefault(countryIndex + 1)
    ?? throw new ArgumentException("--country requires a two-letter ISO country code."));
IClientContext? context = null;
var edgeUrl = Environment.GetEnvironmentVariable("FEATUREHUB_EDGE_URL");
var apiKey = Environment.GetEnvironmentVariable("FEATUREHUB_API_KEY");
if (!string.IsNullOrWhiteSpace(edgeUrl) && !string.IsNullOrWhiteSpace(apiKey))
{
    try
    {
        var config = new EdgeFeatureHubConfig(edgeUrl, apiKey);
        context = config.NewContext();
        if (country != null) context.Attr("country", country);
        var initialization = context.Build();
        await Task.WhenAll(initialization, context.EdgeService.Poll()).WaitAsync(TimeSpan.FromSeconds(10));
        if (context.Repository.Readyness != Readyness.Ready)
            throw new InvalidOperationException("FeatureHub did not provide initial state.");
        Console.Error.WriteLine($"FeatureHub ready (country={country ?? "not supplied"}).");
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

string? CountryValue(string? code)
{
    if (code is null) return null;
    if (code.Length != 2 || !code.All(char.IsAsciiLetter))
        throw new ArgumentException("Use a two-letter ISO country code, for example DK or SE.");
    return new RegionInfo(code.ToUpperInvariant()).EnglishName.ToLowerInvariant().Replace(' ', '_');
}