namespace TravelPlanner.Application.DTOs;

public sealed record DestinationDetailsResponse(Guid Id, string Slug, string Name, string Region,
    string Description, string BestTimeToVisit, int SuggestedStayMinDays, int SuggestedStayMaxDays,
    IReadOnlyList<string> Tags, double Latitude, double Longitude, IReadOnlyList<PlaceResponse> Places,
    double? DistanceFromSarajevoKm = null, double? ElevationMeters = null, double? AverageTemperatureC = null);

public sealed record PlaceResponse(Guid Id, string Name, string Category, double Latitude, double Longitude);
