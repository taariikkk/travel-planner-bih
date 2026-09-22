using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using TravelPlanner.Api.Domain.Entities;
using TravelPlanner.Api.Infrastructure.Persistence;
using TravelPlanner.Infrastructure.Repositories;
using Xunit;

namespace TravelPlanner.Api.Tests.Infrastructure.Persistence;

public sealed class DestinationMapRepositoryTests
{
    [PostgisFact]
    public async Task Returns_every_valid_place_instead_of_limiting_the_map_to_six()
    {
        await using var context = CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TEMP TABLE "Place" (LIKE public."Place" INCLUDING ALL) ON COMMIT DROP;
            CREATE TEMP TABLE "DestinationPlace" (LIKE public."DestinationPlace" INCLUDING ALL) ON COMMIT DROP
            """);
        var destination = await context.Destinations.FirstAsync();
        var prefix = $"Map cluster test {Guid.NewGuid():N}";
        var places = Enumerable.Range(0, 80).Select(index => new Place
        {
            Id = Guid.NewGuid(),
            DestinationLinks = [new() { DestinationId = destination.Id, IsManual = true }],
            Name = $"{prefix} {index:00}",
            Category = index % 2 == 0 ? "attraction" : "restaurant",
            Source = "manual",
            Location = new Point(18.39 + index * 0.0001, 43.84 + index * 0.0001) { SRID = 4326 }
        }).ToArray();
        context.Places.AddRange(places);
        await context.SaveChangesAsync();

        var result = await new DestinationMapRepository(context).GetPlacesBySlugAsync(destination.Slug, default);

        Assert.NotNull(result);
        Assert.Equal(80, result.Count(place => place.Name.StartsWith(prefix, StringComparison.Ordinal)));

        await transaction.RollbackAsync();
    }

    [PostgisFact]
    public async Task Distinguishes_an_empty_destination_from_an_unknown_slug()
    {
        await using var context = CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TEMP TABLE "Place" (LIKE public."Place" INCLUDING ALL) ON COMMIT DROP;
            CREATE TEMP TABLE "DestinationPlace" (LIKE public."DestinationPlace" INCLUDING ALL) ON COMMIT DROP
            """);
        var destination = await context.Destinations.FirstAsync();
        var repository = new DestinationMapRepository(context);

        var empty = await repository.GetPlacesBySlugAsync(destination.Slug, default);
        var missing = await repository.GetPlacesBySlugAsync($"missing-{Guid.NewGuid():N}", default);

        Assert.NotNull(empty);
        Assert.Empty(empty);
        Assert.Null(missing);
        await transaction.RollbackAsync();
    }

    private static ApplicationDbContext CreateContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseNpgsql(Environment.GetEnvironmentVariable("TEST_POSTGIS_CONNECTION"), options => options.UseNetTopologySuite()).Options);
}
