namespace ArticleService;

public static class ArticleRouter
{
    private static readonly string[] Continents =
    [
        "Europe",
        "Asia",
        "Africa",
        "North America",
        "South America",
        "Australia/Oceania",
        "Antarctica"
    ];

    public static string GetDatabaseKey(bool isGlobal, string? continent)
    {
        if (isGlobal)
        {
            return "global";
        }

        if (string.IsNullOrWhiteSpace(continent) || !Continents.Contains(continent, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("A valid continent is required for a non-global article.", nameof(continent));
        }

        return continent.ToLowerInvariant() switch
        {
            "north america" => "north-america",
            "south america" => "south-america",
            "australia/oceania" => "australia-oceania",
            _ => continent.ToLowerInvariant()
        };
    }

    public static IReadOnlyList<string> DatabaseKeys =>
    ["europe", "asia", "africa", "north-america", "south-america", "australia-oceania", "antarctica", "global"];
}
