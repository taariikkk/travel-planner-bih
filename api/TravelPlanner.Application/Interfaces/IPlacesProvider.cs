using TravelPlanner.Application.DTOs;
namespace TravelPlanner.Application.Interfaces;

public interface IPlacesProvider
{
    Task<PlacesData> GetPlacesAsync(PlacesQuery query, CancellationToken cancellationToken);
}
