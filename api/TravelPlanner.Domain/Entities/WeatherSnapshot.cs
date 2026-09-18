namespace TravelPlanner.Api.Domain.Entities;

public sealed class WeatherSnapshot
{
    public Guid Id { get; set; }
    public Guid DestinationId { get; set; }
    public DateTimeOffset FetchedAt { get; set; }
    public string RawData { get; set; } = string.Empty;
    public Destination Destination { get; set; } = null!;
}
