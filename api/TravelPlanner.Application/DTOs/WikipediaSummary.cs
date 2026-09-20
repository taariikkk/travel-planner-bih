namespace TravelPlanner.Application.DTOs;

public sealed record WikipediaSummary(string Text, string ArticleUrl, string License, bool IsSufficient);

public sealed record WikipediaSummaries(WikipediaSummary? Bosnian, WikipediaSummary? English);

public sealed record WikipediaSummaryResult(bool IsSuccess, WikipediaSummaries? Summaries, string? Error)
{
    public static WikipediaSummaryResult Success(WikipediaSummary? bosnian, WikipediaSummary? english) =>
        new(true, new WikipediaSummaries(bosnian, english), null);

    public static WikipediaSummaryResult Failure(string error) => new(false, null, error);
}
