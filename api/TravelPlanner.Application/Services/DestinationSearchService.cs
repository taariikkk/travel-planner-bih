using TravelPlanner.Application.DTOs;
using TravelPlanner.Application.Interfaces;

namespace TravelPlanner.Application.Services;

public sealed class DestinationSearchService(
    IDestinationSearchRepository destinations,
    IDestinationDataProvider destinationData,
    IDestinationImportService importer) : IDestinationSearchService
{
    internal const int ResultLimit = 12;
    internal const int FallbackThreshold = 6;

    public async Task<DestinationSearchResponse> SearchAsync(
        string? query, string? type, string? region, string language,
        CancellationToken cancellationToken)
    {
        var normalizedQuery = query?.Trim() ?? string.Empty;
        var normalizedType = NormalizeFilter(type);
        var normalizedRegion = NormalizeFilter(region);
        var local = await destinations.SearchAsync(
            normalizedQuery, normalizedType, normalizedRegion, language, ResultLimit, cancellationToken);

        var usedFallback = normalizedQuery.Length >= 2 && local.Count < FallbackThreshold;
        if (usedFallback)
        {
            var remote = await destinationData.SearchAsync(normalizedQuery, cancellationToken);
            if (remote.IsSuccess)
            {
                var candidates = remote.Destinations
                    .Where(item => Matches(item, normalizedType, normalizedRegion, language))
                    .DistinctBy(item => item.Qid, StringComparer.OrdinalIgnoreCase)
                    .Take(ResultLimit - local.Count);

                foreach (var candidate in candidates)
                    await importer.ImportAsync(candidate.Qid, cancellationToken);

                local = await destinations.SearchAsync(
                    normalizedQuery, normalizedType, normalizedRegion, language, ResultLimit, cancellationToken);
            }
        }

        var filters = await destinations.GetFiltersAsync(cancellationToken);
        return new DestinationSearchResponse(local, filters, usedFallback);
    }

    private static string? NormalizeFilter(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool Matches(DestinationData item, string? type, string? region, string language)
    {
        if (type is not null && !item.Type.Equals(type, StringComparison.OrdinalIgnoreCase))
            return false;

        if (region is null)
            return true;

        var itemRegion = language == "en" ? item.Region?.NameEn ?? item.Region?.NameBs : item.Region?.NameBs ?? item.Region?.NameEn;
        return itemRegion?.Equals(region, StringComparison.OrdinalIgnoreCase) == true;
    }
}
