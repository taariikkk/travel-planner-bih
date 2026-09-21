using TravelPlanner.Application.DTOs;

namespace TravelPlanner.Application.Interfaces;

public interface IDestinationMapRepository
{
    Task<IReadOnlyList<DestinationMapPlaceResponse>?> GetPlacesBySlugAsync(string slug, CancellationToken cancellationToken);
}
