using System.Net;
using Microsoft.Extensions.Options;
using TravelPlanner.Infrastructure.Providers;
using Xunit;

namespace TravelPlanner.Api.Tests.Infrastructure.Providers;

public sealed class WikipediaSummaryProviderTests
{
    [Fact]
    public async Task Maps_each_language_independently_and_marks_short_text_as_insufficient()
    {
        var handler = new FixtureHandler(new Dictionary<string, FixtureResponse>
        {
            ["bs.wikipedia.org/api/rest_v1/page/summary/Test_bs"] = new(HttpStatusCode.OK, Fixture("summary-standard.json")),
            ["en.wikipedia.org/api/rest_v1/page/summary/Test_en"] = new(HttpStatusCode.OK, Fixture("summary-short.json"))
        });
        var provider = CreateProvider(handler, 80);

        var result = await provider.GetSummariesAsync("Test bs", "Test en", default);

        Assert.True(result.IsSuccess);
        Assert.True(result.Summaries!.Bosnian!.IsSufficient);
        Assert.False(result.Summaries.English!.IsSufficient);
        Assert.Equal("CC BY-SA", result.Summaries.Bosnian.License);
        Assert.Equal("https://bs.wikipedia.org/wiki/Test", result.Summaries.Bosnian.ArticleUrl);
    }

    [Fact]
    public async Task Missing_article_name_or_not_found_article_returns_null_for_that_language()
    {
        var handler = new FixtureHandler(new Dictionary<string, FixtureResponse>
        {
            ["en.wikipedia.org/api/rest_v1/page/summary/Missing"] = new(HttpStatusCode.NotFound, "")
        });
        var provider = CreateProvider(handler);

        var result = await provider.GetSummariesAsync(null, "Missing", default);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Summaries!.Bosnian);
        Assert.Null(result.Summaries.English);
        Assert.Single(handler.Requests);
    }

    [Theory]
    [InlineData("summary-disambiguation.json")]
    [InlineData("summary-redirect.json")]
    public async Task Disambiguation_and_redirect_pages_are_insufficient(string fixture)
    {
        var provider = CreateProvider(new FixtureHandler(new Dictionary<string, FixtureResponse>
        {
            ["bs.wikipedia.org/api/rest_v1/page/summary/Test"] = new(HttpStatusCode.OK, Fixture(fixture))
        }), 20);

        var result = await provider.GetSummariesAsync("Test", null, default);

        Assert.True(result.IsSuccess);
        Assert.False(result.Summaries!.Bosnian!.IsSufficient);
    }

    [Fact]
    public async Task Http_errors_return_failure_instead_of_throwing()
    {
        var provider = CreateProvider(new FixtureHandler(new Dictionary<string, FixtureResponse>()));

        var result = await provider.GetSummariesAsync("Test", null, default);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    private static WikipediaSummaryProvider CreateProvider(FixtureHandler handler, int minimumLength = 80)
    {
        var client = new HttpClient(handler);
        return new WikipediaSummaryProvider(new FixtureHttpClientFactory(client), Options.Create(new WikipediaOptions
        {
            ApiUrlTemplate = "https://{0}.wikipedia.org/api/rest_v1/",
            MinimumSummaryLength = minimumLength
        }));
    }

    private static string Fixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Wikipedia", name));

    private sealed class FixtureHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed record FixtureResponse(HttpStatusCode StatusCode, string Content);

    private sealed class FixtureHandler(IReadOnlyDictionary<string, FixtureResponse> responses) : HttpMessageHandler
    {
        public List<string> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var key = $"{request.RequestUri!.Host}/{request.RequestUri.AbsolutePath.TrimStart('/')}";
            Requests.Add(key);
            var response = responses.GetValueOrDefault(key, new FixtureResponse(HttpStatusCode.InternalServerError, ""));
            return Task.FromResult(new HttpResponseMessage(response.StatusCode) { Content = new StringContent(response.Content) });
        }
    }
}
