namespace TravelPlanner.Application.DTOs;

public enum PlacesImportStatus { Refreshed, Cached, Deferred, NotFound }

public sealed record PlacesImportResult(PlacesImportStatus Status, DateTimeOffset? RetryAt = null);
public sealed record PlacesRefreshResponse(string Status, DateTimeOffset? RetryAt);

public enum PlacesLeaseStatus { Acquired, Cached, Deferred }

public sealed record PlacesLeaseResult(PlacesLeaseStatus Status, PlacesImportLease? Lease = null,
    DateTimeOffset? RetryAt = null);
