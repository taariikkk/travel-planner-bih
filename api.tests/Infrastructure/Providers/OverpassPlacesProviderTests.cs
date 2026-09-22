using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging.Abstractions;
using TravelPlanner.Application.DTOs;
using TravelPlanner.Infrastructure.Providers;
using Xunit;

namespace TravelPlanner.Api.Tests.Infrastructure.Providers;

public sealed class OverpassPlacesProviderTests
{
    [Fact]
    public void Query_limits_to_BiH_and_requested_radius_and_includes_all_element_types()
    {
        var query = OverpassPlacesProvider.BuildQuery(new(43.3, 17.8, 10_000), 10);
        Assert.Contains("[timeout:10]", query);
        Assert.Contains("\"ISO3166-1\"=\"BA\"", query);
        Assert.Contains("(around:10000,43.3,17.8)(area.bih)", query);
        Assert.Contains("nwr[\"amenity\"=\"restaurant\"]", query);
        Assert.Contains("cave_entrance", query);
        Assert.Contains("out center;", query);
    }

    [Fact]
    public void Parses_nodes_ways_relations_languages_contact_and_skips_invalid_places()
    {
        var result = OverpassPlacesProvider.Parse("""
            {"elements":[
              {"type":"node","id":1,"lat":43,"lon":18,"tags":{"name":"Restoran","amenity":"restaurant","tourism":"museum","description":"Original","description:bs":"Opis","description:en":"Description","addr:street":"Ulica","addr:housenumber":"2","addr:city":"Grad","cuisine":"regional","website":"javascript:bad","contact:website":"https://example.org","contact:phone":"123"}},
              {"type":"way","id":1,"center":{"lat":44,"lon":17},"tags":{"name":"Muzej","tourism":"museum"}},
              {"type":"relation","id":1,"center":{"lat":44,"lon":17},"tags":{"name":"Vrh","natural":"peak"}},
              {"type":"node","id":4,"lat":91,"lon":18,"tags":{"name":"Bad","amenity":"restaurant"}},
              {"type":"node","id":5,"lat":43,"lon":18,"tags":{"amenity":"restaurant"}},
              {"type":"way","id":6,"tags":{"name":"No center","tourism":"museum"}}
            ]}
            """);
        Assert.Equal(3, result.Places.Count);
        Assert.Equal(3, result.SkippedCount);
        var node = result.Places[0];
        Assert.Equal("node/1", node.ExternalId);
        Assert.Equal("restaurant", node.Category);
        Assert.Equal("Opis", node.DescriptionBs);
        Assert.Equal("Description", node.DescriptionEn);
        Assert.Equal("und", node.DescriptionLanguage);
        Assert.Equal("Ulica 2, Grad", node.Address);
        Assert.Equal("https://example.org/", node.Website);
        Assert.Equal("123", node.Phone);
        Assert.Equal("way/1", result.Places[1].ExternalId);
        Assert.Equal("relation/1", result.Places[2].ExternalId);
    }

    [Theory]
    [InlineData("bad json")]
    [InlineData("{\"remark\":\"runtime timeout\",\"elements\":[]}")]
    [InlineData("{}")]
    public void Rejects_partial_and_malformed_answers(string json) =>
        Assert.Throws<PlacesProviderException>(() => OverpassPlacesProvider.Parse(json));

    [Fact]
    public void Empty_results_are_valid() => Assert.Empty(OverpassPlacesProvider.Parse("{\"elements\":[]}").Places);

    [Theory]
    [InlineData(500)]
    [InlineData(503)]
    public async Task Server_errors_try_exactly_one_mirror(int status)
    {
        var handler = new Handler((index, _) => Task.FromResult(index == 0 ? new HttpResponseMessage((HttpStatusCode)status) : Ok()));
        var gate = new Gate();
        await Provider(handler, gate).GetPlacesAsync(new(43, 18, 10_000), default);
        Assert.Equal(2, handler.Urls.Count);
        Assert.Contains("private.coffee", handler.Urls[1]);
        Assert.Equal(new[] { false, true }, gate.Fallbacks);
        Assert.Equal(2, gate.Releases);
        Assert.Contains("data=", handler.Body);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Network_errors_and_timeouts_use_mirror(bool timeout)
    {
        var handler = new Handler((index, _) => index == 0
            ? Task.FromException<HttpResponseMessage>(timeout ? new TaskCanceledException() : new HttpRequestException())
            : Task.FromResult(Ok()));
        await Provider(handler, new Gate()).GetPlacesAsync(new(43, 18, 10_000), default);
        Assert.Equal(2, handler.Urls.Count);
    }

    [Theory]
    [InlineData(429)]
    [InlineData(406)]
    public async Task Rate_limit_respects_retry_after_without_mirror(int status)
    {
        var handler = new Handler((_, _) =>
        {
            var response = new HttpResponseMessage((HttpStatusCode)status);
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromMinutes(2));
            return Task.FromResult(response);
        });
        var gate = new Gate();
        var before = DateTimeOffset.UtcNow;
        var error = await Assert.ThrowsAsync<PlacesProviderException>(() => Provider(handler, gate).GetPlacesAsync(new(43, 18, 10_000), default));
        Assert.True(error.RetryAt >= before.AddMinutes(2));
        Assert.Single(handler.Urls);
        Assert.Equal(error.RetryAt, gate.RetryAt);
    }

    [Fact]
    public async Task Caller_cancellation_does_not_fallback_and_releases_gate()
    {
        using var cancellation = new CancellationTokenSource();
        var handler = new Handler((_, _) => { cancellation.Cancel(); return Task.FromCanceled<HttpResponseMessage>(cancellation.Token); });
        var gate = new Gate();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Provider(handler, gate).GetPlacesAsync(new(43, 18, 10_000), cancellation.Token));
        Assert.Single(handler.Urls);
        Assert.Equal(1, gate.Releases);
    }

    [Fact]
    public async Task Invalid_response_does_not_fallback()
    {
        var handler = new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"remark\":\"timeout\",\"elements\":[]}") }));
        await Assert.ThrowsAsync<PlacesProviderException>(() => Provider(handler, new Gate()).GetPlacesAsync(new(43, 18, 10_000), default));
        Assert.Single(handler.Urls);
    }

    private static HttpResponseMessage Ok() => new(HttpStatusCode.OK) { Content = new StringContent("{\"elements\":[]}") };
    private static OverpassPlacesProvider Provider(Handler handler, Gate gate) => new(new Factory(handler), new(), gate, TimeProvider.System, NullLogger<OverpassPlacesProvider>.Instance);
    private sealed class Factory(Handler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }
    private sealed class Handler(Func<int, CancellationToken, Task<HttpResponseMessage>> action) : HttpMessageHandler
    {
        public List<string> Urls { get; } = [];
        public string? Body { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Urls.Add(request.RequestUri!.ToString());
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return await action(Urls.Count - 1, cancellationToken);
        }
    }
    private sealed class Gate : IOverpassRequestGate
    {
        public List<bool> Fallbacks { get; } = [];
        public int Releases { get; private set; }
        public DateTimeOffset? RetryAt { get; private set; }
        public Task<Guid> AcquireAsync(bool fallback, CancellationToken cancellationToken) { Fallbacks.Add(fallback); return Task.FromResult(Guid.NewGuid()); }
        public Task ReleaseAsync(Guid token, DateTimeOffset? retryAfter, CancellationToken cancellationToken) { Releases++; RetryAt = retryAfter; return Task.CompletedTask; }
    }
}
