namespace TravelPlanner.Application.Services;

public sealed class PlacesImportOptions
{
    public const int DestinationLeaseSeconds = 120;
    public int RadiusMeters { get; init; } = 10_000;
    public int TtlHours { get; init; } = 168;
    public int FailureCooldownMinutes { get; init; } = 15;
    public int RefreshTimeoutSeconds { get; init; } = 35;
    public int CleanupTimeoutSeconds { get; init; } = 3;
    // Increment when the provider's category selection/mapping changes.
    public const string QueryVersion = "osm-bih-v1";
}
