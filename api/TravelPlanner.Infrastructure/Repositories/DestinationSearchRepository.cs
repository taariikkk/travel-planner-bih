using Microsoft.EntityFrameworkCore;
using TravelPlanner.Api.Domain.Entities;
using TravelPlanner.Api.Infrastructure.Persistence;
using TravelPlanner.Application.DTOs;
using TravelPlanner.Application.Interfaces;

namespace TravelPlanner.Infrastructure.Repositories;

public sealed class DestinationSearchRepository(ApplicationDbContext context) : IDestinationSearchRepository
{
    public async Task<IReadOnlyList<DestinationSearchItemResponse>> SearchAsync(
        string query, string? type, string? region, string language, int limit,
        CancellationToken cancellationToken)
    {
        var ids = await context.Database.SqlQuery<Guid>($"""
            SELECT destination."Id" AS "Value"
            FROM "Destination" AS destination
            WHERE (CAST({type} AS text) IS NULL OR lower(destination."Type") = lower({type}))
              AND (CAST({region} AS text) IS NULL OR immutable_unaccent(lower(destination."Region")) = immutable_unaccent(lower({region})))
              AND ({query} = ''
                   OR immutable_unaccent(lower(destination."Name")) % immutable_unaccent(lower({query}))
                   OR immutable_unaccent(lower(destination."Name")) LIKE '%' || immutable_unaccent(lower({query})) || '%')
            ORDER BY CASE WHEN lower(destination."Source") = 'manual' THEN 0 ELSE 1 END,
                     CASE
                       WHEN immutable_unaccent(lower(destination."Name")) = immutable_unaccent(lower({query})) THEN 0
                       WHEN immutable_unaccent(lower(destination."Name")) LIKE immutable_unaccent(lower({query})) || '%' THEN 1
                       ELSE 2
                     END,
                     similarity(immutable_unaccent(lower(destination."Name")), immutable_unaccent(lower({query}))) DESC,
                     destination."Name", destination."Id"
            LIMIT {limit}
            """)
            .ToArrayAsync(cancellationToken);

        var items = await context.Destinations.AsNoTracking()
            .Where(destination => ids.Contains(destination.Id))
            .Include(destination => destination.Translations)
            .ToDictionaryAsync(destination => destination.Id, cancellationToken);
        return ids.Select(id => Map(items[id], language)).ToArray();
    }

    public async Task<DestinationSearchFiltersResponse> GetFiltersAsync(CancellationToken cancellationToken)
    {
        var types = await context.Destinations.AsNoTracking().Select(item => item.Type).Distinct().OrderBy(value => value).ToArrayAsync(cancellationToken);
        var regions = await context.Destinations.AsNoTracking().Where(item => item.Region != string.Empty).Select(item => item.Region).Distinct().OrderBy(value => value).ToArrayAsync(cancellationToken);
        return new DestinationSearchFiltersResponse(types, regions);
    }

    private static DestinationSearchItemResponse Map(Destination destination, string language)
    {
        var translation = destination.Translations.FirstOrDefault(item => item.LanguageCode == language);
        var usingEnglishImport = translation is null && language == "en" && !string.IsNullOrWhiteSpace(destination.DescriptionEn);
        var description = translation?.Description
            ?? (usingEnglishImport
                ? destination.DescriptionEn!
                : destination.Description);
        var imageAttribution = destination.ImageUrl is not null
            && !string.IsNullOrWhiteSpace(destination.ImageAuthor)
            && !string.IsNullOrWhiteSpace(destination.ImageLicense)
            && !string.IsNullOrWhiteSpace(destination.ImageSourceUrl)
                ? new ImageAttributionResponse(destination.ImageAuthor, destination.ImageLicense, destination.ImageSourceUrl)
                : null;
        var descriptionAttribution = destination.Source.Equals("wikidata", StringComparison.OrdinalIgnoreCase) && translation is null
            ? CreateTextAttribution(
                usingEnglishImport ? destination.DescriptionEnLicense : destination.DescriptionLicense,
                usingEnglishImport ? destination.DescriptionEnSourceUrl : destination.DescriptionSourceUrl)
            : null;

        return new DestinationSearchItemResponse(
            destination.Id, destination.Slug, destination.Name, destination.Type, destination.Region,
            CreateSummary(description), destination.Source, destination.ImageUrl, imageAttribution, descriptionAttribution);
    }

    private static string CreateSummary(string description)
    {
        const int maximumLength = 240;
        var normalized = description.Trim();
        if (normalized.Length <= maximumLength)
            return normalized;
        var boundary = normalized.LastIndexOf(' ', maximumLength);
        return $"{normalized[..(boundary > 0 ? boundary : maximumLength)].TrimEnd()}…";
    }

    private static TextAttributionResponse? CreateTextAttribution(string? license, string? url) =>
        !string.IsNullOrWhiteSpace(license) && !string.IsNullOrWhiteSpace(url) ? new(license, url) : null;
}
