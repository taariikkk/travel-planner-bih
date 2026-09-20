namespace TravelPlanner.Api.Domain.Entities;

public sealed class Destination
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "other";
    public string Source { get; set; } = "manual";
    public string? ExternalId { get; set; }
    public DateTimeOffset? ImportedAt { get; set; }
    public string Region { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? DescriptionSourceUrl { get; set; }
    public string? DescriptionLanguage { get; set; }
    public string BestTimeToVisit { get; set; } = string.Empty;
    public double? ElevationM { get; set; }
    public int? Population { get; set; }
    public string? ImageUrl { get; set; }
    public string? ImageAuthor { get; set; }
    public string? ImageLicense { get; set; }
    public string? ImageSourceUrl { get; set; }
    // A manual override keeps its canonical value in the normal property; importers skip fields named here.
    // This avoids parallel Manual* columns and makes partial editorial corrections safe across re-imports.
    public List<string> ManualOverrideFields { get; set; } = [];
    public string BudgetTier { get; set; } = string.Empty;
    public int SuggestedStayMinDays { get; set; }
    public int SuggestedStayMaxDays { get; set; }
    public List<string> BestSeasons { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public List<Place> Places { get; set; } = [];
    public List<Accommodation> Accommodations { get; set; } = [];
    public List<WeatherSnapshot> WeatherSnapshots { get; set; } = [];
    public List<DestinationTranslation> Translations { get; set; } = [];
    public List<SavedPlace> SavedPlaces { get; set; } = [];
}
