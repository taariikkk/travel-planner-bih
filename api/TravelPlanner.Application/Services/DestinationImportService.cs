using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using TravelPlanner.Api.Domain.Entities;
using TravelPlanner.Application.DTOs;
using TravelPlanner.Application.Interfaces;

namespace TravelPlanner.Application.Services;

public sealed partial class DestinationImportService(
    IDestinationDataProvider destinationData,
    IWikipediaSummaryProvider wikipedia,
    IImageProvider images,
    IDestinationImportRepository destinations,
    DestinationImportOptions options,
    TimeProvider timeProvider) : IDestinationImportService
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> ImportLocks = new(StringComparer.OrdinalIgnoreCase);

    public async Task<DestinationImportResult> ImportAsync(string qid, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(qid) || !QidPattern().IsMatch(qid))
            return DestinationImportResult.Failure("Q-id mora biti u formatu Q i cifre.");

        var normalizedQid = qid.ToUpperInvariant();
        var importLock = ImportLocks.GetOrAdd(normalizedQid, _ => new SemaphoreSlim(1, 1));
        await importLock.WaitAsync(cancellationToken);
        try
        {
            var existing = await destinations.GetByExternalIdAsync(normalizedQid, cancellationToken);
            var now = timeProvider.GetUtcNow();
            if (existing?.ImportedAt is { } importedAt && importedAt.AddHours(options.TtlHours) > now)
                return DestinationImportResult.Success(existing.Slug);

            var dataResult = await destinationData.GetByQidAsync(normalizedQid, cancellationToken);
            if (!dataResult.IsSuccess || dataResult.Data is null)
                return DestinationImportResult.Failure(dataResult.Error ?? "Wikidata podaci nisu dostupni.");

            var data = dataResult.Data;
            var name = data.NameBs ?? data.NameEn;
            if (string.IsNullOrWhiteSpace(name))
                return DestinationImportResult.Failure("Wikidata entitet nema naziv.");

            var summariesResult = await wikipedia.GetSummariesAsync(data.BosnianWikipediaArticle, data.EnglishWikipediaArticle, cancellationToken);
            var summaries = summariesResult.IsSuccess && summariesResult.Summaries is not null
                ? summariesResult.Summaries
                : new WikipediaSummaries(null, null);

            var imageResult = await images.GetImageAsync(data.ImageName, cancellationToken);

            var destination = existing ?? new Destination
            {
                Id = Guid.NewGuid(),
                ExternalId = normalizedQid,
                Source = "wikidata",
                Slug = await CreateUniqueSlugAsync(name, data.Region?.NameBs ?? data.Region?.NameEn, normalizedQid, cancellationToken)
            };

            if (existing is null)
            {
                ApplyImportedData(destination, data, summaries,
                    imageResult.IsSuccess ? imageResult.Image : null, now);
            }
            else
            {
                ApplyMissingImportedData(destination, data, summaries,
                    imageResult.IsSuccess ? imageResult.Image : null, now);
            }

            if (existing is null)
                destination = await destinations.AddAsync(destination, cancellationToken);
            else
                await destinations.SaveChangesAsync(cancellationToken);

            return DestinationImportResult.Success(destination.Slug);
        }
        finally
        {
            importLock.Release();
            if (importLock.CurrentCount == 1)
                ImportLocks.TryRemove(new KeyValuePair<string, SemaphoreSlim>(normalizedQid, importLock));
        }
    }

    private void ApplyImportedData(Destination destination, DestinationData data, WikipediaSummaries summaries, DestinationImage? image, DateTimeOffset importedAt)
    {
        if (!IsManual(destination, nameof(Destination.Name)))
            destination.Name = data.NameBs ?? data.NameEn ?? destination.Name;
        if (!IsManual(destination, nameof(Destination.Type)))
            destination.Type = data.Type;
        if (!IsManual(destination, nameof(Destination.Region)))
            destination.Region = data.Region?.NameBs ?? data.Region?.NameEn ?? string.Empty;
        if (!IsManual(destination, nameof(Destination.Latitude)))
            destination.Latitude = data.Latitude;
        if (!IsManual(destination, nameof(Destination.Longitude)))
            destination.Longitude = data.Longitude;
        if (!IsManual(destination, nameof(Destination.ElevationM)))
            destination.ElevationM = data.ElevationM;
        if (!IsManual(destination, nameof(Destination.Population)))
            destination.Population = data.Population is > int.MaxValue ? int.MaxValue : (int?)data.Population;

        if (!IsManual(destination, nameof(Destination.Description)))
            destination.Description = summaries.Bosnian?.Text ?? string.Empty;
        if (!IsManual(destination, nameof(Destination.DescriptionSourceUrl)))
            destination.DescriptionSourceUrl = summaries.Bosnian?.ArticleUrl;
        if (!IsManual(destination, nameof(Destination.DescriptionLicense)))
            destination.DescriptionLicense = summaries.Bosnian?.License;
        if (!IsManual(destination, nameof(Destination.DescriptionLanguage)))
            destination.DescriptionLanguage = summaries.Bosnian is null ? null : "bs";
        if (!IsManual(destination, nameof(Destination.DescriptionEn)))
            destination.DescriptionEn = summaries.English?.Text;
        if (!IsManual(destination, nameof(Destination.DescriptionEnSourceUrl)))
            destination.DescriptionEnSourceUrl = summaries.English?.ArticleUrl;
        if (!IsManual(destination, nameof(Destination.DescriptionEnLicense)))
            destination.DescriptionEnLicense = summaries.English?.License;

        if (!IsManual(destination, nameof(Destination.ImageUrl)))
            destination.ImageUrl = image?.Url;
        if (!IsManual(destination, nameof(Destination.ImageAuthor)))
            destination.ImageAuthor = image?.Author;
        if (!IsManual(destination, nameof(Destination.ImageLicense)))
            destination.ImageLicense = image?.License;
        if (!IsManual(destination, nameof(Destination.ImageSourceUrl)))
            destination.ImageSourceUrl = image?.SourceUrl;

        if (!IsManual(destination, nameof(Destination.Tags)))
            destination.Tags = options.TagsByType.GetValueOrDefault(data.Type)?.ToList() ?? [];
        if (!IsManual(destination, nameof(Destination.BudgetTier)))
            destination.BudgetTier = options.DefaultBudgetTier;
        if (!IsManual(destination, nameof(Destination.SuggestedStayMinDays)))
            destination.SuggestedStayMinDays = options.DefaultSuggestedStayMinDays;
        if (!IsManual(destination, nameof(Destination.SuggestedStayMaxDays)))
            destination.SuggestedStayMaxDays = options.DefaultSuggestedStayMaxDays;
        if (!IsManual(destination, nameof(Destination.BestSeasons)))
            destination.BestSeasons = options.DefaultBestSeasons.ToList();
        if (!IsManual(destination, nameof(Destination.BestTimeToVisit)))
            destination.BestTimeToVisit = options.DefaultBestTimeToVisit;

        if (!IsManual(destination, nameof(Destination.IsRecommendationEligible)))
        {
            var hasSufficientBosnianDescription = IsManual(destination, nameof(Destination.Description))
                ? !string.IsNullOrWhiteSpace(destination.Description)
                : summaries.Bosnian?.IsSufficient == true;
            destination.IsRecommendationEligible = (!options.RequireBosnianDescriptionForRecommendation || hasSufficientBosnianDescription)
                && (!options.RequireCoordinatesForRecommendation || data is { Latitude: not null, Longitude: not null });
        }
        destination.ImportedAt = importedAt;
        if (!string.Equals(destination.Source, "manual", StringComparison.OrdinalIgnoreCase))
            destination.Source = "wikidata";
    }

    private static void ApplyMissingImportedData(Destination destination, DestinationData data, WikipediaSummaries summaries, DestinationImage? image, DateTimeOffset importedAt)
    {
        if (destination.ElevationM is null && !IsManual(destination, nameof(Destination.ElevationM)))
            destination.ElevationM = data.ElevationM;
        if (destination.Population is null && !IsManual(destination, nameof(Destination.Population)))
            destination.Population = data.Population is > int.MaxValue ? int.MaxValue : (int?)data.Population;

        if (string.IsNullOrWhiteSpace(destination.Description) && !IsManual(destination, nameof(Destination.Description)))
        {
            destination.Description = summaries.Bosnian?.Text ?? string.Empty;
            destination.DescriptionSourceUrl = summaries.Bosnian?.ArticleUrl;
            destination.DescriptionLicense = summaries.Bosnian?.License;
            destination.DescriptionLanguage = summaries.Bosnian is null ? null : "bs";
        }

        if (string.IsNullOrWhiteSpace(destination.DescriptionEn) && !IsManual(destination, nameof(Destination.DescriptionEn)))
        {
            destination.DescriptionEn = summaries.English?.Text;
            destination.DescriptionEnSourceUrl = summaries.English?.ArticleUrl;
            destination.DescriptionEnLicense = summaries.English?.License;
        }

        if (string.IsNullOrWhiteSpace(destination.ImageUrl) && !IsManual(destination, nameof(Destination.ImageUrl)))
        {
            destination.ImageUrl = image?.Url;
            destination.ImageAuthor = image?.Author;
            destination.ImageLicense = image?.License;
            destination.ImageSourceUrl = image?.SourceUrl;
        }

        destination.ImportedAt = importedAt;
    }

    private async Task<string> CreateUniqueSlugAsync(string name, string? region, string qid, CancellationToken cancellationToken)
    {
        var baseSlug = Slugify(name);
        if (!await destinations.SlugExistsAsync(baseSlug, cancellationToken))
            return baseSlug;

        if (!string.IsNullOrWhiteSpace(region))
        {
            var regionalSlug = $"{baseSlug}-{Slugify(region)}";
            if (!await destinations.SlugExistsAsync(regionalSlug, cancellationToken))
                return regionalSlug;
        }

        return $"{baseSlug}-{qid.ToLowerInvariant()}";
    }

    internal static string Slugify(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Replace("đ", "dj", StringComparison.Ordinal).Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                continue;
            builder.Append(char.IsLetterOrDigit(character) ? character : '-');
        }

        return MultipleHyphensPattern().Replace(builder.ToString(), "-").Trim('-');
    }

    private static bool IsManual(Destination destination, string field) =>
        destination.ManualOverrideFields.Contains(field, StringComparer.OrdinalIgnoreCase);

    [GeneratedRegex("^Q\\d+$", RegexOptions.CultureInvariant)]
    private static partial Regex QidPattern();

    [GeneratedRegex("-+")]
    private static partial Regex MultipleHyphensPattern();
}
