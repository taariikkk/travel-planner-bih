using Microsoft.EntityFrameworkCore;
using TravelPlanner.Api.Domain.Entities;

using TravelPlanner.Application.Interfaces;
using TravelPlanner.Api.Infrastructure.Persistence;

namespace TravelPlanner.Infrastructure.Repositories;

public sealed class UserRepository(ApplicationDbContext context) : IUserRepository
{
    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        context.Users.SingleOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Users.SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

    public Task AddAsync(User user, CancellationToken cancellationToken) => context.Users.AddAsync(user, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
