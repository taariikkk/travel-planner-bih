using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TravelPlanner.Application.DTOs;
using TravelPlanner.Application.Interfaces;

namespace TravelPlanner.Infrastructure.Providers;

public sealed class OverpassPlacesProvider(IHttpClientFactory clients, OverpassOptions options,
    IOverpassRequestGate gate, TimeProvider clock, ILogger<OverpassPlacesProvider> logger) : IPlacesProvider
{
    public const string ClientName = "Overpass";

    public async Task<PlacesData> GetPlacesAsync(PlacesQuery query, CancellationToken cancellationToken)
    {
        var urls = string.IsNullOrWhiteSpace(options.MirrorUrl) ? new[] { options.PrimaryUrl } : new[] { options.PrimaryUrl, options.MirrorUrl };
        for (var attempt = 0; attempt < urls.Length; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var token = await gate.AcquireAsync(attempt > 0, cancellationToken);
            DateTimeOffset? retryAt = null;
            var timer = Stopwatch.StartNew();
            try
            {
                using var client = clients.CreateClient(ClientName);
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(options.HttpTimeoutSeconds));
                using var content = new FormUrlEncodedContent(new Dictionary<string, string> { ["data"] = BuildQuery(query, options.QueryTimeoutSeconds) });
                using var response = await client.PostAsync(urls[attempt], content, timeout.Token);
                if (response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.NotAcceptable)
                {
                    retryAt = clock.GetUtcNow().AddSeconds(30);
                    var header = response.Headers.RetryAfter;
                    var requested = header?.Date ?? (header?.Delta is { } delta ? clock.GetUtcNow() + delta : (DateTimeOffset?)null);
                    if (requested > retryAt) retryAt = requested;
                    throw new PlacesProviderException($"Overpass returned {(int)response.StatusCode}.", retryAt);
                }
                if ((int)response.StatusCode >= 500)
                {
                    logger.LogWarning("Overpass {Server} returned {Status}; attempt {Attempt}", urls[attempt], response.StatusCode, attempt + 1);
                    if (attempt + 1 < urls.Length) continue;
                    throw new PlacesProviderException("Overpass servers unavailable.");
                }
                if (!response.IsSuccessStatusCode)
                    throw new PlacesProviderException($"Overpass returned {(int)response.StatusCode}.");
                var result = Parse(await response.Content.ReadAsStringAsync(timeout.Token));
                logger.LogInformation("Overpass {Server}: {Count} places, {Skipped} skipped, {ElapsedMs} ms",
                    urls[attempt], result.Places.Count, result.SkippedCount, timer.ElapsedMilliseconds);
                return result;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("Overpass {Server} timed out after {ElapsedMs} ms", urls[attempt], timer.ElapsedMilliseconds);
                if (attempt + 1 == urls.Length) throw new PlacesProviderException("Overpass timeout.");
            }
            catch (HttpRequestException exception)
            {
                logger.LogWarning(exception, "Overpass {Server} network failure", urls[attempt]);
                if (attempt + 1 == urls.Length) throw new PlacesProviderException("Overpass network failure.");
            }
            catch (PlacesProviderException exception)
            {
                logger.LogWarning("Overpass {Server}: {Reason}", urls[attempt], exception.Message);
                throw;
            }
            finally
            {
                using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                await gate.ReleaseAsync(token, retryAt, cleanup.Token);
            }
        }
        throw new PlacesProviderException("Overpass unavailable.");
    }

    public static string BuildQuery(PlacesQuery query, int timeoutSeconds)
    {
        if (!double.IsFinite(query.Latitude) || !double.IsFinite(query.Longitude)
            || Math.Abs(query.Latitude) > 90 || Math.Abs(query.Longitude) > 180 || query.RadiusMeters is <= 0 or > 50_000)
            throw new ArgumentOutOfRangeException(nameof(query));
        var around = string.Create(CultureInfo.InvariantCulture, $"(around:{query.RadiusMeters},{query.Latitude:R},{query.Longitude:R})(area.bih)");
        return $$"""
            [out:json][timeout:{{timeoutSeconds}}];
            area["ISO3166-1"="BA"]["admin_level"="2"]->.bih;
            (
              nwr["amenity"="restaurant"]["name"]{{around}};
              nwr["tourism"~"^(attraction|museum|viewpoint|gallery|zoo)$"]["name"]{{around}};
              nwr["historic"~"^(castle|ruins|monument|memorial)$"]["name"]{{around}};
              nwr["natural"~"^(peak|waterfall|cave_entrance)$"]["name"]{{around}};
            );
            out center;
            """;
    }

    public static PlacesData Parse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.TryGetProperty("remark", out _)
                || !root.TryGetProperty("elements", out var elements) || elements.ValueKind != JsonValueKind.Array)
                throw new PlacesProviderException("Incomplete Overpass response.");
            var places = new Dictionary<string, PlaceData>(StringComparer.Ordinal);
            var skipped = 0;
            foreach (var element in elements.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object
                    || !element.TryGetProperty("tags", out var tags) || tags.ValueKind != JsonValueKind.Object
                    || !element.TryGetProperty("type", out var typeNode) || typeNode.ValueKind != JsonValueKind.String
                    || typeNode.GetString() is not ("node" or "way" or "relation")
                    || !element.TryGetProperty("id", out var id) || !id.TryGetInt64(out var osmId) || osmId <= 0)
                { skipped++; continue; }
                string? Tag(string name) => tags.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(value.GetString()) ? value.GetString()!.Trim() : null;
                var name = Tag("name");
                var category = Tag("amenity") == "restaurant" ? "restaurant"
                    : Tag("tourism") is "attraction" or "museum" or "viewpoint" or "gallery" or "zoo"
                    || Tag("historic") is "castle" or "ruins" or "monument" or "memorial"
                    || Tag("natural") is "peak" or "waterfall" or "cave_entrance" ? "attraction" : null;
                var coords = typeNode.GetString() == "node" ? element : element.TryGetProperty("center", out var center) ? center : default;
                if (name is null || category is null || coords.ValueKind != JsonValueKind.Object
                    || !coords.TryGetProperty("lat", out var lat) || !lat.TryGetDouble(out var latitude)
                    || !coords.TryGetProperty("lon", out var lon) || !lon.TryGetDouble(out var longitude)
                    || !double.IsFinite(latitude) || !double.IsFinite(longitude) || Math.Abs(latitude) > 90 || Math.Abs(longitude) > 180)
                { skipped++; continue; }
                var externalId = $"{typeNode.GetString()}/{osmId}";
                var street = string.Join(" ", new[] { Tag("addr:street"), Tag("addr:housenumber") }.Where(v => v is not null));
                var address = Tag("addr:full") ?? string.Join(", ", new[] { street, Tag("addr:postcode"), Tag("addr:city") }.Where(v => !string.IsNullOrEmpty(v)));
                // Unqualified OSM description has no reliable language; never guess it from the country.
                places[externalId] = new(externalId, name, category, latitude, longitude,
                    Tag("description:bs"), Tag("description:en"), Tag("description"), Tag("description") is null ? null : "und",
                    string.IsNullOrEmpty(address) ? null : address, Tag("cuisine"),
                    SafeUrl(Tag("website")) ?? SafeUrl(Tag("contact:website")), Tag("phone") ?? Tag("contact:phone"), Tag("email") ?? Tag("contact:email"));
            }
            return new(places.Values.ToArray(), skipped);
        }
        catch (JsonException) { throw new PlacesProviderException("Invalid Overpass JSON."); }
        catch (InvalidOperationException) { throw new PlacesProviderException("Invalid Overpass element types."); }
    }

    public static string? SafeUrl(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && uri.Scheme is "https" or "http" && string.IsNullOrEmpty(uri.UserInfo) ? uri.AbsoluteUri : null;
}
