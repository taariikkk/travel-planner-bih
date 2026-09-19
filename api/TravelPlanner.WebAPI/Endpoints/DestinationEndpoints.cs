using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using TravelPlanner.Application.DTOs;
using TravelPlanner.Application.Interfaces;

namespace TravelPlanner.WebAPI.Endpoints;

public static class DestinationEndpoints
{
    public static IEndpointRouteBuilder MapDestinationEndpoints(this IEndpointRouteBuilder app)
    {
        var destinations = app.MapGroup("/api/destinations").RequireAuthorization();
        destinations.MapPost("/recommend", RecommendAsync);
        // Public curated destination content; personalized recommendations keep their existing authorization.
        destinations.MapGet("/{slug}", GetDetailsAsync).AllowAnonymous();
        return app;
    }

    private static async Task<IResult> GetDetailsAsync(string slug, string? language, IDestinationDetailsRepository repository, CancellationToken cancellationToken)
    {
        language ??= "bs";
        if (language is not ("bs" or "en")) return Results.BadRequest(new { message = "Podržani jezici su bs i en." });
        var destination = await repository.GetBySlugAsync(slug, language, cancellationToken);
        return destination is null ? Results.NotFound() : Results.Ok(destination);
    }

    private static async Task<IResult> RecommendAsync(RecommendationRequest request, ClaimsPrincipal principal, IRecommendationService recommendations, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"), out var userId)) return Results.Unauthorized();
        try
        {
            var result = await recommendations.RecommendAsync(userId, request, cancellationToken);
            return result is null ? Results.Unauthorized() : Results.Ok(result);
        }
        catch (ValidationException exception)
        {
            return Results.BadRequest(new { message = exception.Message });
        }
    }
}
