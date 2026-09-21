using System.Net;
using Microsoft.Extensions.Options;
using TravelPlanner.Application.DTOs;
using TravelPlanner.Infrastructure.Providers;
using Xunit;

namespace TravelPlanner.Api.Tests.Infrastructure.Providers;

public sealed class MapboxDirectionsProviderTests
{
    [Fact]
    public async Task Builds_the_selected_profile_request_and_maps_geojson_route_data()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, """
            {
              "code": "Ok",
              "routes": [{
                "distance": 128450.5,
                "duration": 7200.25,
                "geometry": { "type": "LineString", "coordinates": [[18.4131,43.8563],[17.8150,43.3373]] }
              }]
            }
            """);
        var provider = CreateProvider(handler);

        var result = await provider.GetRouteAsync(
            [new(43.8563, 18.4131), new(43.3373, 17.8150)], RouteTravelMode.Walking, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(128450.5, result.Route!.DistanceMeters);
        Assert.Equal(7200.25, result.Route.DurationSeconds);
        Assert.Equal(new RouteCoordinate(43.3373, 17.8150), result.Route.Geometry[1]);
        Assert.Contains("/directions/v5/mapbox/walking/18.4131,43.8563;17.815,43.3373", handler.RequestUri!.AbsoluteUri);
        Assert.Contains("geometries=geojson", handler.RequestUri.Query);
        Assert.Contains("overview=full", handler.RequestUri.Query);
        Assert.Contains("access_token=test-token", handler.RequestUri.Query);
    }

    [Fact]
    public async Task Validates_waypoint_count_coordinates_profile_and_configuration_without_an_http_call()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, "{}");
        var provider = CreateProvider(handler);

        var tooFew = await provider.GetRouteAsync([new(43.8, 18.4)], RouteTravelMode.Driving, default);
        var tooMany = await provider.GetRouteAsync(Enumerable.Range(0, 26).Select(index => new RouteCoordinate(43.8, 18.4 + index * 0.001)).ToArray(), RouteTravelMode.Driving, default);
        var invalidCoordinate = await provider.GetRouteAsync([new(91, 18.4), new(43.8, 18.5)], RouteTravelMode.Driving, default);
        var invalidProfile = await provider.GetRouteAsync([new(43.8, 18.4), new(43.9, 18.5)], (RouteTravelMode)999, default);
        var missingToken = await CreateProvider(handler, null).GetRouteAsync([new(43.8, 18.4), new(43.9, 18.5)], RouteTravelMode.Driving, default);

        Assert.All([tooFew, tooMany, invalidCoordinate, invalidProfile, missingToken], result => Assert.False(result.IsSuccess));
        Assert.Null(handler.RequestUri);
    }

    [Theory]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Converts_http_failures_to_controlled_results(HttpStatusCode statusCode)
    {
        var result = await CreateProvider(new RecordingHandler(statusCode, "{}"))
            .GetRouteAsync([new(43.8, 18.4), new(43.9, 18.5)], RouteTravelMode.Cycling, default);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task Converts_Mapbox_no_route_and_invalid_json_to_controlled_results()
    {
        var noRoute = await CreateProvider(new RecordingHandler(HttpStatusCode.OK, """{ "code": "NoRoute", "routes": [] }"""))
            .GetRouteAsync([new(43.8, 18.4), new(43.9, 18.5)], RouteTravelMode.Driving, default);
        var invalidJson = await CreateProvider(new RecordingHandler(HttpStatusCode.OK, "not-json"))
            .GetRouteAsync([new(43.8, 18.4), new(43.9, 18.5)], RouteTravelMode.Driving, default);

        Assert.False(noRoute.IsSuccess);
        Assert.False(invalidJson.IsSuccess);
    }

    [Fact]
    public async Task Distinguishes_a_timeout_from_caller_cancellation()
    {
        var timeout = await CreateProvider(new CancellationHandler())
            .GetRouteAsync([new(43.8, 18.4), new(43.9, 18.5)], RouteTravelMode.Driving, default);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CreateProvider(new RecordingHandler(HttpStatusCode.OK, "{}"))
            .GetRouteAsync([new(43.8, 18.4), new(43.9, 18.5)], RouteTravelMode.Driving, cancellation.Token));
        Assert.False(timeout.IsSuccess);
        Assert.Contains("timed out", timeout.Error, StringComparison.OrdinalIgnoreCase);
    }

    private static MapboxDirectionsProvider CreateProvider(HttpMessageHandler handler, string? accessToken = "test-token")
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.mapbox.com/") };
        return new MapboxDirectionsProvider(new ClientFactory(client), Options.Create(new MapboxOptions
        {
            BaseUrl = "https://api.mapbox.com/",
            AccessToken = accessToken,
            TimeoutSeconds = 10
        }));
    }

    private sealed class ClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class RecordingHandler(HttpStatusCode statusCode, string content) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(statusCode) { Content = new StringContent(content) });
        }
    }

    private sealed class CancellationHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new OperationCanceledException();
    }
}
