namespace TravelPlanner.Api.Domain.Entities;

public sealed class DestinationPlace
{
    public Guid DestinationId { get; set; }
    public Guid PlaceId { get; set; }
    public bool IsManual { get; set; }
    public Destination Destination { get; set; } = null!;
    public Place Place { get; set; } = null!;
}
