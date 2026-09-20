namespace TravelPlanner.Application.Services;

public sealed class DestinationImportOptions
{
    public const string SectionName = "DestinationImport";

    public int TtlHours { get; init; } = 168;
    public string DefaultBudgetTier { get; init; } = "standard";
    public int DefaultSuggestedStayMinDays { get; init; } = 1;
    public int DefaultSuggestedStayMaxDays { get; init; } = 3;
    public string DefaultBestTimeToVisit { get; init; } = string.Empty;
    public List<string> DefaultBestSeasons { get; init; } = ["spring", "summer", "autumn"];
    public Dictionary<string, List<string>> TagsByType { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public bool RequireCoordinatesForRecommendation { get; init; } = true;
    public bool RequireBosnianDescriptionForRecommendation { get; init; } = true;
}
