namespace TravelPlanner.Application.DTOs;

public sealed record PlacesQuery(double Latitude, double Longitude, int RadiusMeters);
public sealed record PlaceData(string ExternalId, string Name, string Category, double Latitude, double Longitude,
    string? DescriptionBs = null, string? DescriptionEn = null, string? Description = null,
    string? DescriptionLanguage = null, string? Address = null, string? Cuisine = null,
    string? Website = null, string? Phone = null, string? Email = null);
public sealed record PlacesData(IReadOnlyList<PlaceData> Places, int SkippedCount = 0);
public sealed record PlacesImportTarget(Guid Id, double? Latitude, double? Longitude);
public sealed record PlacesImportLease(Guid Token, string Signature);

public sealed class PlacesProviderException(string message, DateTimeOffset? retryAt = null, bool deferred = false)
    : Exception(message)
{
    public DateTimeOffset? RetryAt { get; } = retryAt;
    public bool Deferred { get; } = deferred;
}
