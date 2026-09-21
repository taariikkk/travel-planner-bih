namespace TravelPlanner.Application.DTOs;

public sealed record SavePlaceRequest(Guid? DestinationId, Guid? PlaceId);

public sealed record SavedPlaceResponse(Guid Id, Guid? DestinationId, Guid? PlaceId, DateTimeOffset CreatedAt);
