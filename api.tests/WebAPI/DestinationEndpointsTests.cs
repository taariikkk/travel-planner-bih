using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TravelPlanner.Api.Domain.Entities;
using TravelPlanner.Application.Interfaces;
using TravelPlanner.Application.DTOs;
using Xunit;

namespace TravelPlanner.Api.Tests.WebAPI;

public sealed class DestinationEndpointsTests
{
    [Fact]
    public async Task Details_is_public_and_returns_coordinates_places_and_localized_content()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/destinations/mostar?language=en");
        var body = await response.Content.ReadFromJsonAsync<DestinationDetailsResponse>();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("mostar", body.Slug);
        Assert.Equal("English description", body.Description);
        Assert.Equal(43.3373, body.Latitude);
        Assert.Equal(17.815, body.Longitude);
        Assert.Equal(2, body.SuggestedStayMinDays);
        Assert.Equal("restaurant", Assert.Single(body.Places).Category);
        Assert.Equal(518, body.ElevationMeters);
        Assert.Equal("Test author", body.ImageAttribution!.Author);
        Assert.Equal("CC BY-SA", body.DescriptionAttribution!.License);
    }

    [Theory]
    [InlineData("/api/destinations/missing", HttpStatusCode.NotFound)]
    [InlineData("/api/destinations/mostar?language=de", HttpStatusCode.BadRequest)]
    public async Task Details_handles_unknown_slug_and_unsupported_language(string url, HttpStatusCode expected)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        Assert.Equal(expected, (await client.GetAsync(url)).StatusCode);
    }

    [Fact]
    public async Task Details_defaults_to_bosnian_and_preserves_empty_places()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var body = await client.GetFromJsonAsync<DestinationDetailsResponse>("/api/destinations/trebinje");
        Assert.NotNull(body);
        Assert.Equal("Opis", body.Description);
        Assert.Empty(body.Places);
    }

    [Fact]
    public async Task Recommend_requires_a_token()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/destinations/recommend", new { budgetTier = "standard", travelDays = 3, season = "summer", language = "bs" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Import_is_public_and_validates_qid()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var valid = await client.PostAsJsonAsync("/api/destinations/import", new { qid = "Q123" });
        var invalid = await client.PostAsJsonAsync("/api/destinations/import", new { qid = "Sarajevo" });

        Assert.Equal(HttpStatusCode.OK, valid.StatusCode);
        Assert.Equal("test-grad", (await valid.Content.ReadFromJsonAsync<DestinationImportResponse>())!.Slug);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact]
    public async Task Import_is_rate_limited_per_client_ip()
    {
        using var factory = new ApiFactory(importPermitLimit: 1);
        using var client = factory.CreateClient();

        var first = await client.PostAsJsonAsync("/api/destinations/import", new { qid = "Q123" });
        var second = await client.PostAsJsonAsync("/api/destinations/import", new { qid = "Q124" });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
    }

    [Fact]
    public async Task Search_is_public_and_returns_filters_and_results()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/search?q=Bjelasnica&type=planina&region=Sarajevski%20kanton&language=bs");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("bjelasnica", body.RootElement.GetProperty("items")[0].GetProperty("slug").GetString());
        Assert.Equal("planina", body.RootElement.GetProperty("filters").GetProperty("types")[0].GetString());
        Assert.True(body.RootElement.GetProperty("usedFallback").GetBoolean());
    }

    [Theory]
    [InlineData("/api/search?language=de")]
    [InlineData("/api/search?q=aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public async Task Search_validates_language_and_query_length(string url)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(url)).StatusCode);
    }

    [Fact]
    public async Task Search_is_rate_limited_per_client_ip()
    {
        using var factory = new ApiFactory(searchPermitLimit: 1);
        using var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/search?q=Mostar")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.GetAsync("/api/search?q=Jajce")).StatusCode);
    }

    [Fact]
    public async Task Recommend_validates_request_and_returns_at_most_five_localized_results()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await RegisterAsync(client));

        var invalid = await client.PostAsJsonAsync("/api/destinations/recommend", new { budgetTier = "standard", travelDays = 15, season = "summer", language = "bs" });
        var missingLanguage = await client.PostAsJsonAsync("/api/destinations/recommend", new { budgetTier = "standard", travelDays = 3, season = "summer", language = (string?)null });
        var response = await client.PostAsJsonAsync("/api/destinations/recommend", new { budgetTier = "standard", travelDays = 3, season = "summer", language = "en" });
        using var results = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, missingLanguage.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(5, results.RootElement.GetArrayLength());
        Assert.False(string.IsNullOrWhiteSpace(results.RootElement[0].GetProperty("slug").GetString()));
        Assert.Equal("English description", results.RootElement[0].GetProperty("description").GetString());
        var reasons = results.RootElement[0].GetProperty("reasons");
        Assert.Empty(reasons.GetProperty("matchingTags").EnumerateArray());
        Assert.True(reasons.GetProperty("matchesSeason").GetBoolean());
        Assert.True(reasons.GetProperty("matchesBudget").GetBoolean());
        Assert.True(reasons.GetProperty("matchesDuration").GetBoolean());
    }

    private static async Task<string> RegisterAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new { email = "test@example.com", password = "password123", displayName = "Test" });
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("accessToken").GetString()!;
    }

    private sealed class ApiFactory(int importPermitLimit = 5, int searchPermitLimit = 20) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=travelplanner_test;Username=test;Password=test",
                ["Jwt:Key"] = "test-signing-key-that-is-at-least-thirty-two-characters-long",
                ["Jwt:Issuer"] = "TravelPlanner.Api.Tests",
                ["Jwt:Audience"] = "TravelPlanner.Web.Tests",
                ["RateLimiting:DestinationImportPermitLimit"] = importPermitLimit.ToString(),
                ["RateLimiting:DestinationImportWindowSeconds"] = "60",
                ["RateLimiting:DestinationSearchPermitLimit"] = searchPermitLimit.ToString(),
                ["RateLimiting:DestinationSearchWindowSeconds"] = "60"
            }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IUserRepository>();
                services.RemoveAll<IDestinationRepository>();
                services.AddSingleton<Users>();
                services.AddScoped<IUserRepository>(provider => provider.GetRequiredService<Users>());
                services.AddSingleton<IDestinationRepository>(new Destinations());
                services.RemoveAll<IDestinationDetailsRepository>();
                services.AddSingleton<IDestinationDetailsRepository>(new Details());
                services.RemoveAll<IDestinationImportService>();
                services.AddSingleton<IDestinationImportService>(new Importer());
                services.RemoveAll<IDestinationSearchService>();
                services.AddSingleton<IDestinationSearchService>(new Search());
            });
        }
    }

    private sealed class Importer : IDestinationImportService
    {
        public Task<DestinationImportResult> ImportAsync(string qid, CancellationToken cancellationToken) =>
            Task.FromResult(DestinationImportResult.Success("test-grad"));
    }

    private sealed class Details : IDestinationDetailsRepository
    {
        public Task<DestinationDetailsResponse?> GetBySlugAsync(string slug, string language, CancellationToken cancellationToken)
        {
            DestinationDetailsResponse? result = slug is "mostar" or "trebinje"
                ? new(Guid.NewGuid(), slug, slug == "mostar" ? "Mostar" : "Trebinje", "Hercegovina",
                    language == "en" ? "English description" : "Opis", "maj-septembar", 2, 3,
                    ["historija"], 43.3373, 17.815,
                    slug == "mostar" ? [new(Guid.NewGuid(), "Test restoran", "restaurant", 43.338, 17.816)] : [],
                    ElevationMeters: 518, ImageUrl: "https://images.test/test.jpg",
                    ImageAttribution: new("Test author", "CC BY-SA 4.0", "https://commons.test/Test"),
                    DescriptionAttribution: new("CC BY-SA", "https://wikipedia.test/Test"))
                : null;
            return Task.FromResult(result);
        }
    }

    private sealed class Search : IDestinationSearchService
    {
        public Task<DestinationSearchResponse> SearchAsync(string? query, string? type, string? region, string language, CancellationToken cancellationToken) =>
            Task.FromResult(new DestinationSearchResponse(
                [new(Guid.NewGuid(), "bjelasnica", "Bjelašnica", "planina", "Sarajevski kanton", "Opis", "manual", null, null, null)],
                new(["planina"], ["Sarajevski kanton"]), true));
    }

    private sealed class Users : IUserRepository
    {
        private readonly List<User> users = [];
        public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) => Task.FromResult(users.SingleOrDefault(user => user.Email == normalizedEmail));
        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(users.SingleOrDefault(user => user.Id == id));
        public Task AddAsync(User user, CancellationToken cancellationToken) { users.Add(user); return Task.CompletedTask; }
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class Destinations : IDestinationRepository
    {
        private readonly IReadOnlyList<Destination> destinations = Enumerable.Range(1, 6).Select(index => new Destination
        {
            Id = Guid.NewGuid(), Slug = $"grad-{index}", Name = $"Grad {index}", Region = "BiH", Description = "Opis", BestTimeToVisit = "Ljeto",
            BudgetTier = "standard", SuggestedStayMinDays = 2, SuggestedStayMaxDays = 4, BestSeasons = ["summer"], Tags = ["historija"],
            Translations = [new() { LanguageCode = "en", Description = "English description", BestTimeToVisit = "Summer" }]
        }).ToList();

        public Task<IReadOnlyList<Destination>> GetAllWithTranslationsAsync(CancellationToken cancellationToken) => Task.FromResult(destinations);
    }
}
