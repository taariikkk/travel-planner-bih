using Microsoft.Extensions.Logging;
using TravelPlanner.Application.Interfaces;

namespace TravelPlanner.Infrastructure.Providers;

public sealed class PlacesImportDiagnostics(ILogger<PlacesImportDiagnostics> logger) : IPlacesImportDiagnostics
{
    public void CleanupFailed(Guid destinationId, Guid leaseToken, string reason, Exception exception) =>
        logger.LogError(exception,
            "Places import cleanup failed for {DestinationId}, lease {LeaseToken}, after {Reason}",
            destinationId, leaseToken, reason);
}
