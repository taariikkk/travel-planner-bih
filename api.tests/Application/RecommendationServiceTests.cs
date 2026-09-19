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
    public async Task Recommend_scores_tags_season_budget_and_duration_then_uses_stable_tie_breaking()
    {
        var user = new User { Id = Guid.NewGuid(), Preferences = ["historija", "hrana"] };
        var service = CreateService(user,
        [
            Destination("Zagorje", ["historija", "hrana"], "standard", 2, 3, ["summer"]),
            Destination("Alfa", ["historija", "hrana"], "standard", 2, 3, ["summer"]),
            Destination("Planina", ["priroda"], "premium", 1, 2, ["winter"])
        ]);

        var result = await service.RecommendAsync(user.Id, new("standard", 3, "summer", "bs"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(["Alfa", "Zagorje", "Planina"], result.Select(item => item.Name));
        Assert.Equal(15, result[0].Score);
        Assert.Equal(0, result[2].Score);
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
    public async Task Recommend_rejects_invalid_request(string budget, int days, string season, string language)
    {
        var user = new User { Id = Guid.NewGuid() };
        var service = CreateService(user, []);

        await Assert.ThrowsAsync<ValidationException>(() => service.RecommendAsync(user.Id, new(budget, days, season, language), CancellationToken.None));
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
