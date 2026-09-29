using Microsoft.EntityFrameworkCore;
using TravelPlanner.Api.Infrastructure.Persistence;
using TravelPlanner.Application.DTOs;
using TravelPlanner.Application.Interfaces;

namespace TravelPlanner.Infrastructure.Repositories;

public sealed class DestinationMapRepository(ApplicationDbContext context) : IDestinationMapRepository
{
    public async Task<IReadOnlyList<DestinationMapPlaceResponse>?> GetPlacesBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var destinationId = await context.Destinations.AsNoTracking()
            .Where(destination => destination.Slug == slug)
            .Select(destination => (Guid?)destination.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (destinationId is null) return null;

        var places = await PlaceReadModel.Query(context, destinationId.Value)
            .OrderBy(place => place.Name).ThenBy(place => place.Id).ToArrayAsync(cancellationToken);

        return places.Select(place => place.ToMapResponse()).ToArray();
    }
}
