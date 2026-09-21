using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TravelPlanner.Application.DTOs;
using TravelPlanner.Application.Interfaces;

namespace TravelPlanner.Infrastructure.Providers;

public sealed class MapboxDirectionsProvider(IHttpClientFactory httpClientFactory, IOptions<MapboxOptions> options) : IRoutingProvider
{
    public const string ClientName = "MapboxDirections";

    public async Task<RouteResult> GetRouteAsync(
        IReadOnlyList<RouteCoordinate> waypoints,
        RouteTravelMode travelMode,
        CancellationToken cancellationToken)
    {
        if (waypoints.Count is < 2 or > 25)
            return RouteResult.Failure("A route requires between 2 and 25 waypoints.");
        if (waypoints.Any(waypoint => !IsValid(waypoint)))
            return RouteResult.Failure("Route coordinates are invalid.");
        if (string.IsNullOrWhiteSpace(options.Value.AccessToken))
            return RouteResult.Failure("Mapbox routing is not configured.");

        var profile = travelMode switch
        {
            RouteTravelMode.Driving => "driving",
            RouteTravelMode.Walking => "walking",
            RouteTravelMode.Cycling => "cycling",
            _ => null
        };
        if (profile is null) return RouteResult.Failure("The routing profile is not supported.");

        var coordinates = string.Join(';', waypoints.Select(waypoint => string.Create(
            CultureInfo.InvariantCulture, $"{waypoint.Longitude},{waypoint.Latitude}")));
        var token = Uri.EscapeDataString(options.Value.AccessToken.Trim());
        var requestUri = $"directions/v5/mapbox/{profile}/{coordinates}?alternatives=false&geometries=geojson&overview=full&steps=false&access_token={token}";

        try
        {
            using var response = await httpClientFactory.CreateClient(ClientName).GetAsync(requestUri, cancellationToken);
            if (response.StatusCode is HttpStatusCode.UnprocessableEntity or HttpStatusCode.NotFound)
                return RouteResult.Failure("Mapbox could not calculate a route for the supplied waypoints.");
            if (!response.IsSuccessStatusCode)
                return RouteResult.Failure("Mapbox routing is currently unavailable.");

            using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            var root = document.RootElement;
            if (!root.TryGetProperty("code", out var code) || code.GetString() != "Ok"
                || !root.TryGetProperty("routes", out var routes) || routes.GetArrayLength() == 0)
                return RouteResult.Failure("Mapbox could not calculate a route for the supplied waypoints.");

            var route = routes[0];
            var geometry = route.GetProperty("geometry");
            if (geometry.GetProperty("type").GetString() != "LineString")
                return RouteResult.Failure("Mapbox returned an unsupported route geometry.");
            var points = geometry.GetProperty("coordinates").EnumerateArray()
                .Select(point => new RouteCoordinate(point[1].GetDouble(), point[0].GetDouble()))
                .ToArray();
            if (points.Length < 2)
                return RouteResult.Failure("Mapbox returned an empty route geometry.");

            return RouteResult.Success(new RouteData(
                route.GetProperty("distance").GetDouble(),
                route.GetProperty("duration").GetDouble(),
                points));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return RouteResult.Failure("Mapbox routing request timed out.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or InvalidOperationException
            or KeyNotFoundException or IndexOutOfRangeException)
        {
            return RouteResult.Failure("Mapbox routing returned an invalid response.");
        }
    }

    private static bool IsValid(RouteCoordinate coordinate) =>
        double.IsFinite(coordinate.Latitude) && coordinate.Latitude is >= -90 and <= 90
        && double.IsFinite(coordinate.Longitude) && coordinate.Longitude is >= -180 and <= 180;
}
