using Microsoft.EntityFrameworkCore;
using TravelPlanner.Api.Infrastructure.Persistence;
using TravelPlanner.Application.DTOs;
using TravelPlanner.Application.Interfaces;

namespace TravelPlanner.Infrastructure.Repositories;

public sealed class DestinationDetailsRepository(ApplicationDbContext context) : IDestinationDetailsRepository
{
    // MVP assumption: stable URLs and approximate map centers for the existing curated dataset.
    // Destination currently has neither a slug nor a coordinate column; do not derive centers from nearby places.
    private static readonly Dictionary<string, (string Name, double Lat, double Lng)> Catalog = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sarajevo"] = ("Sarajevo", 43.859, 18.429),
        ["mostar"] = ("Mostar", 43.3373, 17.8150),
        ["trebinje"] = ("Trebinje", 42.711, 18.344),
        ["neum"] = ("Neum", 42.923, 17.616),
        ["jahorina"] = ("Jahorina", 43.735, 18.569),
        ["bjelasnica"] = ("Bjelašnica", 43.715, 18.288),
        ["banja-luka"] = ("Banja Luka", 44.772, 17.191),
        ["travnik"] = ("Travnik", 44.227, 17.665),
        ["pocitelj"] = ("Počitelj", 43.134, 17.732),
        ["visegrad"] = ("Višegrad", 43.782, 19.293),
        ["jajce"] = ("Jajce", 44.338, 17.270)
    };

    public async Task<DestinationDetailsResponse?> GetBySlugAsync(string slug, string language, CancellationToken cancellationToken)
    {
        if (!Catalog.TryGetValue(slug, out var entry)) return null;
        var destination = await context.Destinations.AsNoTracking()
            .Include(item => item.Translations)
            .SingleOrDefaultAsync(item => item.Name == entry.Name, cancellationToken);
        if (destination is null) return null;
        var translation = destination.Translations.FirstOrDefault(item => item.LanguageCode == language);
        // Assumption: nearby means places belonging to this destination, ordered from its existing map center.
        // Cast geometry to geography: ST_Distance then returns geodesic meters, not degrees.
        var nearest = await context.Places.FromSqlInterpolated($"""
            SELECT * FROM "Place"
            WHERE "DestinationId" = {destination.Id}
              AND NOT ST_IsEmpty("Location")
              AND ST_X("Location") BETWEEN -180 AND 180
              AND ST_Y("Location") BETWEEN -90 AND 90
            ORDER BY ST_Distance("Location"::geography,
                ST_SetSRID(ST_MakePoint({entry.Lng}, {entry.Lat}), 4326)::geography), "Name", "Id"
            LIMIT 6
            """).AsNoTracking().ToArrayAsync(cancellationToken);
        var places = nearest.Select(place => new PlaceResponse(
            place.Id, place.Name, place.Category, place.Location.Y, place.Location.X)).ToArray();
        var sarajevo = Catalog["sarajevo"];
        var distance = await context.Database.SqlQuery<double>(
            DestinationDistance.QueryKm(entry.Lat, entry.Lng, sarajevo.Lat, sarajevo.Lng))
            .SingleAsync(cancellationToken);
        return new(destination.Id, slug.ToLowerInvariant(), destination.Name, destination.Region,
            translation?.Description ?? (language == "en" ? destination.DescriptionEn : null) ?? destination.Description,
            translation?.BestTimeToVisit ?? destination.BestTimeToVisit,
            destination.SuggestedStayMinDays, destination.SuggestedStayMaxDays, destination.Tags,
            entry.Lat, entry.Lng, places, slug.Equals("sarajevo", StringComparison.OrdinalIgnoreCase) ? null : distance);
    }
}
