namespace TravelPlanner.Infrastructure.Providers;

public sealed class WikidataOptions
{
    public const string SectionName = "Wikidata";

    public string BaseUrl { get; init; } = "https://www.wikidata.org/";
    public string UserAgent { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 10;
    public Dictionary<string, string> ClassTypeMappings { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> RegionEntityIds { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
