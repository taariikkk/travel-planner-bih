namespace TravelPlanner.Application.DTOs;

public enum RouteTravelMode
{
    Driving,
    Walking,
    Cycling
}

public sealed record RouteCoordinate(double Latitude, double Longitude);

public sealed record RouteData(
    double DistanceMeters,
    double DurationSeconds,
    IReadOnlyList<RouteCoordinate> Geometry);

public sealed record RouteResult(bool IsSuccess, RouteData? Route, string? Error)
{
    public static RouteResult Success(RouteData route) => new(true, route, null);
    public static RouteResult Failure(string error) => new(false, null, error);
}
