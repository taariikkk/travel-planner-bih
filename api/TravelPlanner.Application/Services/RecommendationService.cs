using System.ComponentModel.DataAnnotations;
using TravelPlanner.Api.Domain.Entities;
using TravelPlanner.Application.DTOs;
using TravelPlanner.Application.Interfaces;

namespace TravelPlanner.Application.Services;

public sealed class RecommendationService(IUserRepository users, IDestinationRepository destinations) : IRecommendationService
{
    private static readonly HashSet<string> BudgetTiers = ["budget", "standard", "premium"];
    private static readonly HashSet<string> Seasons = ["spring", "summer", "autumn", "winter"];
    private static readonly HashSet<string> Languages = ["bs", "en"];

    public async Task<IReadOnlyList<DestinationRecommendationResponse>?> RecommendAsync(Guid userId, RecommendationRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        var user = await users.GetByIdAsync(userId, cancellationToken);
        if (user is null) return null;

        var language = request.Language.Trim().ToLowerInvariant();
        var preferences = user.Preferences.Select(preference => preference.Trim().ToLowerInvariant()).ToHashSet();
        var scored = (await destinations.GetAllWithTranslationsAsync(cancellationToken))
            .Select(destination => Score(destination, preferences, request))
            .OrderByDescending(item => item.Score)
            .ThenByDescending(item => item.MatchingTags.Count)
            .ThenBy(item => item.Destination.Name, StringComparer.Ordinal)
            .Take(5)
            .Select(item => ToResponse(item, language))
            .ToList();

        return scored;
    }

    private static ScoredDestination Score(Destination destination, HashSet<string> preferences, RecommendationRequest request)
    {
        var matchingTags = destination.Tags.Where(tag => preferences.Contains(tag)).ToList();
        var score = matchingTags.Count * 4;
        var seasonMatches = destination.BestSeasons.Contains(request.Season, StringComparer.OrdinalIgnoreCase);
        if (seasonMatches) score += 3;
        var budgetMatches = string.Equals(destination.BudgetTier, request.BudgetTier, StringComparison.OrdinalIgnoreCase);
        if (budgetMatches) score += 2;
        var durationMatches = request.TravelDays >= destination.SuggestedStayMinDays && request.TravelDays <= destination.SuggestedStayMaxDays;
        if (durationMatches) score += 2;

        return new(destination, score, matchingTags, seasonMatches, budgetMatches, durationMatches);
    }

    private static DestinationRecommendationResponse ToResponse(ScoredDestination item, string language)
    {
        var translation = item.Destination.Translations.SingleOrDefault(value => value.LanguageCode == language);
        var description = translation?.Description ?? item.Destination.Description;
        var bestTime = translation?.BestTimeToVisit ?? item.Destination.BestTimeToVisit;
        return new(
            item.Destination.Id,
            item.Destination.Name,
            item.Destination.Region,
            description,
            bestTime,
            item.Destination.Tags,
            item.Score,
            BuildReason(item, language));
    }

    private static string BuildReason(ScoredDestination item, string language)
    {
        if (item.Score == 0)
            return language == "en" ? "A curated starting suggestion for your trip." : "Kurirana početna preporuka za tvoje putovanje.";

        var reasons = new List<string>();
        if (item.MatchingTags.Count > 0)
            reasons.Add(language == "en" ? $"matches your interests: {string.Join(", ", item.MatchingTags)}" : $"odgovara interesovanjima: {string.Join(", ", item.MatchingTags)}");
        if (item.SeasonMatches) reasons.Add(language == "en" ? "fits the selected season" : "odgovara odabranoj sezoni");
        if (item.BudgetMatches) reasons.Add(language == "en" ? "matches your budget" : "odgovara budžetu");
        if (item.DurationMatches) reasons.Add(language == "en" ? "fits your trip length" : "odgovara trajanju putovanja");

        return language == "en" ? $"It {string.Join(" and ", reasons)}." : $"{string.Join(" i ", reasons).Replace("odgovara", "Odgovara", StringComparison.Ordinal)}.";
    }

    private static void Validate(RecommendationRequest request)
    {
        if (!BudgetTiers.Contains(request.BudgetTier.Trim().ToLowerInvariant())) throw new ValidationException("Budget tier must be budget, standard, or premium.");
        if (!Seasons.Contains(request.Season.Trim().ToLowerInvariant())) throw new ValidationException("Season must be spring, summer, autumn, or winter.");
        if (!Languages.Contains(request.Language.Trim().ToLowerInvariant())) throw new ValidationException("Language must be bs or en.");
        if (request.TravelDays is < 1 or > 14) throw new ValidationException("Travel days must be between 1 and 14.");
    }

    private sealed record ScoredDestination(Destination Destination, int Score, List<string> MatchingTags, bool SeasonMatches, bool BudgetMatches, bool DurationMatches);
}
