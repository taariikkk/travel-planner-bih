using TravelPlanner.Application.DTOs;
using TravelPlanner.Application.Interfaces;
using TravelPlanner.Application.Exceptions;
using TravelPlanner.Infrastructure.Security;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
}
