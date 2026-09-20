namespace TravelPlanner.Infrastructure.Providers;

public sealed class WikimediaOptions
{
    public const string SectionName = "Wikimedia";

    public string UserAgent { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 10;
    public double RequestsPerSecond { get; init; } = 2;
}
