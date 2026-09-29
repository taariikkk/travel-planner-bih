using TravelPlanner.Application.DTOs;
namespace TravelPlanner.Application.Interfaces;

public interface IPlacesImportRepository
{
    Task<PlacesImportTarget?> GetTargetAsync(string slug, CancellationToken cancellationToken);
    Task<PlacesLeaseResult> TryAcquireAsync(Guid destinationId, string signature, DateTimeOffset now,
        TimeSpan ttl, CancellationToken cancellationToken);
    Task<bool> CompleteAsync(Guid destinationId, PlacesImportLease lease, PlacesData data,
        DateTimeOffset now, CancellationToken cancellationToken);
    Task FailAsync(Guid destinationId, PlacesImportLease lease, DateTimeOffset nextAttemptAt, CancellationToken cancellationToken);
}
