namespace TravelPlanner.Application.Interfaces;

public interface IPlacesImportDiagnostics
{
    void CleanupFailed(Guid destinationId, Guid leaseToken, string reason, Exception exception);
}
