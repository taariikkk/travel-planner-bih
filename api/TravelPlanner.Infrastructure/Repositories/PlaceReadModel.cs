using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using TravelPlanner.Api.Infrastructure.Persistence;
using TravelPlanner.Application.DTOs;

namespace TravelPlanner.Infrastructure.Repositories;

// Unmapped read model: no tracked Place entities or import-only bookkeeping are loaded.
internal sealed class PlaceReadModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double DistanceMeters { get; set; }
    public string? ImageUrl { get; set; }
    public string? ImageAuthor { get; set; }
    public string? ImageLicense { get; set; }
    public string? ImageSourceUrl { get; set; }
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
    public string Source { get; set; } = "";
    public string? ExternalId { get; set; }
    public string? SourceUrl { get; set; }
    public DateTimeOffset? ImportedAt { get; set; }
    public DateTimeOffset? LastVerifiedAt { get; set; }

    internal static IQueryable<PlaceReadModel> Query(ApplicationDbContext context, Guid destinationId,
        double? latitude = null, double? longitude = null, bool validCoordinatesOnly = true)
    {
        // Only fixed SQL fragments are composed. All destination values remain SQL parameters.
        const string columns = """
            p."Id", p."Name", p."Category", ST_Y(p."Location") AS "Latitude", ST_X(p."Location") AS "Longitude",
            p."ImageUrl", p."ImageAuthor", p."ImageLicense", p."ImageSourceUrl",
            p."DescriptionBs", p."DescriptionEn", p."Description", p."DescriptionLanguage",
            p."Address", p."Cuisine", p."PriceLevel", p."Website", p."Phone", p."Email",
            p."Source", p."ExternalId", p."SourceUrl", p."ImportedAt", p."LastVerifiedAt"
            """;
        const string valid = """
             AND NOT ST_IsEmpty(p."Location")
             AND ST_X(p."Location") BETWEEN -180 AND 180
             AND ST_Y(p."Location") BETWEEN -90 AND 90
            """;
        var hasOrigin = latitude is not null && longitude is not null;
        var distance = hasOrigin
            ? """ST_Distance(p."Location"::geography, ST_SetSRID(ST_MakePoint({1}, {2}), 4326)::geography)"""
            : "0.0::double precision";
        var sql = "SELECT " + columns + ", " + distance + """ AS "DistanceMeters" FROM "Place" p WHERE p."Id" IN (SELECT "PlaceId" FROM "DestinationPlace" WHERE "DestinationId" = {0})"""
            + (validCoordinatesOnly ? valid : "");
        object[] values = hasOrigin ? [destinationId, longitude!.Value, latitude!.Value] : [destinationId];
        return context.Database.SqlQuery<PlaceReadModel>(FormattableStringFactory.Create(sql, values)).AsNoTracking();
    }

    private PlaceMetadataResponse Metadata => new(DescriptionBs, DescriptionEn, Description, DescriptionLanguage,
        Address, Cuisine, PriceLevel, Website, Phone, Email, Source, ExternalId, SourceUrl, ImportedAt, LastVerifiedAt);

    private ImageAttributionResponse? Attribution =>
        !string.IsNullOrWhiteSpace(ImageUrl) && !string.IsNullOrWhiteSpace(ImageAuthor)
        && !string.IsNullOrWhiteSpace(ImageLicense) && !string.IsNullOrWhiteSpace(ImageSourceUrl)
            ? new(ImageAuthor, ImageLicense, ImageSourceUrl) : null;

    internal PlaceResponse ToDetailsResponse()
    {
        var attribution = Attribution;
        return new(Id, Name, Category, Latitude, Longitude,
            attribution is null ? null : ImageUrl, attribution, Metadata);
    }

    internal DestinationMapPlaceResponse ToMapResponse(bool includeDistance = false)
    {
        var attribution = Attribution;
        return new(Id, Name, Category, Latitude, Longitude, Metadata,
            attribution is null ? null : ImageUrl, attribution,
            includeDistance ? DistanceMeters / 1000 : null);
    }
}
