using TravelPlanner.Application.DTOs;
namespace TravelPlanner.Application.Interfaces;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string passwordHash);
}
