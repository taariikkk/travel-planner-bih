using TravelPlanner.Api.Domain.Entities;

namespace TravelPlanner.Application.DTOs;

public sealed record PlaceMetadataResponse(string? DescriptionBs, string? DescriptionEn, string? Description,
    string? DescriptionLanguage, string? Address, string? Cuisine, string? PriceLevel, string? Website,
    string? Phone, string? Email, string Source, string? ExternalId, string? SourceUrl,
    DateTimeOffset? ImportedAt, DateTimeOffset? LastVerifiedAt)
{
    public static PlaceMetadataResponse From(Place place) => new(place.DescriptionBs, place.DescriptionEn,
        place.Description, place.DescriptionLanguage, place.Address, place.Cuisine, place.PriceLevel,
        place.Website, place.Phone, place.Email, place.Source, place.ExternalId, place.SourceUrl,
        place.ImportedAt, place.LastVerifiedAt);

    public static ImageAttributionResponse? ImageAttribution(Place place) =>
        !string.IsNullOrWhiteSpace(place.ImageUrl) && !string.IsNullOrWhiteSpace(place.ImageAuthor)
        && !string.IsNullOrWhiteSpace(place.ImageLicense) && !string.IsNullOrWhiteSpace(place.ImageSourceUrl)
            ? new(place.ImageAuthor, place.ImageLicense, place.ImageSourceUrl) : null;
}
