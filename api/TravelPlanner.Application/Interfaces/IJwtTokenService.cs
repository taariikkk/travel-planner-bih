using TravelPlanner.Application.DTOs;
namespace TravelPlanner.Application.Interfaces;

public interface IJwtTokenService
{
    AccessToken CreateToken(Guid userId, string email, string displayName, DateTimeOffset now);
}
