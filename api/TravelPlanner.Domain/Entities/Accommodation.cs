namespace TravelPlanner.Api.Domain.Entities;

public sealed class Accommodation
{
    public Guid Id { get; set; }
    public Guid DestinationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal PricePerNightKM { get; set; }
    public string ContactLink { get; set; } = string.Empty;
    public Destination Destination { get; set; } = null!;
}
