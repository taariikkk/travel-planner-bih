using TravelPlanner.Application.DTOs;
namespace TravelPlanner.Application.Interfaces;

public interface IPlacesImportService
{
    Task<PlacesImportResult> RefreshAsync(string slug, CancellationToken cancellationToken);
}
