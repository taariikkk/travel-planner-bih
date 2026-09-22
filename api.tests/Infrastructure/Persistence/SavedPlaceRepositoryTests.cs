using Microsoft.EntityFrameworkCore;
using TravelPlanner.Api.Domain.Entities;
using TravelPlanner.Api.Infrastructure.Persistence;
using TravelPlanner.Infrastructure.Repositories;
using Xunit;

namespace TravelPlanner.Api.Tests.Infrastructure.Persistence;

public sealed class SavedPlaceRepositoryTests
{
    [PostgisFact]
    public async Task Add_is_idempotent_and_uses_the_database_timestamp()
    {
        await using var context = CreateContext();
        var user = await AddUserAsync(context);
        var destinationId = await context.Destinations.Select(destination => destination.Id).FirstAsync();
        var repository = new SavedPlaceRepository(context);

        var first = await repository.AddAsync(new SavedPlace(user.Id, destinationId, null), default);
        var second = await repository.AddAsync(new SavedPlace(user.Id, destinationId, null), default);

        Assert.Equal(first.Id, second.Id);
        Assert.NotEqual(default, first.CreatedAt);
        Assert.True(await repository.ExistsForDestinationAsync(user.Id, destinationId, default));
        Assert.Equal(1, await context.SavedPlaces.CountAsync(saved => saved.UserId == user.Id));
        await DeleteUserAsync(context, user.Id);
    }

    [PostgisFact]
    public async Task Removing_another_users_saved_place_is_rejected()
    {
        await using var context = CreateContext();
        var owner = await AddUserAsync(context);
        var otherUser = await AddUserAsync(context);
        var destinationId = await context.Destinations.Select(destination => destination.Id).FirstAsync();
        var repository = new SavedPlaceRepository(context);
        var savedPlace = await repository.AddAsync(new SavedPlace(owner.Id, destinationId, null), default);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => repository.RemoveAsync(savedPlace.Id, otherUser.Id, default));
        Assert.True(await context.SavedPlaces.AnyAsync(saved => saved.Id == savedPlace.Id));
        await DeleteUserAsync(context, owner.Id, otherUser.Id);
    }

    [PostgisFact]
    public async Task List_returns_only_the_requesting_users_saved_places()
    {
        await using var context = CreateContext();
        var firstUser = await AddUserAsync(context);
        var secondUser = await AddUserAsync(context);
        var destinations = await context.Destinations.Select(destination => destination.Id).Take(2).ToListAsync();
        var repository = new SavedPlaceRepository(context);
        await repository.AddAsync(new SavedPlace(firstUser.Id, destinations[0], null), default);
        await repository.AddAsync(new SavedPlace(secondUser.Id, destinations[1], null), default);

        var savedPlaces = await repository.ListForUserAsync(firstUser.Id, default);

        var savedPlace = Assert.Single(savedPlaces);
        Assert.Equal(firstUser.Id, savedPlace.UserId);
        Assert.Equal(destinations[0], savedPlace.DestinationId);
        await DeleteUserAsync(context, firstUser.Id, secondUser.Id);
    }

    [PostgisFact]
    public async Task Database_enforces_exactly_one_target_and_unique_targets_per_user()
    {
        await using var context = CreateContext();
        var user = await AddUserAsync(context);
        var destinationId = await context.Destinations.Select(destination => destination.Id).FirstAsync();
        var place = new Place
        {
            Id = Guid.NewGuid(), DestinationLinks = [new() { DestinationId = destinationId, IsManual = true }], Name = "Saved place constraint test", Category = "attraction",
            Source = "manual", Location = new NetTopologySuite.Geometries.Point(18.429, 43.859) { SRID = 4326 }
        };
        context.Places.Add(place);
        await context.SaveChangesAsync();

        await Assert.ThrowsAnyAsync<Exception>(() => context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO \"SavedPlace\" (\"Id\", \"UserId\") VALUES ({Guid.NewGuid()}, {user.Id})"));
        await Assert.ThrowsAnyAsync<Exception>(() => context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO \"SavedPlace\" (\"Id\", \"UserId\", \"DestinationId\", \"PlaceId\") VALUES ({Guid.NewGuid()}, {user.Id}, {destinationId}, {place.Id})"));

        context.SavedPlaces.Add(new SavedPlace(user.Id, destinationId, null));
        await context.SaveChangesAsync();
        context.SavedPlaces.Add(new SavedPlace(user.Id, destinationId, null));
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => context.SaveChangesAsync());

        context.ChangeTracker.Clear();
        context.SavedPlaces.Add(new SavedPlace(user.Id, null, place.Id));
        await context.SaveChangesAsync();
        context.SavedPlaces.Add(new SavedPlace(user.Id, null, place.Id));
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => context.SaveChangesAsync());

        context.ChangeTracker.Clear();
        await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM \"SavedPlace\" WHERE \"UserId\" = {user.Id}");
        await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM \"Place\" WHERE \"Id\" = {place.Id}");
        await DeleteUserAsync(context, user.Id);
    }

    private static ApplicationDbContext CreateContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseNpgsql(Environment.GetEnvironmentVariable("TEST_POSTGIS_CONNECTION"), options => options.UseNetTopologySuite()).Options);

    private static async Task<User> AddUserAsync(ApplicationDbContext context)
    {
        var user = new User { Id = Guid.NewGuid(), Email = $"saved-place-{Guid.NewGuid():N}@example.test", PasswordHash = "hash", DisplayName = "Saved place test" };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    private static async Task DeleteUserAsync(ApplicationDbContext context, params Guid[] userIds)
    {
        foreach (var userId in userIds)
            await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM \"User\" WHERE \"Id\" = {userId}");
    }
}
