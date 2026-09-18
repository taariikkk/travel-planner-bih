namespace TravelPlanner.Application.DTOs;

public sealed record UpdateProfileRequest(string DisplayName, List<string> Preferences);
