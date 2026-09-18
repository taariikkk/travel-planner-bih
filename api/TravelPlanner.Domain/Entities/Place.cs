using NetTopologySuite.Geometries;

namespace TravelPlanner.Api.Domain.Entities;

public sealed class Place
{
    public Guid Id { get; set; }
    public Guid DestinationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public Point Location { get; set; } = null!;
    public string Source { get; set; } = string.Empty;
    public Destination Destination { get; set; } = null!;
    public List<ItineraryItem> ItineraryItems { get; set; } = [];
}
