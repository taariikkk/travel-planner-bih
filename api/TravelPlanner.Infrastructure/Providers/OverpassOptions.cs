namespace TravelPlanner.Infrastructure.Providers;

public sealed class OverpassOptions
{
    public string PrimaryUrl { get; init; } = "https://overpass-api.de/api/interpreter";
    public string? MirrorUrl { get; init; } = "https://overpass.private.coffee/api/interpreter";
    public string UserAgent { get; init; } = "TravelPlannerBiH/1.0 (destination POI import)";
    public int QueryTimeoutSeconds { get; init; } = 10;
    public int HttpTimeoutSeconds { get; init; } = 12;
    public int MinImportIntervalSeconds { get; init; } = 30;
    public int DailyAttemptLimit { get; init; } = 90;
}
