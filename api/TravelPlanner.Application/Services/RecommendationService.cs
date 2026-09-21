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
        var preferences = user.Preferences
            .Where(preference => !string.IsNullOrWhiteSpace(preference))
            .Select(preference => preference.Trim().ToLowerInvariant())
            .ToHashSet();
        var scored = (await destinations.GetAllWithTranslationsAsync(cancellationToken))
            .Where(destination => destination.IsRecommendationEligible)
            .Select(destination => Score(destination, preferences, request))
            .OrderByDescending(item => item.Score)
            // A deterministic secondary order keeps equally scored results stable across requests.
            .ThenBy(item => item.Destination.Name, StringComparer.Ordinal)
            .ThenBy(item => item.Destination.Id)
            .Take(5)
            .Select(item => ToResponse(item, language))
            .ToList();

        return scored;
    }

    private static ScoredDestination Score(Destination destination, HashSet<string> preferences, RecommendationRequest request)
    {
        var matchingTags = destination.Tags
            .Where(tag => !string.IsNullOrWhiteSpace(tag) && preferences.Contains(tag.Trim().ToLowerInvariant()))
            .Select(tag => tag.Trim())
            .ToList();
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
        var description = translation?.Description
            ?? (language == "en" ? item.Destination.DescriptionEn : null)
            ?? item.Destination.Description;
        var bestTime = translation?.BestTimeToVisit ?? item.Destination.BestTimeToVisit;
        return new(
            item.Destination.Id,
            item.Destination.Slug,
            item.Destination.Name,
            item.Destination.Region,
            description,
            bestTime,
            item.Destination.Tags,
            item.Score,
            new RecommendationReasonsResponse(
                item.MatchingTags,
                item.SeasonMatches,
                item.BudgetMatches,
                item.DurationMatches));
    }

    private static void Validate(RecommendationRequest request)
    {
        if (!IsAllowed(request.BudgetTier, BudgetTiers)) throw new ValidationException("Budget tier must be budget, standard, or premium.");
        if (!IsAllowed(request.Season, Seasons)) throw new ValidationException("Season must be spring, summer, autumn, or winter.");
        if (!IsAllowed(request.Language, Languages)) throw new ValidationException("Language must be bs or en.");
        if (request.TravelDays is < 1 or > 14) throw new ValidationException("Travel days must be between 1 and 14.");
    }

    private static bool IsAllowed(string? value, HashSet<string> allowedValues) =>
        !string.IsNullOrWhiteSpace(value) && allowedValues.Contains(value.Trim().ToLowerInvariant());

    private sealed record ScoredDestination(Destination Destination, int Score, List<string> MatchingTags, bool SeasonMatches, bool BudgetMatches, bool DurationMatches);
}
