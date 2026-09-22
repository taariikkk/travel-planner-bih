namespace TravelPlanner.Application.Interfaces;

public interface IPlacesImportService
{
    Task RefreshAsync(string slug, CancellationToken cancellationToken);
}
