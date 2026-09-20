namespace TravelPlanner.Infrastructure.Repositories;

public static class DestinationDistance
{
    // Geography uses the WGS84 spheroid and returns meters; the API exposes kilometers.
    public static FormattableString QueryKm(double latitude, double longitude, double originLatitude, double originLongitude) => $"""
        SELECT ST_Distance(
            ST_SetSRID(ST_MakePoint({longitude}, {latitude}), 4326)::geography,
            ST_SetSRID(ST_MakePoint({originLongitude}, {originLatitude}), 4326)::geography
        ) / 1000.0 AS "Value"
        """;
}
