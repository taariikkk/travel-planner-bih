namespace TravelPlanner.Application.DTOs;

public sealed record DestinationRecommendationResponse(
    Guid Id,
    string Name,
    string Region,
    string Description,
    string BestTimeToVisit,
    List<string> Tags,
    int Score,
    string Reason);
