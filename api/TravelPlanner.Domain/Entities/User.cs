namespace TravelPlanner.Api.Domain.Entities;

public sealed class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public List<string> Preferences { get; set; } = [];

    public List<Trip> Trips { get; set; } = [];
}
