using Microsoft.EntityFrameworkCore;
using TravelPlanner.Api.Domain.Entities;
using TravelPlanner.Api.Infrastructure.Persistence;
using TravelPlanner.Application.DTOs;
using TravelPlanner.Infrastructure.Repositories;
using Xunit;

namespace TravelPlanner.Api.Tests.Infrastructure.Persistence;

public sealed class PlaceReadModelTests
{
    [Fact]
    public void Projection_translates_with_geography_order_limit_and_parameterized_values()
    {
        using var context = Context();
        var sql = PlaceReadModel.Query(context, Guid.NewGuid(), 43.3373, 17.815)
            .OrderBy(place => place.DistanceMeters).ThenBy(place => place.Name).ThenBy(place => place.Id)
            .Take(6).ToQueryString();
        Assert.Contains("ST_Distance", sql);
        Assert.Contains("::geography", sql);
        Assert.Contains("ST_MakePoint(@p1, @p2)", sql);
        Assert.Contains("ORDER BY", sql);
        Assert.Contains("LIMIT", sql);
        Assert.DoesNotContain("SELECT *", sql);
        Assert.DoesNotContain("ManualOverrideFields", sql);
        Assert.DoesNotContain("ManualKey", sql);
        Assert.Contains("ST_IsEmpty", sql);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public void Map_projection_and_unlocated_details_translate_without_distance_calculation()
    {
        using var context = Context();
        var map = PlaceReadModel.Query(context, Guid.NewGuid())
            .OrderBy(place => place.Name).ThenBy(place => place.Id).ToQueryString();
        Assert.DoesNotContain("ST_Distance", map);
        Assert.DoesNotContain("LIMIT", map);
        Assert.Contains("ST_IsEmpty", map);
        var unlocated = PlaceReadModel.Query(context, Guid.NewGuid(), validCoordinatesOnly: false)
            .OrderBy(place => place.Name).ThenBy(place => place.Id).Take(6).ToQueryString();
        Assert.DoesNotContain("ST_Distance", unlocated);
        Assert.DoesNotContain("ST_IsEmpty", unlocated);
        Assert.Contains("LIMIT", unlocated);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("https://example.org/source")]
    public void Projection_preserves_metadata_and_image_attribution_rules(string? imageSource)
    {
        var model = new PlaceReadModel
        {
            Id = Guid.NewGuid(), Name = "Place", Category = "restaurant", Latitude = 43, Longitude = 18,
            ImageUrl = "https://example.org/image", ImageAuthor = "Author", ImageLicense = "CC0", ImageSourceUrl = imageSource,
            DescriptionBs = "Opis", DescriptionEn = "Description", Description = "Original", DescriptionLanguage = "und",
            Address = "Address", Cuisine = "local", PriceLevel = "budget", Website = "https://example.org",
            Phone = "123", Email = "test@example.org", Source = "osm", ExternalId = "node/123",
            SourceUrl = "https://openstreetmap.org/node/123", ImportedAt = DateTimeOffset.UnixEpoch,
            LastVerifiedAt = DateTimeOffset.UnixEpoch.AddDays(1), DistanceMeters = 1234
        };
        var expectedMetadata = new PlaceMetadataResponse("Opis", "Description", "Original", "und",
            "Address", "local", "budget", "https://example.org", "123", "test@example.org", "osm",
            "node/123", "https://openstreetmap.org/node/123", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(1));
        var expectedImage = PlaceMetadataResponse.ImageAttribution(new Place
        {
            ImageUrl = model.ImageUrl, ImageAuthor = model.ImageAuthor, ImageLicense = model.ImageLicense, ImageSourceUrl = imageSource
        });
        var details = model.ToDetailsResponse();
        var map = model.ToMapResponse();
        var mapWithDistance = model.ToMapResponse(includeDistance: true);
        Assert.Equal(expectedMetadata, details.Metadata);
        Assert.Equal(expectedMetadata, map.Metadata);
        Assert.Equal(expectedImage, details.ImageAttribution);
        Assert.Equal(expectedImage, map.ImageAttribution);
        Assert.Equal(expectedImage is null ? null : model.ImageUrl, details.ImageUrl);
        Assert.Equal(details.ImageUrl, map.ImageUrl);
        Assert.Equal(43, map.Latitude);
        Assert.Equal(18, map.Longitude);
        Assert.Null(map.DistanceKm);
        Assert.Equal(1.234, mapWithDistance.DistanceKm);
    }

    private static ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseNpgsql("Host=localhost;Database=unused", options => options.UseNetTopologySuite()).Options);
}
