namespace TravelPlanner.Infrastructure.Providers;

public sealed class WikipediaOptions
{
    public const string SectionName = "Wikipedia";

    public string ApiUrlTemplate { get; init; } = "https://{0}.wikipedia.org/api/rest_v1/";
    public int MinimumSummaryLength { get; init; } = 120;
}
