using Microsoft.EntityFrameworkCore;
using TravelPlanner.Api.Domain.Entities;
using TravelPlanner.Api.Infrastructure.Persistence;
using TravelPlanner.Application.Interfaces;

namespace TravelPlanner.Infrastructure.Repositories;

public sealed class DestinationImportRepository(ApplicationDbContext context) : IDestinationImportRepository
{
    public Task<Destination?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken) =>
        context.Destinations.SingleOrDefaultAsync(destination => destination.ExternalId == externalId, cancellationToken);

    public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken) =>
        context.Destinations.AnyAsync(destination => destination.Slug == slug, cancellationToken);

    public async Task<Destination> AddAsync(Destination destination, CancellationToken cancellationToken)
    {
        context.Destinations.Add(destination);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return destination;
        }
        catch (DbUpdateException)
        {
            context.Entry(destination).State = EntityState.Detached;
            var existing = await GetByExternalIdAsync(destination.ExternalId!, cancellationToken);
            if (existing is not null)
                return existing;
            throw;
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
