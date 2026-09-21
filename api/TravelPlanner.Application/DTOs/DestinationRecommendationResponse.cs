namespace TravelPlanner.Application.DTOs;

public sealed record DestinationRecommendationResponse(
    Guid Id,
    string Slug,
    string Name,
    string Region,
    string Description,
    string BestTimeToVisit,
    List<string> Tags,
    int Score,
    RecommendationReasonsResponse Reasons);

public sealed record RecommendationReasonsResponse(
    List<string> MatchingTags,
    bool MatchesSeason,
    bool MatchesBudget,
    bool MatchesDuration);
