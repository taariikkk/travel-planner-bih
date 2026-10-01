using Microsoft.EntityFrameworkCore;
using TravelPlanner.Api.Infrastructure.Persistence;
using TravelPlanner.Application.DTOs;
using TravelPlanner.Application.Interfaces;

namespace TravelPlanner.Infrastructure.Repositories;

public sealed class DestinationMapRepository(ApplicationDbContext context) : IDestinationMapRepository
{
    public async Task<IReadOnlyList<DestinationMapPlaceResponse>?> GetPlacesBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var destination = await context.Destinations.AsNoTracking()
            .Where(destination => destination.Slug == slug)
            .Select(destination => new { destination.Id, destination.Latitude, destination.Longitude })
            .SingleOrDefaultAsync(cancellationToken);
        if (destination is null) return null;

        var hasCoordinates = destination.Latitude is not null && destination.Longitude is not null;
        var placesQuery = PlaceReadModel.Query(context, destination.Id, destination.Latitude, destination.Longitude,
            validCoordinatesOnly: true);
        var places = await (hasCoordinates
                ? placesQuery.OrderBy(place => place.DistanceMeters).ThenBy(place => place.Name).ThenBy(place => place.Id)
                : placesQuery.OrderBy(place => place.Name).ThenBy(place => place.Id))
            .ToArrayAsync(cancellationToken);

        return places.Select(place => place.ToMapResponse(hasCoordinates)).ToArray();
    }
}
