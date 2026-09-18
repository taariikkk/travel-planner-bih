namespace TravelPlanner.Application.DTOs;

public sealed record UserResponse(Guid Id, string Email, string DisplayName, DateTimeOffset CreatedAt, List<string> Preferences);
