namespace TravelPlanner.Api.Domain.Entities;

public sealed class TripDay
{
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public int DayNumber { get; set; }
    public DateOnly Date { get; set; }
    public Trip Trip { get; set; } = null!;
    public List<ItineraryItem> ItineraryItems { get; set; } = [];
}
