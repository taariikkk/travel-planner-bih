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

public sealed class AuthEndpointsTests
{
    [Fact]
    public async Task Register_returns_created_token_and_user_without_password_hash()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new { email = "tarik@example.com", password = "password123", displayName = "Tarik" });
        var body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(body.RootElement.GetProperty("accessToken").GetString()));
        Assert.Equal("tarik@example.com", body.RootElement.GetProperty("user").GetProperty("email").GetString());
        Assert.False(body.RootElement.GetProperty("user").TryGetProperty("passwordHash", out _));
    }

    [Fact]
    public async Task Register_rejects_duplicate_email_and_invalid_request()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var first = await client.PostAsJsonAsync("/api/auth/register", new { email = "tarik@example.com", password = "password123", displayName = "Tarik" });
        var duplicate = await client.PostAsJsonAsync("/api/auth/register", new { email = "TARIK@example.com", password = "password123", displayName = "Tarik" });
        var invalid = await client.PostAsJsonAsync("/api/auth/register", new { email = "invalid", password = "short", displayName = "" });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact]
    public async Task Login_returns_token_and_rejects_invalid_credentials_or_empty_fields()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        await RegisterAsync(client);

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "tarik@example.com", password = "password123" });
        var wrongPassword = await client.PostAsJsonAsync("/api/auth/login", new { email = "tarik@example.com", password = "wrong-password" });
        var emptyFields = await client.PostAsJsonAsync("/api/auth/login", new { email = "", password = "" });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, emptyFields.StatusCode);
    }

    [Fact]
    public async Task Profile_requires_token_and_only_updates_allowed_fields()
    {
        using var factory = new ApiFactory();
        using var anonymousClient = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymousClient.GetAsync("/api/users/me")).StatusCode);

        var token = await RegisterAsync(anonymousClient);
        anonymousClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var initial = await ReadJsonAsync(await anonymousClient.GetAsync("/api/users/me"));
        var update = await anonymousClient.PutAsJsonAsync("/api/users/me", new
        {
            displayName = "Novi Tarik",
            preferences = new[] { "priroda", "hrana" },
            email = "should-not-change@example.com",
            password = "should-not-change"
        });
        var updated = await ReadJsonAsync(update);

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(initial.RootElement.GetProperty("email").GetString(), updated.RootElement.GetProperty("email").GetString());
        Assert.Equal("Novi Tarik", updated.RootElement.GetProperty("displayName").GetString());
        Assert.Equal(["priroda", "hrana"], updated.RootElement.GetProperty("preferences").EnumerateArray().Select(value => value.GetString()!).ToArray());
    }

    private static async Task<string> RegisterAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new { email = "tarik@example.com", password = "password123", displayName = "Tarik" });
        var body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return body.RootElement.GetProperty("accessToken").GetString()!;
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(body);
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
                services.AddSingleton<InMemoryUserRepository>();
                services.AddScoped<IUserRepository>(provider => provider.GetRequiredService<InMemoryUserRepository>());
            });
        }
    }

    private sealed class InMemoryUserRepository : IUserRepository
    {
        private readonly List<User> users = [];

        public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
            Task.FromResult(users.SingleOrDefault(user => user.Email == normalizedEmail));

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(users.SingleOrDefault(user => user.Id == id));

        public Task AddAsync(User user, CancellationToken cancellationToken)
        {
            users.Add(user);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
