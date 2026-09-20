using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using TravelPlanner.Application.DTOs;
using TravelPlanner.Application.Interfaces;

namespace TravelPlanner.Infrastructure.Providers;

public sealed partial class WikimediaCommonsImageProvider(IHttpClientFactory httpClientFactory, IOptions<WikimediaCommonsOptions> options) : IImageProvider
{
    public const string ClientName = "WikimediaCommons";

    public async Task<DestinationImageResult> GetImageAsync(string? imageName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(imageName))
            return DestinationImageResult.Success(null);

        try
        {
            var client = httpClientFactory.CreateClient(ClientName);
            var title = Uri.EscapeDataString($"File:{imageName.Trim()}");
            var response = await client.GetAsync($"{options.Value.BaseUrl}w/api.php?action=query&format=json&prop=imageinfo&iiprop=url%7Cextmetadata&titles={title}", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return DestinationImageResult.Success(null);
            response.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
            var pages = document.RootElement.GetProperty("query").GetProperty("pages");
            var page = pages.EnumerateObject().Select(item => item.Value).FirstOrDefault();
            if (page.ValueKind == JsonValueKind.Undefined || !page.TryGetProperty("imageinfo", out var imageInfoArray))
                return DestinationImageResult.Success(null);
            var imageInfo = imageInfoArray[0];
            var metadata = imageInfo.TryGetProperty("extmetadata", out var value) ? value : default;
            return DestinationImageResult.Success(new DestinationImage(
                imageInfo.GetProperty("url").GetString()!,
                Metadata(metadata, "Artist"),
                Metadata(metadata, "LicenseShortName"),
                imageInfo.TryGetProperty("descriptionurl", out var source) ? source.GetString() : null));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return DestinationImageResult.Failure("Wikimedia Commons request timed out.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return DestinationImageResult.Failure("Wikimedia Commons image could not be loaded.");
        }
    }

    private static string? Metadata(JsonElement metadata, string name)
    {
        if (metadata.ValueKind != JsonValueKind.Object || !metadata.TryGetProperty(name, out var item) || !item.TryGetProperty("value", out var value))
            return null;
        return WebUtility.HtmlDecode(HtmlPattern().Replace(value.GetString() ?? string.Empty, string.Empty)).Trim();
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlPattern();
}
