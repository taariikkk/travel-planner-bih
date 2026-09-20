namespace TravelPlanner.Infrastructure.Providers;

public sealed class WikimediaCommonsOptions
{
    public const string SectionName = "WikimediaCommons";

    public string BaseUrl { get; init; } = "https://commons.wikimedia.org/";
}
