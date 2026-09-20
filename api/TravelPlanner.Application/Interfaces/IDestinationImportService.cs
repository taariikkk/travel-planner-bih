using TravelPlanner.Application.DTOs;

namespace TravelPlanner.Application.Interfaces;

public interface IDestinationImportService
{
    Task<DestinationImportResult> ImportAsync(string qid, CancellationToken cancellationToken);
}
