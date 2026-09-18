namespace TravelPlanner.Api.Domain.Entities;

public sealed class ItineraryItem
{
    public Guid Id { get; set; }
    public Guid TripDayId { get; set; }
    public Guid PlaceId { get; set; }
    public int Order { get; set; }
    public string? Notes { get; set; }
    public TripDay TripDay { get; set; } = null!;
    public Place Place { get; set; } = null!;
}
