using NetTopologySuite.Geometries;

namespace TravelPlanner.Api.Domain.Entities;

public sealed class Place
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public Point Location { get; set; } = null!;
    public string Source { get; set; } = string.Empty;
    public string? ExternalId { get; set; }
    public string? ManualKey { get; set; }
    public string? DescriptionBs { get; set; }
    public string? DescriptionEn { get; set; }
    public string? Description { get; set; }
    public string? DescriptionLanguage { get; set; }
    public string? Address { get; set; }
    public string? Cuisine { get; set; }
    public string? PriceLevel { get; set; }
    public string? Website { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? SourceUrl { get; set; }
    public DateTimeOffset? ImportedAt { get; set; }
    public DateTimeOffset? LastVerifiedAt { get; set; }
    public string? ImageUrl { get; set; }
    public string? ImageAuthor { get; set; }
    public string? ImageLicense { get; set; }
    public string? ImageSourceUrl { get; set; }
    public List<string> ManualOverrideFields { get; set; } = [];
    public List<DestinationPlace> DestinationLinks { get; set; } = [];
    public List<ItineraryItem> ItineraryItems { get; set; } = [];
    public List<SavedPlace> SavedPlaces { get; set; } = [];
}
