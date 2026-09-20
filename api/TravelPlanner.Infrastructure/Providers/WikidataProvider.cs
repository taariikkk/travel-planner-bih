using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using TravelPlanner.Application.DTOs;
using TravelPlanner.Application.Interfaces;

namespace TravelPlanner.Infrastructure.Providers;

public sealed partial class WikidataProvider(IHttpClientFactory httpClientFactory, IOptions<WikidataOptions> options) : IDestinationDataProvider
{
    public const string ClientName = "Wikidata";
    private const string BosniaAndHerzegovinaQid = "Q225";
    private readonly WikidataOptions options = options.Value;

    public async Task<DestinationDataSearchResult> SearchAsync(string searchText, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return DestinationDataSearchResult.Failure("Search text is required.");

        try
        {
            var entityCache = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            var bosnianResults = await SearchByLanguageAsync(searchText.Trim(), "bs", entityCache, cancellationToken);
            if (bosnianResults.Count > 0)
                return DestinationDataSearchResult.Success(bosnianResults);

            return DestinationDataSearchResult.Success(await SearchByLanguageAsync(searchText.Trim(), "en", entityCache, cancellationToken));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return DestinationDataSearchResult.Failure("Wikidata request timed out.");
        }
        catch (HttpRequestException)
        {
            return DestinationDataSearchResult.Failure("Wikidata request failed.");
        }
        catch (JsonException)
        {
            return DestinationDataSearchResult.Failure("Wikidata returned an invalid response.");
        }
        catch (Exception)
        {
            return DestinationDataSearchResult.Failure("Wikidata data could not be processed.");
        }
    }

    public async Task<DestinationDataResult> GetByQidAsync(string qid, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(qid) || !QidPattern().IsMatch(qid))
            return DestinationDataResult.Failure("Q-id must have the form Q followed by digits.");

        try
        {
            var entityCache = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            var entity = await GetEntityAsync(qid, entityCache, cancellationToken);
            if (entity is null)
                return DestinationDataResult.Failure("Wikidata entity was not found.");

            var data = await MapEntityAsync(qid, entity.Value, entityCache, cancellationToken);
            return data is null
                ? DestinationDataResult.Failure("Wikidata entity is not in Bosnia and Herzegovina.")
                : DestinationDataResult.Success(data);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return DestinationDataResult.Failure("Wikidata request timed out.");
        }
        catch (HttpRequestException)
        {
            return DestinationDataResult.Failure("Wikidata request failed.");
        }
        catch (JsonException)
        {
            return DestinationDataResult.Failure("Wikidata returned an invalid response.");
        }
        catch (Exception)
        {
            return DestinationDataResult.Failure("Wikidata data could not be processed.");
        }
    }

    private async Task<List<DestinationData>> SearchByLanguageAsync(string searchText, string language, Dictionary<string, JsonElement> entityCache, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(ClientName);
        var response = await client.GetAsync($"w/api.php?action=wbsearchentities&format=json&language={language}&limit=10&search={Uri.EscapeDataString(searchText)}", cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));

        var results = new List<DestinationData>();
        var seenQids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!document.RootElement.TryGetProperty("search", out var searchResults))
            return results;

        foreach (var result in searchResults.EnumerateArray())
        {
            if (!result.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.String || !seenQids.Add(id.GetString()!))
                continue;

            var entity = await GetEntityAsync(id.GetString()!, entityCache, cancellationToken);
            if (entity is null)
                continue;
            var data = await MapEntityAsync(id.GetString()!, entity.Value, entityCache, cancellationToken);
            if (data is not null)
                results.Add(data);
        }

        return results;
    }

    private async Task<JsonElement?> GetEntityAsync(string qid, Dictionary<string, JsonElement> entityCache, CancellationToken cancellationToken)
    {
        if (entityCache.TryGetValue(qid, out var cached))
            return cached;

        var client = httpClientFactory.CreateClient(ClientName);
        var response = await client.GetAsync($"wiki/Special:EntityData/{qid}.json", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
        if (!document.RootElement.TryGetProperty("entities", out var entities) || !entities.TryGetProperty(qid, out var entity))
            return null;

        var copy = entity.Clone();
        entityCache[qid] = copy;
        return copy;
    }

    private async Task<DestinationData?> MapEntityAsync(string qid, JsonElement entity, Dictionary<string, JsonElement> entityCache, CancellationToken cancellationToken)
    {
        var administrativeChain = await GetAdministrativeChainAsync(entity, entityCache, cancellationToken);
        if (!IsInBosniaAndHerzegovina(entity, administrativeChain))
            return null;

        var coordinate = GetFirstDataValue(entity, "P625");
        var latitude = coordinate is { ValueKind: JsonValueKind.Object } && coordinate.Value.TryGetProperty("latitude", out var lat) ? (double?)lat.GetDouble() : null;
        var longitude = coordinate is { ValueKind: JsonValueKind.Object } && coordinate.Value.TryGetProperty("longitude", out var lon) ? (double?)lon.GetDouble() : null;
        var classes = GetEntityIds(entity, "P31");
        var type = classes.Select(classId => options.ClassTypeMappings.GetValueOrDefault(classId)).FirstOrDefault(mapped => !string.IsNullOrWhiteSpace(mapped)) ?? "ostalo";
        var regionEntity = administrativeChain.FirstOrDefault(item => options.RegionEntityIds.Contains(item.Qid));

        return new DestinationData(
            qid,
            GetLocalizedValue(entity, "labels", "bs"),
            GetLocalizedValue(entity, "labels", "en"),
            latitude,
            longitude,
            type,
            GetNumber(entity, "P2044"),
            GetInteger(entity, "P1082"),
            GetString(entity, "P18"),
            GetSitelinkTitle(entity, "bswiki"),
            GetSitelinkTitle(entity, "enwiki"),
            regionEntity is null ? null : new DestinationDataRegion(GetLocalizedValue(regionEntity.Entity, "labels", "bs"), GetLocalizedValue(regionEntity.Entity, "labels", "en")));
    }

    private async Task<List<AdministrativeEntity>> GetAdministrativeChainAsync(JsonElement entity, Dictionary<string, JsonElement> entityCache, CancellationToken cancellationToken)
    {
        var chain = new List<AdministrativeEntity>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var nextQid = GetEntityIds(entity, "P131").FirstOrDefault();
        while (nextQid is not null && visited.Add(nextQid))
        {
            var nextEntity = await GetEntityAsync(nextQid, entityCache, cancellationToken);
            if (nextEntity is null)
                break;
            chain.Add(new AdministrativeEntity(nextQid, nextEntity.Value));
            if (options.RegionEntityIds.Contains(nextQid))
                break;
            nextQid = GetEntityIds(nextEntity.Value, "P131").FirstOrDefault();
        }

        return chain;
    }

    private static bool IsInBosniaAndHerzegovina(JsonElement entity, IEnumerable<AdministrativeEntity> administrativeChain) =>
        GetEntityIds(entity, "P17").Contains(BosniaAndHerzegovinaQid, StringComparer.OrdinalIgnoreCase)
        || administrativeChain.Any(item => item.Qid.Equals(BosniaAndHerzegovinaQid, StringComparison.OrdinalIgnoreCase)
            || GetEntityIds(item.Entity, "P17").Contains(BosniaAndHerzegovinaQid, StringComparer.OrdinalIgnoreCase));

    private static IEnumerable<string> GetEntityIds(JsonElement entity, string property) =>
        GetStatements(entity, property)
            .Select(statement => statement.TryGetProperty("mainsnak", out var snak) ? GetDataValue(snak) : null)
            .Where(value => value is { ValueKind: JsonValueKind.Object } && value.Value.TryGetProperty("id", out _))
            .Select(value => value!.Value.GetProperty("id").GetString()!)
            .Where(id => !string.IsNullOrWhiteSpace(id));

    private static JsonElement? GetFirstDataValue(JsonElement entity, string property) =>
        GetStatements(entity, property)
            .Select(statement => statement.TryGetProperty("mainsnak", out var snak) ? GetDataValue(snak) : null)
            .FirstOrDefault(value => value is not null);

    private static IEnumerable<JsonElement> GetStatements(JsonElement entity, string property) =>
        entity.TryGetProperty("claims", out var claims) && claims.TryGetProperty(property, out var statements)
            ? statements.EnumerateArray()
            : [];

    private static JsonElement? GetDataValue(JsonElement snak) =>
        snak.TryGetProperty("datavalue", out var dataValue) && dataValue.TryGetProperty("value", out var value) ? value : null;

    private static string? GetLocalizedValue(JsonElement entity, string section, string language) =>
        entity.TryGetProperty(section, out var values) && values.TryGetProperty(language, out var value) && value.TryGetProperty("value", out var text)
            ? text.GetString()
            : null;

    private static string? GetSitelinkTitle(JsonElement entity, string site) =>
        entity.TryGetProperty("sitelinks", out var sitelinks) && sitelinks.TryGetProperty(site, out var sitelink) && sitelink.TryGetProperty("title", out var title)
            ? title.GetString()
            : null;

    private static string? GetString(JsonElement entity, string property) =>
        GetFirstDataValue(entity, property) is { ValueKind: JsonValueKind.String } value ? value.GetString() : null;

    private static double? GetNumber(JsonElement entity, string property)
    {
        var amount = GetFirstDataValue(entity, property) is { ValueKind: JsonValueKind.Object } value && value.TryGetProperty("amount", out var rawAmount)
            ? rawAmount.GetString()
            : null;
        return double.TryParse(amount, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : null;
    }

    private static long? GetInteger(JsonElement entity, string property)
    {
        var amount = GetFirstDataValue(entity, property) is { ValueKind: JsonValueKind.Object } value && value.TryGetProperty("amount", out var rawAmount)
            ? rawAmount.GetString()
            : null;
        return long.TryParse(amount, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : null;
    }

    private sealed record AdministrativeEntity(string Qid, JsonElement Entity);

    [GeneratedRegex("^Q\\d+$", RegexOptions.CultureInvariant)]
    private static partial Regex QidPattern();
}
