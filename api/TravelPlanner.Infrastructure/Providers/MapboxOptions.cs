namespace TravelPlanner.Infrastructure.Providers;

public sealed class MapboxOptions
{
    public const string SectionName = "Mapbox";

    public string BaseUrl { get; init; } = "https://api.mapbox.com/";
    public string? AccessToken { get; init; }
    public int TimeoutSeconds { get; init; } = 10;
}
