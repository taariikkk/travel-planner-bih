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
        return app;
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
