using TravelPlanner.Application.DTOs;
using TravelPlanner.Application.Interfaces;
using TravelPlanner.Application.Exceptions;
using TravelPlanner.Infrastructure.Security;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace TravelPlanner.Api.Tests.Application.Security;

public sealed class JwtTokenServiceTests
{
    [Fact]
    public void CreateToken_contains_user_claims_and_expires_in_one_hour()
    {
        var service = new JwtTokenService(new JwtOptions
        {
            Key = "test-signing-key-that-is-at-least-thirty-two-characters-long",
            Issuer = "TravelPlanner.Api",
            Audience = "TravelPlanner.Web"
        });
        var now = DateTimeOffset.UtcNow;

        var token = service.CreateToken(Guid.Parse("a0d4cb3a-4847-45fb-9ad1-dc13c5ded052"), "user@example.com", "Tarik", now);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token.Value);

        Assert.Equal("TravelPlanner.Api", parsed.Issuer);
        Assert.Contains("TravelPlanner.Web", parsed.Audiences);
        Assert.Equal("a0d4cb3a-4847-45fb-9ad1-dc13c5ded052", parsed.Subject);
        Assert.Equal("user@example.com", parsed.Claims.Single(claim => claim.Type == ClaimTypes.Email).Value);
        Assert.Equal("Tarik", parsed.Claims.Single(claim => claim.Type == ClaimTypes.Name).Value);
        Assert.InRange(token.ExpiresAt, now.AddMinutes(59), now.AddMinutes(61));
    }

    [Fact]
    public void Validation_rejects_invalid_signature_issuer_audience_and_expired_tokens()
    {
        const string key = "test-signing-key-that-is-at-least-thirty-two-characters-long";
        var service = new JwtTokenService(new JwtOptions { Key = key, Issuer = "TravelPlanner.Api", Audience = "TravelPlanner.Web" });
        var validToken = service.CreateToken(Guid.NewGuid(), "user@example.com", "Tarik", DateTimeOffset.UtcNow);
        var expiredToken = service.CreateToken(Guid.NewGuid(), "user@example.com", "Tarik", DateTimeOffset.UtcNow.AddHours(-2));

        AssertInvalid(validToken.Value, key, "WrongIssuer", "TravelPlanner.Web");
        AssertInvalid(validToken.Value, key, "TravelPlanner.Api", "WrongAudience");
        AssertInvalid(validToken.Value, "another-signing-key-that-is-at-least-thirty-two-characters", "TravelPlanner.Api", "TravelPlanner.Web");
        AssertInvalid(expiredToken.Value, key, "TravelPlanner.Api", "TravelPlanner.Web");
    }

    private static void AssertInvalid(string token, string key, string issuer, string audience)
    {
        var handler = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        Assert.ThrowsAny<SecurityTokenException>(() => handler.ValidateToken(token, parameters, out _));
    }
}
