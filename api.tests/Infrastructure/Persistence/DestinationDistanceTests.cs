using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using TravelPlanner.Api.Infrastructure.Persistence;
using TravelPlanner.Infrastructure.Repositories;
using Xunit;

namespace TravelPlanner.Api.Tests.Infrastructure.Persistence;

public sealed class DestinationDistanceTests
{
    [PostgisFact]
    public async Task Nearby_places_are_limited_to_six_in_distance_order_and_empty_state_is_preserved()
    {
        var commands = new ReadCommands();
        using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable("TEST_POSTGIS_CONNECTION"), options => options.UseNetTopologySuite())
            .AddInterceptors(commands).Options);
        await using var transaction = await context.Database.BeginTransactionAsync();
        // A transaction-local shadow table keeps all real Place records untouched.
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TEMP TABLE "Place" (LIKE public."Place" INCLUDING ALL) ON COMMIT DROP;
            CREATE TEMP TABLE "DestinationPlace" (LIKE public."DestinationPlace" INCLUDING ALL) ON COMMIT DROP
            """);
        var repository = new DestinationDetailsRepository(context);
        var empty = await repository.GetBySlugAsync("mostar", "bs", default);
        Assert.NotNull(empty);
        Assert.Empty(empty.Places);
        for (var index = 8; index >= 1; index--)
            context.Places.Add(new TravelPlanner.Api.Domain.Entities.Place
            {
                Id = Guid.NewGuid(), DestinationLinks = [new() { DestinationId = empty.Id, IsManual = true }], Name = $"Place {index}",
                Category = "attraction", Source = "manual",
                Location = new NetTopologySuite.Geometries.Point(17.815 + index * .001, 43.3373) { SRID = 4326 }
            });
        await context.SaveChangesAsync();
        var result = await repository.GetBySlugAsync("mostar", "en", default);
        Assert.NotNull(result);
        Assert.Equal(Enumerable.Range(1, 6).Select(index => $"Place {index}"), result.Places.Select(place => place.Name));
        Assert.All(result.Places, place => Assert.Equal("attraction", place.Category));
        Assert.InRange(result.DistanceFromSarajevoKm!.Value, 75, 77);
        Assert.Null(result.ElevationMeters);
        Assert.Null(result.AverageTemperatureC);
        commands.Sql.Clear();
        var sarajevo = await repository.GetBySlugAsync("sarajevo", "bs", default);
        Assert.NotNull(sarajevo);
        Assert.Null(sarajevo.DistanceFromSarajevoKm);
        Assert.Equal(2, commands.Sql.Count);
        Assert.Single(commands.Sql, sql => sql.Contains("FROM \"Destination\""));
        var placesSql = Assert.Single(commands.Sql, sql => sql.Contains("FROM \"Place\""));
        Assert.DoesNotContain("SELECT *", placesSql);
        Assert.DoesNotContain("ManualOverrideFields", placesSql);
        Assert.Contains("ORDER BY", placesSql);
        Assert.Contains("LIMIT", placesSql);
        await transaction.RollbackAsync();
    }

    private sealed class ReadCommands : DbCommandInterceptor
    {
        public List<string> Sql { get; } = [];
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Sql.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }

    [Fact]
    public void Distance_query_uses_longitude_first_geography_and_converts_meters_to_km()
    {
        var query = DestinationDistance.QueryKm(43.3373, 17.815, 43.859, 18.429);
        Assert.Equal(new object[] { 17.815, 43.3373, 18.429, 43.859 }, query.GetArguments());
        Assert.Contains("ST_Distance(", query.Format);
        Assert.Contains("::geography", query.Format);
        Assert.Contains("/ 1000.0", query.Format);
        using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=unused", options => options.UseNetTopologySuite()).Options);
        var sql = context.Database.SqlQuery<double>(query).ToQueryString();
        Assert.Contains("ST_MakePoint(@p0, @p1)", sql);
    }

    // The numerical calculation belongs to PostGIS, so verify it against PostGIS rather than a planar NTS substitute.
    [PostgisFact]
    public async Task Postgis_distance_is_zero_at_origin_and_about_76_km_to_Mostar()
    {
        using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable("TEST_POSTGIS_CONNECTION"), options => options.UseNetTopologySuite()).Options);
        var same = await context.Database.SqlQuery<double>(
            DestinationDistance.QueryKm(43.859, 18.429, 43.859, 18.429)).SingleAsync();
        var distance = await context.Database.SqlQuery<double>(
            DestinationDistance.QueryKm(43.3373, 17.815, 43.859, 18.429)).SingleAsync();
        var reverse = await context.Database.SqlQuery<double>(
            DestinationDistance.QueryKm(43.859, 18.429, 43.3373, 17.815)).SingleAsync();
        Assert.Equal(0, same);
        Assert.InRange(distance, 75, 77);
        Assert.Equal(distance, reverse, 6);
    }
}

public sealed class PostgisFactAttribute : FactAttribute
{
    public PostgisFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TEST_POSTGIS_CONNECTION")))
            Skip = "Set TEST_POSTGIS_CONNECTION to run against a PostGIS database.";
    }
}
