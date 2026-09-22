using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using TravelPlanner.Api.Domain.Entities;
using TravelPlanner.Api.Infrastructure.Persistence;
using TravelPlanner.Application.DTOs;
using TravelPlanner.Infrastructure.Providers;
using TravelPlanner.Infrastructure.Repositories;
using Xunit;

namespace TravelPlanner.Api.Tests.Infrastructure.Persistence;

public sealed class PlacesIntegrationTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 21, 12, 0, 0, TimeSpan.Zero);
    private static readonly PlaceData Imported = new("node/123", "OSM name", "restaurant", 43.3, 17.8, DescriptionEn: "OSM description", Website: "https://example.org/");

    [PostgisFact]
    public async Task Shared_OSM_identity_preserves_references_and_manual_overrides_after_refresh()
    {
        await using var database = await Database.CreateAsync();
        var first = await database.DestinationAsync("mostar");
        var second = await database.DestinationAsync("sarajevo");
        await database.ImportAsync(first, [Imported]);
        await database.ImportAsync(second, [Imported]);
        Guid placeId;
        await using (var context = database.Context())
        {
            var place = await context.Places.SingleAsync(); placeId = place.Id;
            Assert.Equal(2, await context.DestinationPlaces.CountAsync());
            var user = new User { Id = Guid.NewGuid(), Email = "test@example.test", PasswordHash = "test", DisplayName = "Test" };
            context.Users.Add(user);
            context.SavedPlaces.Add(new SavedPlace(user.Id, null, placeId));
            var trip = new Trip { Id = Guid.NewGuid(), User = user, Title = "Test", StartDate = new(2026, 9, 21), EndDate = new(2026, 9, 21), Travelers = 1 };
            var day = new TripDay { Id = Guid.NewGuid(), Trip = trip, DayNumber = 1, Date = trip.StartDate };
            context.ItineraryItems.Add(new() { Id = Guid.NewGuid(), TripDay = day, PlaceId = placeId });
            await context.SaveChangesAsync();
        }
        await database.SeedAsync(Seed("edited", new { name = "Manual name", website = (string?)null }));
        await database.ImportAsync(first, [Imported with { Name = "Changed OSM", DescriptionEn = "Updated" }], Now.AddDays(8));
        await using (var context = database.Context())
        {
            var place = await context.Places.SingleAsync();
            Assert.Equal(placeId, place.Id);
            Assert.Equal("Manual name", place.Name);
            Assert.Equal("manual", place.Source);
            Assert.Null(place.Website);
            Assert.Equal("Updated", place.DescriptionEn);
            Assert.Equal(placeId, (await context.SavedPlaces.SingleAsync()).PlaceId);
            Assert.Equal(placeId, (await context.ItineraryItems.SingleAsync()).PlaceId);
        }
        await database.ImportAsync(first, [], Now.AddDays(16));
        await database.ImportAsync(second, [], Now.AddDays(16));
        await using (var context = database.Context())
        {
            Assert.Single(await context.Places.ToArrayAsync());
            var link = Assert.Single(await context.DestinationPlaces.ToArrayAsync());
            Assert.Equal(first, link.DestinationId);
            Assert.True(link.IsManual);
        }
    }

    [PostgisFact]
    public async Task Manual_first_then_import_and_reseed_is_idempotent()
    {
        await using var database = await Database.CreateAsync();
        var json = Seed("manual-first", new { name = "Manual", category = "attraction", location = new { latitude = 43.31, longitude = 17.81 }, descriptionBs = "Opis" });
        await database.SeedAsync(json);
        Guid id;
        await using (var context = database.Context()) id = (await context.Places.SingleAsync()).Id;
        await database.ImportAsync(await database.DestinationAsync("mostar"), [Imported]);
        await database.SeedAsync(json);
        await using (var context = database.Context())
        {
            var place = await context.Places.SingleAsync();
            Assert.Equal(id, place.Id);
            Assert.Equal("Manual", place.Name);
            Assert.Equal("attraction", place.Category);
            Assert.Equal(43.31, place.Location.Y);
            Assert.NotNull(place.LastVerifiedAt);
            Assert.Equal("OSM description", place.DescriptionEn);
            Assert.Single(await context.DestinationPlaces.ToArrayAsync());
        }
    }

    [PostgisFact]
    public async Task Seeder_validates_whole_document_and_rolls_back_all_records()
    {
        await using var database = await Database.CreateAsync();
        var json = "[" + Seed("valid", new { name = "Manual", category = "restaurant", location = new { latitude = 43, longitude = 18 } }).Trim('[', ']')
            + "," + Seed("incomplete", new { name = "No location" }, "node/124").Trim('[', ']') + "]";
        await Assert.ThrowsAsync<ValidationException>(() => database.SeedAsync(json));
        await using var context = database.Context();
        Assert.Empty(await context.Places.ToArrayAsync());
        await Assert.ThrowsAsync<ValidationException>(() => database.SeedAsync(Seed("image", new { name = "Manual", category = "restaurant", location = new { latitude = 43, longitude = 18 }, imageUrl = "https://example.org/image.jpg" })));
        Assert.Empty(await context.Places.AsNoTracking().ToArrayAsync());
    }

    [PostgisFact]
    public async Task TTL_boundary_empty_cache_query_change_and_failure_backoff()
    {
        await using var database = await Database.CreateAsync();
        var id = await database.DestinationAsync("mostar");
        await using var context = database.Context();
        var repo = Repository(context);
        var lease = await repo.TryAcquireAsync(id, "query", Now, TimeSpan.FromDays(7), default);
        Assert.NotNull(lease);
        await repo.CompleteAsync(id, lease, new([]), Now, default);
        Assert.Null(await repo.TryAcquireAsync(id, "query", Now.AddDays(7).AddTicks(-10), TimeSpan.FromDays(7), default));
        var boundary = await repo.TryAcquireAsync(id, "query", Now.AddDays(7), TimeSpan.FromDays(7), default);
        Assert.NotNull(boundary);
        await repo.FailAsync(id, boundary, Now.AddDays(7).AddMinutes(15), default);
        Assert.Null(await repo.TryAcquireAsync(id, "changed-query", Now.AddDays(7).AddMinutes(1), TimeSpan.FromDays(7), default));
        var changed = await repo.TryAcquireAsync(id, "changed-query", Now.AddDays(7).AddMinutes(15), TimeSpan.FromDays(7), default);
        Assert.NotNull(changed);
        await repo.CompleteAsync(id, changed, new([]), Now.AddDays(7).AddMinutes(15), default);
        var state = await context.PlacesImportStates.AsNoTracking().SingleAsync();
        Assert.Equal("changed-query", state.QuerySignature);
        Assert.Equal(Now.AddDays(7).AddMinutes(15), state.SucceededAt);
        Assert.NotNull(await repo.TryAcquireAsync(id, "new-radius", Now.AddDays(7).AddMinutes(16), TimeSpan.FromDays(7), default));
    }

    [PostgisFact]
    public async Task Concurrent_requests_have_one_lease_and_expired_writer_is_fenced_out()
    {
        await using var database = await Database.CreateAsync();
        var id = await database.DestinationAsync("mostar");
        async Task<PlacesImportLease?> Acquire() { await using var c = database.Context(); return await Repository(c).TryAcquireAsync(id, "query", Now, TimeSpan.FromDays(7), default); }
        var results = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => Acquire()));
        var oldLease = Assert.Single(results, r => r is not null)!;
        await using var context = database.Context();
        var repo = Repository(context);
        var newLease = await repo.TryAcquireAsync(id, "query", Now.AddMinutes(3), TimeSpan.FromDays(7), default);
        Assert.NotNull(newLease);
        await repo.CompleteAsync(id, oldLease, new([Imported]), Now.AddMinutes(3), default);
        Assert.Empty(await context.Places.ToArrayAsync());
        await repo.CompleteAsync(id, newLease, new([Imported]), Now.AddMinutes(3), default);
        Assert.Single(await context.Places.ToArrayAsync());
    }

    [PostgisFact]
    public async Task Import_transaction_rolls_back_places_links_and_success_timestamp()
    {
        await using var database = await Database.CreateAsync();
        var id = await database.DestinationAsync("mostar");
        await using var context = database.Context();
        var repo = Repository(context);
        var lease = await repo.TryAcquireAsync(id, "query", Now, TimeSpan.FromDays(7), default);
        await Assert.ThrowsAsync<DbUpdateException>(() => repo.CompleteAsync(id, lease!, new([Imported, Imported with { ExternalId = "node/456", Name = null! }]), Now, default));
        await using var check = database.Context();
        Assert.Empty(await check.Places.ToArrayAsync());
        Assert.Empty(await check.DestinationPlaces.ToArrayAsync());
        Assert.Null((await check.PlacesImportStates.SingleAsync()).SucceededAt);
    }

    [PostgisFact]
    public async Task Global_gate_enforces_concurrency_interval_budget_retry_after_and_day_reset()
    {
        await using var database = await Database.CreateAsync();
        var clock = new Clock { Now = Now };
        var options = new OverpassOptions { DailyAttemptLimit = 2 };
        await using var first = database.Context();
        await using var second = database.Context();
        var gate = new OverpassRequestGate(first, options, clock);
        var other = new OverpassRequestGate(second, options, clock);
        var token = await gate.AcquireAsync(false, default);
        await Assert.ThrowsAsync<PlacesProviderException>(() => other.AcquireAsync(true, default));
        await gate.ReleaseAsync(token, null, default);
        await Assert.ThrowsAsync<PlacesProviderException>(() => other.AcquireAsync(false, default));
        var fallback = await other.AcquireAsync(true, default);
        await other.ReleaseAsync(fallback, Now.AddMinutes(2), default);
        await Assert.ThrowsAsync<PlacesProviderException>(() => gate.AcquireAsync(true, default));
        clock.Now = Now.AddDays(1);
        var nextDay = await gate.AcquireAsync(false, default);
        await gate.ReleaseAsync(nextDay, clock.Now.AddMinutes(2), default);
        var throttled = await Assert.ThrowsAsync<PlacesProviderException>(() => other.AcquireAsync(true, default));
        Assert.Equal(clock.Now.AddMinutes(2), throttled.RetryAt);
    }

    [Theory]
    [InlineData("[{\"key\":\"x\",\"destinations\":[\"mostar\"],\"externalId\":\"123\",\"fields\":{}}]")]
    [InlineData("[{\"key\":\"x\",\"destinations\":[\"mostar\"],\"fields\":{\"name\":null}}]")]
    [InlineData("[{\"key\":\"x\",\"destinations\":[\"mostar\"],\"fields\":{\"website\":\"javascript:alert(1)\"}}]")]
    [InlineData("[{\"key\":\"x\",\"destinations\":[\"mostar\"],\"fields\":{\"location\":{\"latitude\":91,\"longitude\":18}}}]")]
    [InlineData("[{\"key\":\"x\",\"destinations\":[\"mostar\"],\"fields\":{\"unknown\":\"bad\"}}]")]
    public void Seeder_rejects_invalid_data(string json) => Assert.Throws<ValidationException>(() => ManualPlacesSeeder.Parse(json));

    private static string Seed(string key, object fields, string externalId = "node/123") =>
        JsonSerializer.Serialize(new[] { new { key, destinations = new[] { "mostar" }, externalId, fields } });
    private static PlacesImportRepository Repository(ApplicationDbContext context) => new(context, NullLogger<PlacesImportRepository>.Instance);
    private sealed class Clock : TimeProvider { public DateTimeOffset Now { get; set; } public override DateTimeOffset GetUtcNow() => Now; }
    private sealed class EnvironmentStub : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = ".";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
    // SQL identifiers below are generated GUIDs and a fixed table allowlist, never user input.
#pragma warning disable EF1002
    private sealed class Database(string connection, string schema) : IAsyncDisposable
    {
        public ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connection, o => o.UseNetTopologySuite()).Options);
        public static async Task<Database> CreateAsync()
        {
            var schema = "places_test_" + Guid.NewGuid().ToString("N");
            var connection = new NpgsqlConnectionStringBuilder(System.Environment.GetEnvironmentVariable("TEST_POSTGIS_CONNECTION")) { SearchPath = schema + ",public" }.ConnectionString;
            var database = new Database(connection, schema);
            await using var context = database.Context();
            await context.Database.ExecuteSqlRawAsync($"CREATE SCHEMA {schema}");
            foreach (var table in new[] { "Destination", "Place", "DestinationPlace", "PlacesImportState", "OverpassRequestState", "User", "SavedPlace", "Trip", "TripDay", "ItineraryItem" })
                await context.Database.ExecuteSqlRawAsync($"CREATE TABLE {schema}.\"{table}\" (LIKE public.\"{table}\" INCLUDING ALL)");
            await context.Database.ExecuteSqlRawAsync($"INSERT INTO {schema}.\"Destination\" SELECT * FROM public.\"Destination\"");
            await context.Database.ExecuteSqlRawAsync("""
                ALTER TABLE "DestinationPlace" ADD FOREIGN KEY ("PlaceId") REFERENCES "Place" ("Id"), ADD FOREIGN KEY ("DestinationId") REFERENCES "Destination" ("Id");
                ALTER TABLE "SavedPlace" ADD FOREIGN KEY ("PlaceId") REFERENCES "Place" ("Id");
                ALTER TABLE "ItineraryItem" ADD FOREIGN KEY ("PlaceId") REFERENCES "Place" ("Id");
                """);
            return database;
        }
        public async Task<Guid> DestinationAsync(string slug) { await using var c = Context(); return await c.Destinations.Where(d => d.Slug == slug).Select(d => d.Id).SingleAsync(); }
        public async Task SeedAsync(string json) { await using var c = Context(); await new ManualPlacesSeeder(c, new EnvironmentStub()).SeedJsonAsync(json, default); }
        public async Task ImportAsync(Guid id, IReadOnlyList<PlaceData> places, DateTimeOffset? now = null)
        {
            await using var c = Context(); var repo = Repository(c); var time = now ?? Now;
            var lease = await repo.TryAcquireAsync(id, "query", time, TimeSpan.FromDays(7), default);
            Assert.NotNull(lease);
            await repo.CompleteAsync(id, lease, new(places), time, default);
        }
        public async ValueTask DisposeAsync()
        {
            await using var c = Context();
            await c.Database.ExecuteSqlRawAsync($"DROP SCHEMA {schema} CASCADE");
        }
    }
}
