using TravelPlanner.Application.DTOs;
using TravelPlanner.Application.Interfaces;
using TravelPlanner.Application.Exceptions;
using System.ComponentModel.DataAnnotations;
using TravelPlanner.Api.Domain.Entities;

namespace TravelPlanner.Application.Services;

public sealed class AuthService(IUserRepository users, IPasswordHasher passwordHasher, IJwtTokenService tokens) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        ValidateRegistration(request);
        var email = NormalizeEmail(request.Email);
        if (await users.GetByEmailAsync(email, cancellationToken) is not null)
        {
            throw new DuplicateEmailException();
        }

        var user = new User { Id = Guid.NewGuid(), Email = email, PasswordHash = passwordHasher.Hash(request.Password), DisplayName = request.DisplayName.Trim(), CreatedAt = DateTimeOffset.UtcNow };
        await users.AddAsync(user, cancellationToken);
        await users.SaveChangesAsync(cancellationToken);
        return CreateAuthResponse(user);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await users.GetByEmailAsync(NormalizeEmail(request.Email), cancellationToken);
        return user is not null && passwordHasher.Verify(request.Password, user.PasswordHash) ? CreateAuthResponse(user) : null;
    }

    public async Task<UserResponse?> GetProfileAsync(Guid userId, CancellationToken cancellationToken) =>
        (await users.GetByIdAsync(userId, cancellationToken)) is { } user ? ToResponse(user) : null;

    public async Task<UserResponse?> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName)) throw new ValidationException("Display name is required.");
        var user = await users.GetByIdAsync(userId, cancellationToken);
        if (user is null) return null;
        user.DisplayName = request.DisplayName.Trim();
        user.Preferences = request.Preferences ?? [];
        await users.SaveChangesAsync(cancellationToken);
        return ToResponse(user);
    }

    private AuthResponse CreateAuthResponse(User user)
    {
        var token = tokens.CreateToken(user.Id, user.Email, user.DisplayName, DateTimeOffset.UtcNow);
        return new AuthResponse(token.Value, token.ExpiresAt, ToResponse(user));
    }

    private static void ValidateRegistration(RegisterRequest request)
    {
        if (!new EmailAddressAttribute().IsValid(request.Email)) throw new ValidationException("A valid email is required.");
        if (string.IsNullOrWhiteSpace(request.DisplayName)) throw new ValidationException("Display name is required.");
        if (request.Password is null || request.Password.Length < 8) throw new ValidationException("Password must contain at least 8 characters.");
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
    private static UserResponse ToResponse(User user) => new(user.Id, user.Email, user.DisplayName, user.CreatedAt, user.Preferences);
}
