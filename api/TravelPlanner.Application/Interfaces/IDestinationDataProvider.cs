using TravelPlanner.Application.DTOs;

namespace TravelPlanner.Application.Interfaces;

public interface IDestinationDataProvider
{
    Task<DestinationDataSearchResult> SearchAsync(string searchText, CancellationToken cancellationToken);
    Task<DestinationDataResult> GetByQidAsync(string qid, CancellationToken cancellationToken);
}
