using Microsoft.EntityFrameworkCore;
using TravelPlanner.Application.DTOs;
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
            DescriptionBs = "Opis", DescriptionEn = "Description", Description = "Original",
            DescriptionLanguage = "und", Address = "Address", Cuisine = "local", PriceLevel = "budget",
            Website = "https://example.org", Phone = "123", Email = "test@example.org",
            ExternalId = $"node/{index + 1}", SourceUrl = "https://openstreetmap.org",
            ImportedAt = DateTimeOffset.Parse("2026-09-21T12:00:00Z"), LastVerifiedAt = DateTimeOffset.Parse("2026-09-22T12:00:00Z"),
            ImageUrl = "https://example.org/image.jpg", ImageAuthor = "Author", ImageLicense = "CC0",
            ImageSourceUrl = index == 0 ? null : "https://example.org/source",
            Location = new Point(18.39 + index * 0.0001, 43.84 + index * 0.0001) { SRID = 4326 }
        }).ToArray();
        context.Places.AddRange(places);
        context.Places.Add(new Place
        {
            Id = Guid.NewGuid(), Name = "Invalid coordinates", Category = "attraction", Source = "manual",
            Location = new Point(181, 43) { SRID = 4326 },
            DestinationLinks = [new() { DestinationId = destination.Id, IsManual = true }]
        });
        await context.SaveChangesAsync();

        var result = await new DestinationMapRepository(context).GetPlacesBySlugAsync(destination.Slug, default);

        Assert.NotNull(result);
        Assert.Equal(80, result.Count);
        Assert.All(result, place => Assert.NotNull(place.DistanceKm));
        Assert.Equal(result.OrderBy(place => place.DistanceKm).ThenBy(place => place.Name).ThenBy(place => place.Id), result);
        var withoutAttribution = Assert.Single(result, place => place.Id == places[0].Id);
        Assert.Null(withoutAttribution.ImageUrl);
        Assert.Null(withoutAttribution.ImageAttribution);
        var attributed = Assert.Single(result, place => place.Id == places[1].Id);
        Assert.Equal(new ImageAttributionResponse("Author", "CC0", "https://example.org/source"), attributed.ImageAttribution);
        Assert.Equal(places[1].ImageUrl, attributed.ImageUrl);
        Assert.Equal(PlaceMetadataResponse.From(places[1]), attributed.Metadata);
        var details = await new DestinationDetailsRepository(context).GetBySlugAsync(destination.Slug, "bs", default);
        Assert.NotNull(details);
        Assert.Equal(6, details.Places.Count);
        foreach (var detail in details.Places)
        {
            var mapPlace = Assert.Single(result, place => place.Id == detail.Id);
            Assert.Equal(mapPlace.Metadata, detail.Metadata);
            Assert.Equal(mapPlace.ImageAttribution, detail.ImageAttribution);
            Assert.Equal(mapPlace.ImageUrl, detail.ImageUrl);
            Assert.Equal(mapPlace.Latitude, detail.Latitude);
            Assert.Equal(mapPlace.Longitude, detail.Longitude);
        }

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
