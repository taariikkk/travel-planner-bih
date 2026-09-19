using TravelPlanner.Application.DTOs;

namespace TravelPlanner.Application.Interfaces;

public interface IRecommendationService
{
    Task<IReadOnlyList<DestinationRecommendationResponse>?> RecommendAsync(Guid userId, RecommendationRequest request, CancellationToken cancellationToken);
}
