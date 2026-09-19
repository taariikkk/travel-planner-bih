using System.ComponentModel.DataAnnotations;
using TravelPlanner.Api.Domain.Entities;
using TravelPlanner.Application.DTOs;
using TravelPlanner.Application.Interfaces;
using TravelPlanner.Application.Services;
using Xunit;

namespace TravelPlanner.Api.Tests.Application;

public sealed class RecommendationServiceTests
{
    [Fact]
    public async Task Recommend_scores_normalized_matching_tags_and_exposes_structured_reasons()
    {
        var user = new User { Id = Guid.NewGuid(), Preferences = [" planina ", "priroda"] };
        var service = CreateService(user,
        [
            Destination("Bjelašnica", ["Planina", "priroda", "avantura"], "standard", 2, 3, ["summer"])
        ]);

        var result = await service.RecommendAsync(user.Id, new("standard", 3, "summer", "bs"), CancellationToken.None);

        var recommendation = Assert.Single(result!);
        Assert.Equal(15, recommendation.Score);
        Assert.Equal(["Planina", "priroda"], recommendation.Reasons.MatchingTags);
        Assert.True(recommendation.Reasons.MatchesSeason);
        Assert.True(recommendation.Reasons.MatchesBudget);
        Assert.True(recommendation.Reasons.MatchesDuration);
    }

    [Fact]
    public async Task Recommend_applies_season_budget_and_duration_weights_independently()
    {
        var user = new User { Id = Guid.NewGuid() };
        var service = CreateService(user,
        [
            Destination("Sezona", [], "premium", 7, 8, ["summer"]),
            Destination("Budžet", [], "standard", 7, 8, ["winter"]),
            Destination("Trajanje", [], "premium", 2, 4, ["winter"])
        ]);

        var result = await service.RecommendAsync(user.Id, new("standard", 3, "summer", "bs"), CancellationToken.None);

        var scores = result!.ToDictionary(item => item.Name, item => item.Score);
        Assert.Equal(3, scores["Sezona"]);
        Assert.Equal(2, scores["Budžet"]);
        Assert.Equal(2, scores["Trajanje"]);
    }

    [Fact]
    public async Task Recommend_orders_by_score_and_uses_a_deterministic_order_for_ties()
    {
        var user = new User { Id = Guid.NewGuid(), Preferences = ["historija"] };
        var service = CreateService(user,
        [
            Destination("Zeta", ["historija"], "premium", 7, 8, ["summer"]),
            Destination("Alfa", ["historija"], "premium", 7, 8, ["summer"]),
            Destination("Beta", ["historija"], "standard", 7, 8, ["winter"]),
            Destination("Gama", [], "premium", 7, 8, ["summer"])
        ]);

        var result = await service.RecommendAsync(user.Id, new("standard", 3, "summer", "bs"), CancellationToken.None);

        var recommendations = result!;
        Assert.Equal(["Alfa", "Zeta", "Beta", "Gama"], recommendations.Select(item => item.Name));
        Assert.Equal([7, 7, 6, 3], recommendations.Select(item => item.Score));
    }

    [Fact]
    public async Task Recommend_returns_curated_zero_score_results_when_user_has_no_matching_preferences()
    {
        var user = new User { Id = Guid.NewGuid(), Preferences = ["vino"] };
        var service = CreateService(user,
        [
            Destination("Zeta", ["priroda"], "premium", 7, 8, ["winter"]),
            Destination("Alfa", ["historija"], "premium", 7, 8, ["winter"])
        ]);

        var result = await service.RecommendAsync(user.Id, new("standard", 3, "summer", "en"), CancellationToken.None);

        var recommendations = result!;
        Assert.Equal(["Alfa", "Zeta"], recommendations.Select(item => item.Name));
        Assert.All(recommendations, item =>
        {
            Assert.Equal(0, item.Score);
            Assert.Empty(item.Reasons.MatchingTags);
            Assert.False(item.Reasons.MatchesSeason);
            Assert.False(item.Reasons.MatchesBudget);
            Assert.False(item.Reasons.MatchesDuration);
        });
    }

    [Fact]
    public async Task Recommend_uses_non_interest_criteria_when_user_selected_no_interests()
    {
        var user = new User { Id = Guid.NewGuid(), Preferences = [] };
        var service = CreateService(user,
        [
            Destination("Mostar", ["historija"], "standard", 2, 3, ["summer"])
        ]);

        var result = await service.RecommendAsync(user.Id, new("standard", 3, "summer", "bs"), CancellationToken.None);

        var recommendation = Assert.Single(result!);
        Assert.Equal(7, recommendation.Score);
        Assert.Empty(recommendation.Reasons.MatchingTags);
        Assert.True(recommendation.Reasons.MatchesSeason);
        Assert.True(recommendation.Reasons.MatchesBudget);
        Assert.True(recommendation.Reasons.MatchesDuration);
    }

    [Fact]
    public async Task Recommend_returns_localized_translation_and_only_top_five()
    {
        var user = new User { Id = Guid.NewGuid(), Preferences = ["historija"] };
        var destinations = Enumerable.Range(1, 6).Select(index => Destination($"Grad {index}", ["historija"], "standard", 1, 4, ["summer"])).ToList();
        destinations[0].Translations = [new() { LanguageCode = "en", Description = "English description", BestTimeToVisit = "Summer" }];
        var service = CreateService(user, destinations);

        var result = await service.RecommendAsync(user.Id, new("standard", 2, "summer", "en"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(5, result.Count);
        Assert.Equal("English description", result[0].Description);
    }

    [Theory]
    [InlineData("luxury", 3, "summer", "bs")]
    [InlineData("standard", 0, "summer", "bs")]
    [InlineData("standard", 3, "monsoon", "bs")]
    [InlineData("standard", 3, "summer", "de")]
    [InlineData(null, 3, "summer", "bs")]
    [InlineData("standard", 3, null, "bs")]
    [InlineData("standard", 3, "summer", null)]
    public async Task Recommend_rejects_invalid_request(string? budget, int days, string? season, string? language)
    {
        var user = new User { Id = Guid.NewGuid() };
        var service = CreateService(user, []);

        await Assert.ThrowsAsync<ValidationException>(() => service.RecommendAsync(user.Id, new(budget!, days, season!, language!), CancellationToken.None));
    }

    private static RecommendationService CreateService(User user, IReadOnlyList<Destination> destinations) => new(new Users(user), new Destinations(destinations));

    private static Destination Destination(string name, List<string> tags, string budget, int minDays, int maxDays, List<string> seasons) => new()
    {
        Id = Guid.NewGuid(), Name = name, Region = "BiH", Description = "Opis", BestTimeToVisit = "Ljeto", Tags = tags,
        BudgetTier = budget, SuggestedStayMinDays = minDays, SuggestedStayMaxDays = maxDays, BestSeasons = seasons
    };

    private sealed class Users(User user) : IUserRepository
    {
        public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) => Task.FromResult<User?>(user);
        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<User?>(user.Id == id ? user : null);
        public Task AddAsync(User newUser, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class Destinations(IReadOnlyList<Destination> values) : IDestinationRepository
    {
        public Task<IReadOnlyList<Destination>> GetAllWithTranslationsAsync(CancellationToken cancellationToken) => Task.FromResult(values);
    }
}
