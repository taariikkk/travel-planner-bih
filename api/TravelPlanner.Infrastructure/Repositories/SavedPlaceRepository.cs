using Microsoft.EntityFrameworkCore;
using TravelPlanner.Api.Domain.Entities;
using TravelPlanner.Api.Infrastructure.Persistence;
using TravelPlanner.Application.Interfaces;

namespace TravelPlanner.Infrastructure.Repositories;

public sealed class SavedPlaceRepository(ApplicationDbContext context) : ISavedPlaceRepository
{
    public async Task<SavedPlace> AddAsync(SavedPlace savedPlace, CancellationToken cancellationToken)
    {
        var existing = await FindExistingAsync(savedPlace.UserId, savedPlace.DestinationId, savedPlace.PlaceId, cancellationToken);
        if (existing is not null)
            return existing;

        await context.SavedPlaces.AddAsync(savedPlace, cancellationToken);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return savedPlace;
        }
        catch (DbUpdateException)
        {
            // The partial unique indexes also make concurrent requests idempotent.
            context.Entry(savedPlace).State = EntityState.Detached;
            var concurrentSavedPlace = await FindExistingAsync(savedPlace.UserId, savedPlace.DestinationId, savedPlace.PlaceId, cancellationToken);
            if (concurrentSavedPlace is not null)
                return concurrentSavedPlace;

            throw;
        }
    }

    public async Task<bool> RemoveAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var savedPlace = await context.SavedPlaces.SingleOrDefaultAsync(place => place.Id == id, cancellationToken);
        if (savedPlace is null)
            return false;

        if (savedPlace.UserId != userId)
            throw new UnauthorizedAccessException("A saved place can only be removed by its owner.");

        context.SavedPlaces.Remove(savedPlace);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<SavedPlace>> ListForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        await context.SavedPlaces
            .AsNoTracking()
            .Where(place => place.UserId == userId)
            .OrderByDescending(place => place.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsForDestinationAsync(Guid userId, Guid destinationId, CancellationToken cancellationToken) =>
        context.SavedPlaces.AnyAsync(place => place.UserId == userId && place.DestinationId == destinationId, cancellationToken);

    public Task<bool> ExistsForPlaceAsync(Guid userId, Guid placeId, CancellationToken cancellationToken) =>
        context.SavedPlaces.AnyAsync(place => place.UserId == userId && place.PlaceId == placeId, cancellationToken);

    private Task<SavedPlace?> FindExistingAsync(Guid userId, Guid? destinationId, Guid? placeId, CancellationToken cancellationToken) =>
        destinationId is { } destination
            ? context.SavedPlaces.SingleOrDefaultAsync(savedPlace => savedPlace.UserId == userId && savedPlace.DestinationId == destination, cancellationToken)
            : context.SavedPlaces.SingleOrDefaultAsync(savedPlace => savedPlace.UserId == userId && savedPlace.PlaceId == placeId, cancellationToken);
}
