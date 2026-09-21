using Microsoft.EntityFrameworkCore;
using TravelPlanner.Api.Domain.Entities;
using TravelPlanner.Api.Infrastructure.Persistence;
using TravelPlanner.Infrastructure.Repositories;
using TravelPlanner.Application.DTOs;
using Xunit;

namespace TravelPlanner.Api.Tests.Infrastructure.Persistence;

public sealed class DestinationImportRepositoryTests
{
    [PostgisFact]
    public async Task Details_repository_returns_imported_attributes_by_slug()
    {
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable("TEST_POSTGIS_CONNECTION"), options => options.UseNetTopologySuite()).Options);
        var destination = Destination($"Q{Random.Shared.Next(100000000, 999999999)}", $"imported-{Guid.NewGuid():N}");
        destination.Latitude = 43.859;
        destination.Longitude = 18.429;
        destination.ElevationM = 518;
        destination.Population = 275524;
        destination.ImageUrl = "https://images.test/test.jpg";
        destination.ImageAuthor = "Test author";
        destination.ImageLicense = "CC BY-SA 4.0";
        destination.ImageSourceUrl = "https://commons.test/Test";
        destination.DescriptionLicense = "CC BY-SA";
        destination.DescriptionSourceUrl = "https://bs.wikipedia.test/Test";
        context.Destinations.Add(destination);
        await context.SaveChangesAsync();

        var result = await new DestinationDetailsRepository(context).GetBySlugAsync(destination.Slug, "bs", default);

        Assert.NotNull(result);
        Assert.Equal(destination.ElevationM, result.ElevationMeters);
        Assert.Equal(destination.Population, result.Population);
        Assert.Equal(destination.ImageUrl, result.ImageUrl);
        Assert.Equal("Test author", result.ImageAttribution!.Author);
        Assert.Equal("CC BY-SA", result.DescriptionAttribution!.License);
        Assert.Equal(destination.Latitude, result.Latitude);
        await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM \"Destination\" WHERE \"Id\" = {destination.Id}");
    }

    [PostgisFact]
    public async Task Unique_external_id_conflict_returns_the_existing_destination()
    {
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable("TEST_POSTGIS_CONNECTION"), options => options.UseNetTopologySuite()).Options);
        var repository = new DestinationImportRepository(context);
        var qid = $"Q{Random.Shared.Next(100000000, 999999999)}";
        var first = Destination(qid, $"conflict-a-{Guid.NewGuid():N}");
        var second = Destination(qid, $"conflict-b-{Guid.NewGuid():N}");

        var saved = await repository.AddAsync(first, default);
        var existing = await repository.AddAsync(second, default);

        Assert.Equal(saved.Id, existing.Id);
        Assert.Equal(saved.Slug, existing.Slug);
        await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM \"Destination\" WHERE \"Id\" = {saved.Id}");
    }

    private static Destination Destination(string qid, string slug) => new()
    {
        Id = Guid.NewGuid(), Name = "Conflict test", Slug = slug, Type = "grad", Source = "wikidata", ExternalId = qid,
        ImportedAt = DateTimeOffset.UtcNow, Region = "BiH", Description = "Opis", DescriptionLanguage = "bs",
        BestTimeToVisit = "proljeće-jesen", BudgetTier = "standard", SuggestedStayMinDays = 1,
        SuggestedStayMaxDays = 3, BestSeasons = ["spring"], Tags = ["grad"], IsRecommendationEligible = true
    };
}
