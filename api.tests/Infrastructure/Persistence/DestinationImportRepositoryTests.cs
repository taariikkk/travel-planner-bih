using Microsoft.EntityFrameworkCore;
using TravelPlanner.Api.Domain.Entities;
using TravelPlanner.Api.Infrastructure.Persistence;
using TravelPlanner.Infrastructure.Repositories;
using Xunit;

namespace TravelPlanner.Api.Tests.Infrastructure.Persistence;

public sealed class DestinationImportRepositoryTests
{
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
