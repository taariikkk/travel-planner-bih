namespace TravelPlanner.Api.Domain.Entities;

public sealed class Destination
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BestTimeToVisit { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public List<Place> Places { get; set; } = [];
    public List<Accommodation> Accommodations { get; set; } = [];
    public List<WeatherSnapshot> WeatherSnapshots { get; set; } = [];
}
