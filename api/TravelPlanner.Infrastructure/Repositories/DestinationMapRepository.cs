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

        var places = await context.Places.FromSqlInterpolated($"""
            SELECT * FROM "Place"
            WHERE "DestinationId" = {destinationId.Value}
              AND NOT ST_IsEmpty("Location")
              AND ST_X("Location") BETWEEN -180 AND 180
              AND ST_Y("Location") BETWEEN -90 AND 90
            ORDER BY "Name", "Id"
            """).AsNoTracking().ToArrayAsync(cancellationToken);

        return places.Select(place => new DestinationMapPlaceResponse(
            place.Id, place.Name, place.Category, place.Location.Y, place.Location.X)).ToArray();
    }
}
