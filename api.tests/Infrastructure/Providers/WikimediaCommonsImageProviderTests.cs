using System.Net;
using Microsoft.Extensions.Options;
using TravelPlanner.Infrastructure.Providers;
using Xunit;

namespace TravelPlanner.Api.Tests.Infrastructure.Providers;

public sealed class WikimediaCommonsImageProviderTests
{
    [Fact]
    public async Task Maps_image_url_and_sanitized_attribution_from_a_fixture_response()
    {
        var provider = CreateProvider(new FixtureHandler(HttpStatusCode.OK, """
            { "query": { "pages": { "1": { "imageinfo": [{
              "url": "https://upload.wikimedia.org/test.jpg",
              "descriptionurl": "https://commons.wikimedia.org/wiki/File:Test.jpg",
              "extmetadata": { "Artist": { "value": "<b>Test Author</b>" }, "LicenseShortName": { "value": "CC BY-SA 4.0" } }
            }] } } } }
            """));

        var result = await provider.GetImageAsync("Test.jpg", default);

        Assert.True(result.IsSuccess);
        Assert.Equal("https://upload.wikimedia.org/test.jpg", result.Image!.Url);
        Assert.Equal("Test Author", result.Image.Author);
        Assert.Equal("CC BY-SA 4.0", result.Image.License);
    }

    [Fact]
    public async Task Missing_file_and_http_failure_are_handled_without_throwing()
    {
        var missing = await CreateProvider(new FixtureHandler(HttpStatusCode.NotFound, "")).GetImageAsync("Missing.jpg", default);
        var failed = await CreateProvider(new FixtureHandler(HttpStatusCode.InternalServerError, "")).GetImageAsync("Broken.jpg", default);

        Assert.True(missing.IsSuccess);
        Assert.Null(missing.Image);
        Assert.False(failed.IsSuccess);
    }

    private static WikimediaCommonsImageProvider CreateProvider(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler);
        return new WikimediaCommonsImageProvider(new ClientFactory(client), Options.Create(new WikimediaCommonsOptions
        {
            BaseUrl = "https://commons.wikimedia.org/"
        }));
    }

    private sealed class ClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class FixtureHandler(HttpStatusCode statusCode, string content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode) { Content = new StringContent(content) });
    }
}
