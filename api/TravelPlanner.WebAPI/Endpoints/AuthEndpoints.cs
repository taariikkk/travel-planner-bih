using TravelPlanner.Application.DTOs;
using TravelPlanner.Application.Interfaces;
using TravelPlanner.Application.Exceptions;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace TravelPlanner.WebAPI.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth");
        auth.MapPost("/register", RegisterAsync);
        auth.MapPost("/login", LoginAsync);

        var users = app.MapGroup("/api/users").RequireAuthorization();
        users.MapGet("/me", GetProfileAsync);
        users.MapPut("/me", UpdateProfileAsync);
        return app;
    }

    private static async Task<IResult> RegisterAsync(RegisterRequest request, IAuthService auth, CancellationToken cancellationToken)
    {
        try { return Results.Created("/api/users/me", await auth.RegisterAsync(request, cancellationToken)); }
        catch (DuplicateEmailException) { return Results.Conflict(new { message = "An account with this email already exists." }); }
        catch (ValidationException exception) { return Results.BadRequest(new { message = exception.Message }); }
    }

    private static async Task<IResult> LoginAsync(LoginRequest request, IAuthService auth, CancellationToken cancellationToken)
    {
        var result = await auth.LoginAsync(request, cancellationToken);
        return result is null ? Results.Unauthorized() : Results.Ok(result);
    }

    private static async Task<IResult> GetProfileAsync(ClaimsPrincipal principal, IAuthService auth, CancellationToken cancellationToken)
    {
        return TryGetUserId(principal, out var userId) && await auth.GetProfileAsync(userId, cancellationToken) is { } profile ? Results.Ok(profile) : Results.Unauthorized();
    }

    private static async Task<IResult> UpdateProfileAsync(UpdateProfileRequest request, ClaimsPrincipal principal, IAuthService auth, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId)) return Results.Unauthorized();
        try { return await auth.UpdateProfileAsync(userId, request, cancellationToken) is { } profile ? Results.Ok(profile) : Results.NotFound(); }
        catch (ValidationException exception) { return Results.BadRequest(new { message = exception.Message }); }
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"), out userId);
}
