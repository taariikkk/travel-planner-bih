using TravelPlanner.Api.Domain.Entities;

namespace TravelPlanner.Application.Interfaces;

public interface IDestinationImportRepository
{
    Task<Destination?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken);
    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken);
    Task<Destination> AddAsync(Destination destination, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
