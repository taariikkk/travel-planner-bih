using Microsoft.EntityFrameworkCore;
using TravelPlanner.Api.Domain.Entities;
using TravelPlanner.Api.Infrastructure.Persistence;
using TravelPlanner.Infrastructure.Repositories;
using Xunit;

namespace TravelPlanner.Api.Tests.Infrastructure.Persistence;

public sealed class DestinationSearchRepositoryTests
{
    [PostgisFact]
    public async Task Search_ignores_Bosnian_diacritics()
    {
        await using var context = CreateContext();
        var results = await new DestinationSearchRepository(context)
            .SearchAsync("Bjelasnica", null, null, "bs", 12, default);

        Assert.Contains(results, destination => destination.Name == "Bjelašnica");
    }

    [PostgisFact]
    public async Task Search_applies_filters_limits_results_and_orders_manual_first()
    {
        await using var context = CreateContext();
        var results = await new DestinationSearchRepository(context)
            .SearchAsync(string.Empty, "grad", "Hercegovina", "en", 2, default);

        Assert.InRange(results.Count, 1, 2);
        Assert.All(results, destination =>
        {
            Assert.Equal("grad", destination.Type);
            Assert.Equal("Hercegovina", destination.Region);
        });
        Assert.Equal("manual", results[0].Source);
        Assert.False(string.IsNullOrWhiteSpace(results[0].Description));
    }

    [PostgisFact]
    public async Task Search_ranks_manual_then_exact_prefix_and_fuzzy_matches()
    {
        await using var context = CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        context.Destinations.AddRange(
            Destination("Qwertina", "manual"),
            Destination("Qwertina", "wikidata"),
            Destination("Qwertina Hills", "wikidata"),
            Destination("Qwertnia", "wikidata"));
        await context.SaveChangesAsync();

        var results = await new DestinationSearchRepository(context)
            .SearchAsync("Qwertina", null, null, "bs", 12, default);

        Assert.Equal("manual", results[0].Source);
        Assert.Equal("Qwertina", results[1].Name);
        Assert.Equal("Qwertina Hills", results[2].Name);
        Assert.Contains(results.Skip(3), item => item.Name == "Qwertnia");
    }

    private static Destination Destination(string name, string source) => new()
    {
        Id = Guid.NewGuid(), Name = name, Slug = $"{name.ToLowerInvariant().Replace(' ', '-')}-{Guid.NewGuid():N}",
        Type = "grad", Source = source, Region = "Testna regija", Description = "Dovoljno dug opis za test pretrage.",
        BestTimeToVisit = "proljeće", BudgetTier = "standard", SuggestedStayMinDays = 1,
        SuggestedStayMaxDays = 2, BestSeasons = ["spring"], Tags = ["grad"]
    };

    private static ApplicationDbContext CreateContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseNpgsql(Environment.GetEnvironmentVariable("TEST_POSTGIS_CONNECTION"), options => options.UseNetTopologySuite()).Options);
}
