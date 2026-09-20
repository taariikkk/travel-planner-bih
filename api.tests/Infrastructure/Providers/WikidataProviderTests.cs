using System.Net;
using Microsoft.Extensions.Options;
using TravelPlanner.Infrastructure.Providers;
using Xunit;

namespace TravelPlanner.Api.Tests.Infrastructure.Providers;

public sealed class WikidataProviderTests
{
    [Fact]
    public async Task Get_by_qid_maps_a_BiH_entity_from_fixture_data()
    {
        var provider = CreateProvider(new Dictionary<string, string>
        {
            ["wiki/Special:EntityData/Q111.json"] = Fixture("Q111.json"),
            ["wiki/Special:EntityData/Q300.json"] = Fixture("Q300.json")
        });

        var result = await provider.GetByQidAsync("Q111", default);

        Assert.True(result.IsSuccess);
        var destination = Assert.IsType<TravelPlanner.Application.DTOs.DestinationData>(result.Data);
        Assert.Equal("Test Grad", destination.NameBs);
        Assert.Equal("Test City", destination.NameEn);
        Assert.Equal(43.859, destination.Latitude);
        Assert.Equal(18.429, destination.Longitude);
        Assert.Equal("grad", destination.Type);
        Assert.Equal(518, destination.ElevationM);
        Assert.Equal(275524, destination.Population);
        Assert.Equal("Sarajevo.jpg", destination.ImageName);
        Assert.Equal("Test Grad", destination.BosnianWikipediaArticle);
        Assert.Equal("Test City", destination.EnglishWikipediaArticle);
        Assert.Equal("Kanton test", destination.Region!.NameBs);
    }

    [Fact]
    public async Task Search_uses_english_only_when_bosnian_returns_no_BiH_results()
    {
        var handler = new FixtureHandler(new Dictionary<string, string>
        {
            ["w/api.php?action=wbsearchentities&format=json&language=bs&limit=10&search=Sarajevo"] = Fixture("search-empty.json"),
            ["w/api.php?action=wbsearchentities&format=json&language=en&limit=10&search=Sarajevo"] = Fixture("search-bs.json"),
            ["wiki/Special:EntityData/Q111.json"] = Fixture("Q111.json"),
            ["wiki/Special:EntityData/Q300.json"] = Fixture("Q300.json")
        });
        var provider = CreateProvider(handler);

        var result = await provider.SearchAsync("Sarajevo", default);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Destinations);
        Assert.Contains(handler.RequestPaths, path => path.Contains("language=bs", StringComparison.Ordinal));
        Assert.Contains(handler.RequestPaths, path => path.Contains("language=en", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Non_BiH_entities_and_invalid_qids_return_failures_without_throwing()
    {
        var provider = CreateProvider(new Dictionary<string, string>
        {
            ["wiki/Special:EntityData/Q999.json"] = Fixture("Q999.json")
        });

        var invalid = await provider.GetByQidAsync("sarajevo", default);
        var foreign = await provider.GetByQidAsync("Q999", default);

        Assert.False(invalid.IsSuccess);
        Assert.False(foreign.IsSuccess);
        Assert.Null(foreign.Data);
    }

    [Fact]
    public async Task Http_failures_return_unsuccessful_results()
    {
        var provider = CreateProvider(new FixtureHandler(new Dictionary<string, string>()));

        var result = await provider.GetByQidAsync("Q111", default);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    private static WikidataProvider CreateProvider(IReadOnlyDictionary<string, string> responses) => CreateProvider(new FixtureHandler(responses));

    private static WikidataProvider CreateProvider(FixtureHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://www.wikidata.org/"), Timeout = TimeSpan.FromSeconds(1) };
        var factory = new FixtureHttpClientFactory(client);
        var options = Options.Create(new WikidataOptions
        {
            UserAgent = "TravelPlannerBiH.Tests/1.0 (https://example.test)",
            ClassTypeMappings = new Dictionary<string, string> { ["Q515"] = "grad" },
            RegionEntityIds = new HashSet<string> { "Q300" }
        });
        return new WikidataProvider(factory, options);
    }

    private static string Fixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Wikidata", name));

    private sealed class FixtureHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class FixtureHandler(IReadOnlyDictionary<string, string> responses) : HttpMessageHandler
    {
        public List<string> RequestPaths { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.PathAndQuery.TrimStart('/');
            RequestPaths.Add(path);
            return Task.FromResult(responses.TryGetValue(path, out var response)
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(response) }
                : new HttpResponseMessage(HttpStatusCode.InternalServerError));
        }
    }
}
