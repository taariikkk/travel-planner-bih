using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.RegularExpressions;
using TravelPlanner.Application.DTOs;
using TravelPlanner.Application.Interfaces;
using TravelPlanner.Application.Services;

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

    private static async Task<IResult> GetMapPlacesAsync(string slug, IDestinationMapRepository repository,
        HttpContext context, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();
        var logger = loggerFactory.CreateLogger("TravelPlanner.DestinationEndpoints");
        try
        {
            var places = await repository.GetPlacesBySlugAsync(slug, cancellationToken);
            logger.LogInformation(
                "Destination map places GET {RequestId} for {Slug} returned {StatusCode} in {ElapsedMs} ms",
                context.TraceIdentifier, slug, places is null ? StatusCodes.Status404NotFound : StatusCodes.Status200OK,
                timer.ElapsedMilliseconds);
            return places is null ? Results.NotFound() : Results.Ok(places);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Destination map places GET {RequestId} for {Slug} failed in {ElapsedMs} ms",
                context.TraceIdentifier, slug, timer.ElapsedMilliseconds);
            throw;
        }
    }

    private static async Task<IResult> RefreshPlacesAsync(string slug, IPlacesImportService importer,
        PlacesImportOptions options, HttpContext context, ILoggerFactory loggerFactory)
    {
        var timer = Stopwatch.StartNew();
        var logger = loggerFactory.CreateLogger("TravelPlanner.DestinationEndpoints");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(options.RefreshTimeoutSeconds));
        PlacesImportResult result;
        try
        {
            result = await importer.RefreshAsync(slug, timeout.Token);
        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "Destination places refresh POST {RequestId} for {Slug} failed in {ElapsedMs} ms",
                context.TraceIdentifier, slug, timer.ElapsedMilliseconds);
            throw;
        }
        var status = result.Status switch
        {
            PlacesImportStatus.Refreshed => "refreshed",
            PlacesImportStatus.Cached => "cached",
            PlacesImportStatus.Deferred => "deferred",
            PlacesImportStatus.NotFound => "not-found",
            _ => throw new InvalidOperationException("Unexpected places import status.")
        };
        logger.LogInformation(
            "Destination places refresh POST {RequestId} for {Slug} finished with {ImportStatus} in {ElapsedMs} ms",
            context.TraceIdentifier, slug, status, timer.ElapsedMilliseconds);
        return result.Status switch
        {
            PlacesImportStatus.NotFound => Results.NotFound(),
            PlacesImportStatus.Refreshed => Results.Ok(new PlacesRefreshResponse(status, result.RetryAt)),
            PlacesImportStatus.Cached => Results.Ok(new PlacesRefreshResponse(status, result.RetryAt)),
            PlacesImportStatus.Deferred => Results.Ok(new PlacesRefreshResponse(status, result.RetryAt)),
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

    private static async Task<IResult> GetDetailsAsync(string slug, string? language, IDestinationDetailsRepository repository,
        HttpContext context, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();
        var logger = loggerFactory.CreateLogger("TravelPlanner.DestinationEndpoints");
        language ??= "bs";
        if (language is not ("bs" or "en"))
        {
            logger.LogInformation(
                "Destination details GET {RequestId} for {Slug} returned {StatusCode} in {ElapsedMs} ms",
                context.TraceIdentifier, slug, StatusCodes.Status400BadRequest, timer.ElapsedMilliseconds);
            return Results.BadRequest(new { message = "Podržani jezici su bs i en." });
        }
        try
        {
            var destination = await repository.GetBySlugAsync(slug, language, cancellationToken);
            logger.LogInformation(
                "Destination details GET {RequestId} for {Slug} returned {StatusCode} in {ElapsedMs} ms",
                context.TraceIdentifier, slug, destination is null ? StatusCodes.Status404NotFound : StatusCodes.Status200OK,
                timer.ElapsedMilliseconds);
            return destination is null ? Results.NotFound() : Results.Ok(destination);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Destination details GET {RequestId} for {Slug} failed in {ElapsedMs} ms",
                context.TraceIdentifier, slug, timer.ElapsedMilliseconds);
            throw;
        }
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
