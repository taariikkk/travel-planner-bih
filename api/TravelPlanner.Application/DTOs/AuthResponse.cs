namespace TravelPlanner.Application.DTOs;

public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, UserResponse User);
