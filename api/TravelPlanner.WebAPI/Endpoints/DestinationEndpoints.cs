using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.RegularExpressions;
using TravelPlanner.Application.DTOs;
using TravelPlanner.Application.Interfaces;

namespace TravelPlanner.WebAPI.Endpoints;

public static partial class DestinationEndpoints
{
    public static IEndpointRouteBuilder MapDestinationEndpoints(this IEndpointRouteBuilder app)
    {
        var destinations = app.MapGroup("/api/destinations").RequireAuthorization();
        destinations.MapPost("/recommend", RecommendAsync);
        destinations.MapPost("/import", ImportAsync).AllowAnonymous().RequireRateLimiting("destination-import");
        // Public curated destination content; personalized recommendations keep their existing authorization.
        destinations.MapGet("/{slug}", GetDetailsAsync).AllowAnonymous();
        destinations.MapGet("/{slug}/map-places", GetMapPlacesAsync).AllowAnonymous();
        destinations.MapPost("/{slug}/places/refresh", RefreshPlacesAsync).AllowAnonymous()
            .RequireRateLimiting("places-refresh");
        return app;
    }

    private static async Task<IResult> GetMapPlacesAsync(string slug, IDestinationMapRepository repository, CancellationToken cancellationToken)
    {
        var places = await repository.GetPlacesBySlugAsync(slug, cancellationToken);
        return places is null ? Results.NotFound() : Results.Ok(places);
    }

    private static async Task<IResult> RefreshPlacesAsync(string slug, IPlacesImportService importer, CancellationToken cancellationToken)
    {
        var result = await importer.RefreshAsync(slug, cancellationToken);
        return result.Status switch
        {
            PlacesImportStatus.NotFound => Results.NotFound(),
            PlacesImportStatus.Refreshed => Results.Ok(new PlacesRefreshResponse("refreshed", result.RetryAt)),
            PlacesImportStatus.Cached => Results.Ok(new PlacesRefreshResponse("cached", result.RetryAt)),
            PlacesImportStatus.Deferred => Results.Ok(new PlacesRefreshResponse("deferred", result.RetryAt)),
            _ => throw new InvalidOperationException("Unexpected places import status.")
        };
    }

    private static async Task<IResult> ImportAsync(DestinationImportRequest? request, IDestinationImportService importer, CancellationToken cancellationToken)
    {
        if (request?.Qid is null || !QidPattern().IsMatch(request.Qid))
            return Results.BadRequest(new { message = "Q-id mora biti u formatu Q i cifre." });

        var result = await importer.ImportAsync(request.Qid, cancellationToken);
        return result.IsSuccess
            ? Results.Ok(new DestinationImportResponse(result.Slug!))
            : Results.BadRequest(new { message = result.Error });
    }

    private static async Task<IResult> GetDetailsAsync(string slug, string? language, IDestinationDetailsRepository repository, CancellationToken cancellationToken)
    {
        language ??= "bs";
        if (language is not ("bs" or "en")) return Results.BadRequest(new { message = "Podržani jezici su bs i en." });
        var destination = await repository.GetBySlugAsync(slug, language, cancellationToken);
        return destination is null ? Results.NotFound() : Results.Ok(destination);
    }

    private static async Task<IResult> RecommendAsync(RecommendationRequest? request, ClaimsPrincipal principal, IRecommendationService recommendations, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"), out var userId)) return Results.Unauthorized();
        if (request is null) return Results.BadRequest(new { message = "Recommendation request is required." });
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

    [GeneratedRegex("^Q\\d+$", RegexOptions.CultureInvariant)]
    private static partial Regex QidPattern();
}
