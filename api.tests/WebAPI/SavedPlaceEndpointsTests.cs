using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TravelPlanner.Api.Domain.Entities;
using TravelPlanner.Application.DTOs;
using TravelPlanner.Application.Interfaces;
using Xunit;

namespace TravelPlanner.Api.Tests.WebAPI;

public sealed class SavedPlaceEndpointsTests
{
    [Fact]
    public async Task Saved_place_endpoints_require_authentication()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/saved-places")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/saved-places", new { destinationId = Guid.NewGuid() })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.DeleteAsync($"/api/saved-places/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task Add_validates_exactly_one_target_and_is_idempotent()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await RegisterAsync(client));
        var destinationId = Guid.NewGuid();

        var neither = await client.PostAsJsonAsync("/api/saved-places", new { });
        var both = await client.PostAsJsonAsync("/api/saved-places", new { destinationId, placeId = Guid.NewGuid() });
        var first = await client.PostAsJsonAsync("/api/saved-places", new { destinationId });
        var second = await client.PostAsJsonAsync("/api/saved-places", new { destinationId });
        var firstBody = await first.Content.ReadFromJsonAsync<SavedPlaceResponse>();
        var secondBody = await second.Content.ReadFromJsonAsync<SavedPlaceResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, neither.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, both.StatusCode);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.NotNull(firstBody);
        Assert.Equal(firstBody.Id, secondBody!.Id);
        Assert.Equal(destinationId, firstBody.DestinationId);
    }

    [Fact]
    public async Task List_returns_only_current_users_saved_places()
    {
        using var factory = new ApiFactory();
        using var firstClient = factory.CreateClient();
        using var secondClient = factory.CreateClient();
        firstClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await RegisterAsync(firstClient, "first@example.com"));
        secondClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await RegisterAsync(secondClient, "second@example.com"));
        var firstDestination = Guid.NewGuid();
        var secondDestination = Guid.NewGuid();

        await firstClient.PostAsJsonAsync("/api/saved-places", new { destinationId = firstDestination });
        await secondClient.PostAsJsonAsync("/api/saved-places", new { destinationId = secondDestination });
        var savedPlaces = await firstClient.GetFromJsonAsync<SavedPlaceResponse[]>("/api/saved-places");

        var savedPlace = Assert.Single(savedPlaces!);
        Assert.Equal(firstDestination, savedPlace.DestinationId);
    }

    [Fact]
    public async Task Delete_returns_no_content_not_found_and_forbidden_as_appropriate()
    {
        using var factory = new ApiFactory();
        using var ownerClient = factory.CreateClient();
        using var otherClient = factory.CreateClient();
        ownerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await RegisterAsync(ownerClient, "owner@example.com"));
        otherClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await RegisterAsync(otherClient, "other@example.com"));
        var saved = await (await ownerClient.PostAsJsonAsync("/api/saved-places", new { destinationId = Guid.NewGuid() })).Content.ReadFromJsonAsync<SavedPlaceResponse>();

        Assert.Equal(HttpStatusCode.Forbidden, (await otherClient.DeleteAsync($"/api/saved-places/{saved!.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ownerClient.DeleteAsync($"/api/saved-places/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await ownerClient.DeleteAsync($"/api/saved-places/{saved.Id}")).StatusCode);
    }

    private static async Task<string> RegisterAsync(HttpClient client, string email = "saved@example.com")
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new { email, password = "password123", displayName = "Saved" });
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!.AccessToken;
    }

    private sealed class ApiFactory : WebApplicationFactory<Program>
    {
        private readonly InMemoryUsers users = new();
        private readonly InMemorySavedPlaces savedPlaces = new();

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
                services.RemoveAll<ISavedPlaceRepository>();
                services.AddSingleton<IUserRepository>(users);
                services.AddSingleton<ISavedPlaceRepository>(savedPlaces);
            });
        }
    }

    private sealed class InMemoryUsers : IUserRepository
    {
        private readonly List<User> users = [];
        public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) => Task.FromResult(users.SingleOrDefault(user => user.Email == normalizedEmail));
        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(users.SingleOrDefault(user => user.Id == id));
        public Task AddAsync(User user, CancellationToken cancellationToken) { users.Add(user); return Task.CompletedTask; }
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class InMemorySavedPlaces : ISavedPlaceRepository
    {
        private readonly List<SavedPlace> savedPlaces = [];

        public Task<SavedPlace> AddAsync(SavedPlace savedPlace, CancellationToken cancellationToken)
        {
            var existing = savedPlaces.SingleOrDefault(place => place.UserId == savedPlace.UserId && place.DestinationId == savedPlace.DestinationId && place.PlaceId == savedPlace.PlaceId);
            if (existing is not null) return Task.FromResult(existing);
            savedPlaces.Add(savedPlace);
            return Task.FromResult(savedPlace);
        }

        public Task<bool> RemoveAsync(Guid id, Guid userId, CancellationToken cancellationToken)
        {
            var savedPlace = savedPlaces.SingleOrDefault(place => place.Id == id);
            if (savedPlace is null) return Task.FromResult(false);
            if (savedPlace.UserId != userId) throw new UnauthorizedAccessException();
            savedPlaces.Remove(savedPlace);
            return Task.FromResult(true);
        }

        public Task<IReadOnlyList<SavedPlace>> ListForUserAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SavedPlace>>(savedPlaces.Where(place => place.UserId == userId).ToArray());

        public Task<bool> ExistsForDestinationAsync(Guid userId, Guid destinationId, CancellationToken cancellationToken) =>
            Task.FromResult(savedPlaces.Any(place => place.UserId == userId && place.DestinationId == destinationId));

        public Task<bool> ExistsForPlaceAsync(Guid userId, Guid placeId, CancellationToken cancellationToken) =>
            Task.FromResult(savedPlaces.Any(place => place.UserId == userId && place.PlaceId == placeId));
    }
}
