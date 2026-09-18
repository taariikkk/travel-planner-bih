namespace TravelPlanner.Application.DTOs;

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);
