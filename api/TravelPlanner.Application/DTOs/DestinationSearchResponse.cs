namespace TravelPlanner.Application.DTOs;

public sealed record DestinationSearchResponse(
    IReadOnlyList<DestinationSearchItemResponse> Items,
    DestinationSearchFiltersResponse Filters,
    bool UsedFallback);

public sealed record DestinationSearchItemResponse(
    Guid Id,
    string Slug,
    string Name,
    string Type,
    string Region,
    string Description,
    string Source,
    string? ImageUrl,
    ImageAttributionResponse? ImageAttribution,
    TextAttributionResponse? DescriptionAttribution);

public sealed record DestinationSearchFiltersResponse(
    IReadOnlyList<string> Types,
    IReadOnlyList<string> Regions);
