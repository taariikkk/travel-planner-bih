namespace TravelPlanner.Api.Domain.Entities;

public sealed class DestinationTranslation
{
    public Guid Id { get; set; }
    public Guid DestinationId { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BestTimeToVisit { get; set; } = string.Empty;
    public Destination Destination { get; set; } = null!;
}
