using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TravelPlanner.Application.DTOs;
using TravelPlanner.Application.Interfaces;

namespace TravelPlanner.Infrastructure.Providers;

public sealed class WikipediaSummaryProvider(IHttpClientFactory httpClientFactory, IOptions<WikipediaOptions> options) : IWikipediaSummaryProvider
{
    public const string ClientName = "Wikipedia";
    private const string License = "CC BY-SA";

    public async Task<WikipediaSummaryResult> GetSummariesAsync(string? bosnianArticle, string? englishArticle, CancellationToken cancellationToken)
    {
        try
        {
            var bosnian = await GetSummaryAsync("bs", bosnianArticle, cancellationToken);
            var english = await GetSummaryAsync("en", englishArticle, cancellationToken);
            return WikipediaSummaryResult.Success(bosnian, english);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return WikipediaSummaryResult.Failure("Wikipedia request timed out.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            return WikipediaSummaryResult.Failure("Wikipedia request failed.");
        }
        catch (JsonException)
        {
            return WikipediaSummaryResult.Failure("Wikipedia returned an invalid response.");
        }
        catch (Exception)
        {
            return WikipediaSummaryResult.Failure("Wikipedia data could not be processed.");
        }
    }

    private async Task<WikipediaSummary?> GetSummaryAsync(string language, string? article, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(article))
            return null;

        var client = httpClientFactory.CreateClient(ClientName);
        var baseUrl = string.Format(options.Value.ApiUrlTemplate, language);
        var title = Uri.EscapeDataString(article.Trim().Replace(' ', '_'));
        var response = await client.GetAsync($"{baseUrl}page/summary/{title}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        if ((int)response.StatusCode is >= 300 and < 400)
            return new WikipediaSummary(string.Empty, response.Headers.Location?.ToString() ?? string.Empty, License, false);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
        var root = document.RootElement;
        var text = root.TryGetProperty("extract", out var extract) ? extract.GetString()?.Trim() ?? string.Empty : string.Empty;
        var articleUrl = root.TryGetProperty("content_urls", out var contentUrls)
            && contentUrls.TryGetProperty("desktop", out var desktop)
            && desktop.TryGetProperty("page", out var page)
            ? page.GetString() ?? string.Empty
            : string.Empty;
        var isDisambiguation = root.TryGetProperty("type", out var type) && type.GetString() == "disambiguation";
        var isRedirect = root.TryGetProperty("redirect", out var redirect) && redirect.ValueKind == JsonValueKind.True;
        var isSufficient = !isDisambiguation && !isRedirect && text.Length >= options.Value.MinimumSummaryLength;
        return new WikipediaSummary(text, articleUrl, License, isSufficient);
    }
}
