using System.Text.Json;
using System.Text.RegularExpressions;
using TravelPlanner.Api.Domain.Entities;

namespace TravelPlanner.Api.Infrastructure.Persistence;

public static partial class SeedWikidataIds
{
    public static IReadOnlyDictionary<string, string> Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("seed-wikidata-ids.json mora sadržavati objekt slug → Q-id.");

        var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var seenSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var qidOwners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in document.RootElement.EnumerateObject())
        {
            var slug = property.Name.Trim();
            if (string.IsNullOrWhiteSpace(slug) || !seenSlugs.Add(slug))
                throw new InvalidOperationException("seed-wikidata-ids.json sadrži neispravan ili dupliran slug.");

            if (property.Value.ValueKind == JsonValueKind.Null)
                continue;
            if (property.Value.ValueKind != JsonValueKind.String)
                throw new InvalidOperationException($"Q-id za slug '{slug}' mora biti string ili null.");

            var qid = property.Value.GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(qid))
                continue;

            qid = qid.ToUpperInvariant();
            if (!QidPattern().IsMatch(qid))
                throw new InvalidOperationException($"Q-id za slug '{slug}' mora biti u formatu Q i cifre.");
            if (qidOwners.TryGetValue(qid, out var owner))
                throw new InvalidOperationException($"Q-id '{qid}' je dodijeljen slugovima '{owner}' i '{slug}'.");

            qidOwners.Add(qid, slug);
            mappings.Add(slug, qid);
        }

        return mappings;
    }

    public static void Apply(IReadOnlyDictionary<string, string> mappings, IEnumerable<Destination> destinations)
    {
        var bySlug = destinations.ToDictionary(destination => destination.Slug, StringComparer.OrdinalIgnoreCase);
        var byExternalId = destinations
            .Where(destination => !string.IsNullOrWhiteSpace(destination.ExternalId))
            .ToDictionary(destination => destination.ExternalId!, StringComparer.OrdinalIgnoreCase);

        foreach (var (slug, qid) in mappings)
        {
            if (!bySlug.TryGetValue(slug, out var destination))
                throw new InvalidOperationException($"Slug '{slug}' iz seed-wikidata-ids.json ne postoji među seed destinacijama.");

            if (byExternalId.TryGetValue(qid, out var qidOwner) && qidOwner.Id != destination.Id)
                throw new InvalidOperationException($"Q-id '{qid}' već pripada destinaciji '{qidOwner.Slug}'.");

            if (string.IsNullOrWhiteSpace(destination.ExternalId))
            {
                destination.ExternalId = qid;
                byExternalId[qid] = destination;
                continue;
            }

            if (!string.Equals(destination.ExternalId, qid, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Destinacija '{slug}' već ima ExternalId '{destination.ExternalId}', a konfiguracija traži '{qid}'.");
        }
    }

    [GeneratedRegex("^Q\\d+$", RegexOptions.CultureInvariant)]
    private static partial Regex QidPattern();
}
