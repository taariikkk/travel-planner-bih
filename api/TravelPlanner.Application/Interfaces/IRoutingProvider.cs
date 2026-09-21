using TravelPlanner.Application.DTOs;

namespace TravelPlanner.Application.Interfaces;

public interface IRoutingProvider
{
    Task<RouteResult> GetRouteAsync(
        IReadOnlyList<RouteCoordinate> waypoints,
        RouteTravelMode travelMode,
        CancellationToken cancellationToken);
}
