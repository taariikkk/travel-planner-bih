using TravelPlanner.Application.Interfaces;

namespace TravelPlanner.WebAPI.Endpoints;

public static class SearchEndpoints
{
    private const int MaximumQueryLength = 100;

    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/search", SearchAsync)
            .AllowAnonymous()
            .RequireRateLimiting("destination-search");
        return app;
    }

    private static async Task<IResult> SearchAsync(
        string? q, string? type, string? region, string? language,
        IDestinationSearchService search, CancellationToken cancellationToken)
    {
        language ??= "bs";
        if (language is not ("bs" or "en"))
            return Results.BadRequest(new { message = "Podržani jezici su bs i en." });
        if (q?.Trim().Length > MaximumQueryLength)
            return Results.BadRequest(new { message = $"Upit može imati najviše {MaximumQueryLength} znakova." });

        return Results.Ok(await search.SearchAsync(q, type, region, language, cancellationToken));
    }
}
