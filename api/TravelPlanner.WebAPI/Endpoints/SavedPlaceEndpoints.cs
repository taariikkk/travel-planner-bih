using System.Security.Claims;
using TravelPlanner.Api.Domain.Entities;
using TravelPlanner.Application.DTOs;
using TravelPlanner.Application.Interfaces;

namespace TravelPlanner.WebAPI.Endpoints;

public static class SavedPlaceEndpoints
{
    public static IEndpointRouteBuilder MapSavedPlaceEndpoints(this IEndpointRouteBuilder app)
    {
        var savedPlaces = app.MapGroup("/api/saved-places").RequireAuthorization();
        savedPlaces.MapGet("/", ListAsync);
        savedPlaces.MapPost("/", AddAsync);
        savedPlaces.MapDelete("/{id:guid}", RemoveAsync);
        return app;
    }

    private static async Task<IResult> ListAsync(ClaimsPrincipal principal, ISavedPlaceRepository repository, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();
        var savedPlaces = await repository.ListForUserAsync(userId, cancellationToken);
        return Results.Ok(savedPlaces.Select(ToResponse));
    }

    private static async Task<IResult> AddAsync(SavePlaceRequest? request, ClaimsPrincipal principal, ISavedPlaceRepository repository, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();
        if (request is null || (request.DestinationId is null) == (request.PlaceId is null))
            return Results.BadRequest(new { message = "Exactly one of destinationId or placeId must be provided." });

        var savedPlace = await repository.AddAsync(new SavedPlace(userId, request.DestinationId, request.PlaceId), cancellationToken);
        return Results.Ok(ToResponse(savedPlace));
    }

    private static async Task<IResult> RemoveAsync(Guid id, ClaimsPrincipal principal, ISavedPlaceRepository repository, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();
        try
        {
            return await repository.RemoveAsync(id, userId, cancellationToken) ? Results.NoContent() : Results.NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }
    }

    private static SavedPlaceResponse ToResponse(SavedPlace savedPlace) =>
        new(savedPlace.Id, savedPlace.DestinationId, savedPlace.PlaceId, savedPlace.CreatedAt);

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"), out userId);
}
