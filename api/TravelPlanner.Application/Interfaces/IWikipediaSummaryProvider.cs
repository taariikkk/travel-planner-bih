using TravelPlanner.Application.DTOs;

namespace TravelPlanner.Application.Interfaces;

public interface IWikipediaSummaryProvider
{
    Task<WikipediaSummaryResult> GetSummariesAsync(string? bosnianArticle, string? englishArticle, CancellationToken cancellationToken);
}
