using TravelPlanner.Application.DTOs;

namespace TravelPlanner.Application.Interfaces;

public interface IDestinationDetailsRepository
{
    Task<DestinationDetailsResponse?> GetBySlugAsync(string slug, string language, CancellationToken cancellationToken);
}
