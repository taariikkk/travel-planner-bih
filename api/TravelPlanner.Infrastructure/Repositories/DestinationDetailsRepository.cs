using Microsoft.EntityFrameworkCore;
using TravelPlanner.Api.Infrastructure.Persistence;
using TravelPlanner.Application.DTOs;
using TravelPlanner.Application.Interfaces;

namespace TravelPlanner.Infrastructure.Repositories;

public sealed class DestinationDetailsRepository(ApplicationDbContext context) : IDestinationDetailsRepository
{
    public async Task<DestinationDetailsResponse?> GetBySlugAsync(string slug, string language, CancellationToken cancellationToken)
    {
        var destination = await context.Destinations.AsNoTracking()
            .Include(item => item.Translations)
            .SingleOrDefaultAsync(item => item.Slug == slug, cancellationToken);
        if (destination is null) return null;
        var translation = destination.Translations.FirstOrDefault(item => item.LanguageCode == language);
        var hasCoordinates = destination is { Latitude: not null, Longitude: not null };
        var placesQuery = PlaceReadModel.Query(context, destination.Id, destination.Latitude, destination.Longitude,
            validCoordinatesOnly: hasCoordinates);
        var nearest = await (hasCoordinates
                ? placesQuery.OrderBy(place => place.DistanceMeters).ThenBy(place => place.Name).ThenBy(place => place.Id)
                : placesQuery.OrderBy(place => place.Name).ThenBy(place => place.Id))
            .Take(6).ToArrayAsync(cancellationToken);
        var placeResponses = nearest.Select(place => place.ToDetailsResponse()).ToArray();
        var sarajevo = destination.Slug != "sarajevo" && destination.Latitude is not null && destination.Longitude is not null
            ? await context.Destinations.AsNoTracking().Where(item => item.Slug == "sarajevo")
                .Select(item => new { item.Latitude, item.Longitude }).SingleOrDefaultAsync(cancellationToken)
            : null;
        var distance = destination.Slug != "sarajevo" && destination.Latitude is not null && destination.Longitude is not null
            && sarajevo is { Latitude: not null, Longitude: not null }
            ? await context.Database.SqlQuery<double>(DestinationDistance.QueryKm(destination.Latitude.Value, destination.Longitude.Value, sarajevo.Latitude.Value, sarajevo.Longitude.Value)).SingleAsync(cancellationToken)
            : (double?)null;
        var usingEnglishImport = translation is null && !string.IsNullOrWhiteSpace(destination.DescriptionEn)
            && (language == "en" || string.IsNullOrWhiteSpace(destination.Description));
        var description = translation?.Description
            ?? (usingEnglishImport ? destination.DescriptionEn! : destination.Description);
        var descriptionLanguage = translation?.LanguageCode
            ?? (usingEnglishImport ? "en" : destination.DescriptionLanguage ?? "bs");
        var attribution = destination.Source.Equals("wikidata", StringComparison.OrdinalIgnoreCase) && translation is null
            ? usingEnglishImport
                ? CreateTextAttribution(destination.DescriptionEnLicense, destination.DescriptionEnSourceUrl)
                : CreateTextAttribution(destination.DescriptionLicense, destination.DescriptionSourceUrl)
            : null;
        var imageAttribution = destination.ImageUrl is not null
            && !string.IsNullOrWhiteSpace(destination.ImageAuthor)
            && !string.IsNullOrWhiteSpace(destination.ImageLicense)
            && !string.IsNullOrWhiteSpace(destination.ImageSourceUrl)
                ? new ImageAttributionResponse(destination.ImageAuthor, destination.ImageLicense, destination.ImageSourceUrl)
                : null;
        return new(destination.Id, destination.Slug, destination.Name, destination.Region,
            description,
            translation?.BestTimeToVisit ?? destination.BestTimeToVisit,
            destination.SuggestedStayMinDays, destination.SuggestedStayMaxDays, destination.Tags,
            destination.Latitude, destination.Longitude, placeResponses, distance, destination.ElevationM,
            null, destination.Population, destination.ImageUrl, imageAttribution, attribution, descriptionLanguage);
    }

    private static TextAttributionResponse? CreateTextAttribution(string? license, string? url) =>
        !string.IsNullOrWhiteSpace(license) && !string.IsNullOrWhiteSpace(url) ? new(license, url) : null;
}
