using TravelPlanner.Api.Domain.Entities;
using Xunit;

namespace TravelPlanner.Api.Tests.Domain;

public sealed class SavedPlaceTests
{
    [Fact]
    public void Creating_a_saved_place_without_a_target_is_rejected() =>
        Assert.Throws<ArgumentException>(() => new SavedPlace(Guid.NewGuid(), null, null));

    [Fact]
    public void Creating_a_saved_place_with_both_targets_is_rejected() =>
        Assert.Throws<ArgumentException>(() => new SavedPlace(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));

    [Fact]
    public void Creating_a_saved_place_with_one_target_is_allowed()
    {
        var userId = Guid.NewGuid();
        var destinationId = Guid.NewGuid();

        var savedPlace = new SavedPlace(userId, destinationId, null);

        Assert.Equal(userId, savedPlace.UserId);
        Assert.Equal(destinationId, savedPlace.DestinationId);
        Assert.Null(savedPlace.PlaceId);
        Assert.NotEqual(Guid.Empty, savedPlace.Id);
    }
}
