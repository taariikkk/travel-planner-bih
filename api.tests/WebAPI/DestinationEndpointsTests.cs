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
using Xunit;

namespace TravelPlanner.Api.Tests.WebAPI;

public sealed class DestinationEndpointsTests
{
    [Fact]
    public async Task Recommend_requires_a_token()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/destinations/recommend", new { budgetTier = "standard", travelDays = 3, season = "summer", language = "bs" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Recommend_validates_request_and_returns_at_most_five_localized_results()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await RegisterAsync(client));

        var invalid = await client.PostAsJsonAsync("/api/destinations/recommend", new { budgetTier = "standard", travelDays = 15, season = "summer", language = "bs" });
        var response = await client.PostAsJsonAsync("/api/destinations/recommend", new { budgetTier = "standard", travelDays = 3, season = "summer", language = "en" });
        using var results = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(5, results.RootElement.GetArrayLength());
        Assert.Equal("English description", results.RootElement[0].GetProperty("description").GetString());
    }

    private static async Task<string> RegisterAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new { email = "test@example.com", password = "password123", displayName = "Test" });
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("accessToken").GetString()!;
    }

    private sealed class ApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=travelplanner_test;Username=test;Password=test",
                ["Jwt:Key"] = "test-signing-key-that-is-at-least-thirty-two-characters-long",
                ["Jwt:Issuer"] = "TravelPlanner.Api.Tests",
                ["Jwt:Audience"] = "TravelPlanner.Web.Tests"
            }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IUserRepository>();
                services.RemoveAll<IDestinationRepository>();
                services.AddSingleton<Users>();
                services.AddScoped<IUserRepository>(provider => provider.GetRequiredService<Users>());
                services.AddSingleton<IDestinationRepository>(new Destinations());
            });
        }
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
            Id = Guid.NewGuid(), Name = $"Grad {index}", Region = "BiH", Description = "Opis", BestTimeToVisit = "Ljeto",
            BudgetTier = "standard", SuggestedStayMinDays = 2, SuggestedStayMaxDays = 4, BestSeasons = ["summer"], Tags = ["historija"],
            Translations = [new() { LanguageCode = "en", Description = "English description", BestTimeToVisit = "Summer" }]
        }).ToList();

        public Task<IReadOnlyList<Destination>> GetAllWithTranslationsAsync(CancellationToken cancellationToken) => Task.FromResult(destinations);
    }
}
