namespace TravelPlanner.Application.DTOs;

public sealed record RecommendationRequest(string BudgetTier, int TravelDays, string Season, string Language);
