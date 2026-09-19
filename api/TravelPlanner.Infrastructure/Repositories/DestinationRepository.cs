using Microsoft.EntityFrameworkCore;
using TravelPlanner.Api.Domain.Entities;
using TravelPlanner.Api.Infrastructure.Persistence;
using TravelPlanner.Application.Interfaces;

namespace TravelPlanner.Infrastructure.Repositories;

public sealed class DestinationRepository(ApplicationDbContext context) : IDestinationRepository
{
    public async Task<IReadOnlyList<Destination>> GetAllWithTranslationsAsync(CancellationToken cancellationToken) =>
        await context.Destinations
            .AsNoTracking()
            .Include(destination => destination.Translations)
            .ToListAsync(cancellationToken);
}
