using TravelPlanner.Application.DTOs;

namespace TravelPlanner.Application.Interfaces;

public interface IDestinationSearchService
{
    Task<DestinationSearchResponse> SearchAsync(
        string? query, string? type, string? region, string language,
        CancellationToken cancellationToken);
}
