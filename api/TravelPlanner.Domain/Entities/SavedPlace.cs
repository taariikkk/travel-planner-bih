namespace TravelPlanner.Api.Domain.Entities;

public sealed class SavedPlace
{
    private SavedPlace()
    {
    }

    public SavedPlace(Guid userId, Guid? destinationId, Guid? placeId)
    {
        if ((destinationId is null) == (placeId is null))
            throw new ArgumentException("Exactly one of destinationId or placeId must be provided.");

        Id = Guid.NewGuid();
        UserId = userId;
        DestinationId = destinationId;
        PlaceId = placeId;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? DestinationId { get; private set; }
    public Guid? PlaceId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public User User { get; private set; } = null!;
    public Destination? Destination { get; private set; }
    public Place? Place { get; private set; }
}
