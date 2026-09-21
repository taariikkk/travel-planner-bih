using TravelPlanner.Application.DTOs;

namespace TravelPlanner.Application.Interfaces;

public interface IDestinationSearchRepository
{
    Task<IReadOnlyList<DestinationSearchItemResponse>> SearchAsync(
        string query, string? type, string? region, string language, int limit,
        CancellationToken cancellationToken);

    Task<DestinationSearchFiltersResponse> GetFiltersAsync(CancellationToken cancellationToken);
}
