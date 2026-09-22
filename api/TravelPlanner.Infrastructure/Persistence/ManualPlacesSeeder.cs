using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using NetTopologySuite.Geometries;
using TravelPlanner.Api.Domain.Entities;
using TravelPlanner.Infrastructure.Providers;
using TravelPlanner.Infrastructure.Repositories;

namespace TravelPlanner.Api.Infrastructure.Persistence;

public sealed partial class ManualPlacesSeeder(ApplicationDbContext context, IHostEnvironment environment)
{
    private static readonly IReadOnlyDictionary<string, string> Fields = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["name"] = nameof(Place.Name), ["category"] = nameof(Place.Category), ["location"] = nameof(Place.Location),
        ["descriptionBs"] = nameof(Place.DescriptionBs), ["descriptionEn"] = nameof(Place.DescriptionEn),
        ["description"] = nameof(Place.Description), ["descriptionLanguage"] = nameof(Place.DescriptionLanguage),
        ["address"] = nameof(Place.Address), ["cuisine"] = nameof(Place.Cuisine), ["priceLevel"] = nameof(Place.PriceLevel),
        ["website"] = nameof(Place.Website), ["phone"] = nameof(Place.Phone), ["email"] = nameof(Place.Email),
        ["imageUrl"] = nameof(Place.ImageUrl), ["imageAuthor"] = nameof(Place.ImageAuthor),
        ["imageLicense"] = nameof(Place.ImageLicense), ["imageSourceUrl"] = nameof(Place.ImageSourceUrl)
    };

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var path = Path.Combine(environment.ContentRootPath, "SeedData", "manual-places.json");
        await SeedJsonAsync(await File.ReadAllTextAsync(path, cancellationToken), cancellationToken);
    }

    public async Task SeedJsonAsync(string json, CancellationToken cancellationToken)
    {
        var records = Parse(json);
        if (records.Count == 0) return;
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({PlacesImportRepository.WriteLock})", cancellationToken);
        var slugs = records.SelectMany(r => r.Destinations).Distinct().ToArray();
        var destinations = await context.Destinations.Where(d => slugs.Contains(d.Slug)).ToDictionaryAsync(d => d.Slug, cancellationToken);
        if (destinations.Count != slugs.Length) throw Invalid("Nepoznat slug destinacije.");
        foreach (var record in records)
        {
            var byKey = await context.Places.Include(p => p.DestinationLinks).SingleOrDefaultAsync(p => p.ManualKey == record.Key, cancellationToken);
            var byOsm = record.ExternalId is null ? null : await context.Places.Include(p => p.DestinationLinks)
                .SingleOrDefaultAsync(p => p.ExternalId == record.ExternalId, cancellationToken);
            if (byKey is not null && byOsm is not null && byKey.Id != byOsm.Id
                || byKey?.ExternalId is not null && record.ExternalId is not null && byKey.ExternalId != record.ExternalId
                || byOsm?.ManualKey is not null && byOsm.ManualKey != record.Key)
                throw Invalid($"Konflikt identiteta: {record.Key}.");
            var place = byKey ?? byOsm;
            if (place is null)
            {
                if (!new[] { "name", "category", "location" }.All(record.Fields.ContainsKey))
                    throw Invalid($"Novo mjesto {record.Key} zahtijeva name, category i location.");
                place = new Place { Id = Guid.NewGuid(), ExternalId = record.ExternalId };
                context.Places.Add(place);
            }
            place.ManualKey = record.Key;
            place.ExternalId ??= record.ExternalId;
            if (place.ExternalId is not null) place.SourceUrl = $"https://www.openstreetmap.org/{place.ExternalId}";
            place.Source = "manual";
            foreach (var (field, value) in record.Fields)
            {
                var property = Fields[field];
                if (field == "location") place.Location = ReadLocation(value);
                else typeof(Place).GetProperty(property)!.SetValue(place, value.ValueKind == JsonValueKind.Null ? null : value.GetString());
                if (!place.ManualOverrideFields.Contains(property)) place.ManualOverrideFields.Add(property);
            }
            var imageValues = new[] { place.ImageUrl, place.ImageAuthor, place.ImageLicense, place.ImageSourceUrl };
            if (imageValues.Any(v => v is not null) && imageValues.Any(string.IsNullOrWhiteSpace))
                throw Invalid($"Slika {record.Key} zahtijeva URL, autora, licencu i izvorni URL.");
            foreach (var slug in record.Destinations)
            {
                var destinationId = destinations[slug].Id;
                var link = place.DestinationLinks.SingleOrDefault(l => l.DestinationId == destinationId);
                if (link is null) place.DestinationLinks.Add(new() { DestinationId = destinationId, IsManual = true });
                else link.IsManual = true;
            }
        }
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public sealed record ManualPlace(string Key, string[] Destinations, string? ExternalId, Dictionary<string, JsonElement> Fields);

    public static IReadOnlyList<ManualPlace> Parse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array) throw Invalid("Očekivan JSON niz.");
            var records = new List<ManualPlace>();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in document.RootElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) throw Invalid("Očekivan objekat mjesta.");
                var properties = item.EnumerateObject().ToArray();
                if (properties.Select(p => p.Name).Distinct().Count() != properties.Length
                    || properties.Any(p => p.Name is not ("key" or "destinations" or "externalId" or "fields")))
                    throw Invalid("Nepoznata ili ponovljena svojstva mjesta.");
                var key = item.GetProperty("key").GetString();
                if (string.IsNullOrWhiteSpace(key) || key.Length > 120 || !keys.Add(key)) throw Invalid("Neispravan ili ponovljen key.");
                var destinations = item.GetProperty("destinations").EnumerateArray().Select(e => e.GetString()!).ToArray();
                if (destinations.Length == 0 || destinations.Any(string.IsNullOrWhiteSpace)
                    || destinations.Distinct().Count() != destinations.Length) throw Invalid($"Neispravne destinacije: {key}.");
                var externalId = item.TryGetProperty("externalId", out var external) && external.ValueKind != JsonValueKind.Null ? external.GetString() : null;
                if (externalId is not null && (!OsmId().IsMatch(externalId) || !ids.Add(externalId))) throw Invalid($"Neispravan ili ponovljen ExternalId: {key}.");
                var fields = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
                foreach (var property in item.GetProperty("fields").EnumerateObject())
                {
                    if (!Fields.ContainsKey(property.Name) || !fields.TryAdd(property.Name, property.Value.Clone()))
                        throw Invalid($"Nepoznato ili ponovljeno polje: {property.Name}.");
                    if (property.Name == "location") { ReadLocation(property.Value); continue; }
                    if (property.Value.ValueKind is not (JsonValueKind.Null or JsonValueKind.String)) throw Invalid($"Očekivan tekst: {property.Name}.");
                    var value = property.Value.ValueKind == JsonValueKind.Null ? null : property.Value.GetString();
                    if (value is not null && (string.IsNullOrWhiteSpace(value) || value.Length > 10_000)) throw Invalid($"Neispravna vrijednost: {property.Name}.");
                    if (property.Name == "name" && value is null) throw Invalid("Naziv je obavezan.");
                    if (property.Name == "category" && value is not ("restaurant" or "attraction")) throw Invalid("Nepoznata kategorija.");
                    if (property.Name == "priceLevel" && value is not (null or "budget" or "standard" or "premium")) throw Invalid("Nepoznat cjenovni nivo.");
                    if (property.Name is "website" or "imageUrl" or "imageSourceUrl" && value is not null && OverpassPlacesProvider.SafeUrl(value) is null)
                        throw Invalid($"Dozvoljeni su samo HTTP(S) URL-ovi: {property.Name}.");
                }
                records.Add(new(key, destinations, externalId, fields));
            }
            return records;
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        { throw Invalid($"Neispravan JSON: {exception.Message}"); }
    }

    private static Point ReadLocation(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object) throw Invalid("Location mora biti objekat.");
        var properties = value.EnumerateObject().ToArray();
        if (properties.Length != 2 || properties.Select(p => p.Name).Distinct().Count() != 2
            || properties.Any(p => p.Name is not ("latitude" or "longitude"))) throw Invalid("Location zahtijeva latitude i longitude.");
        var latitude = value.GetProperty("latitude").GetDouble();
        var longitude = value.GetProperty("longitude").GetDouble();
        if (!double.IsFinite(latitude) || !double.IsFinite(longitude) || Math.Abs(latitude) > 90 || Math.Abs(longitude) > 180)
            throw Invalid("Neispravne koordinate.");
        return new Point(longitude, latitude) { SRID = 4326 };
    }

    private static System.ComponentModel.DataAnnotations.ValidationException Invalid(string message) => new($"manual-places.json: {message}");
    [GeneratedRegex("^(node|way|relation)/[1-9][0-9]*$", RegexOptions.CultureInvariant)]
    private static partial Regex OsmId();
}
