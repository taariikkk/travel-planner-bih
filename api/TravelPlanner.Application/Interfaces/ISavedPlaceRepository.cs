using TravelPlanner.Api.Domain.Entities;

namespace TravelPlanner.Application.Interfaces;

public interface ISavedPlaceRepository
{
    Task<SavedPlace> AddAsync(SavedPlace savedPlace, CancellationToken cancellationToken);
    Task<bool> RemoveAsync(Guid id, Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<SavedPlace>> ListForUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> ExistsForDestinationAsync(Guid userId, Guid destinationId, CancellationToken cancellationToken);
    Task<bool> ExistsForPlaceAsync(Guid userId, Guid placeId, CancellationToken cancellationToken);
}
