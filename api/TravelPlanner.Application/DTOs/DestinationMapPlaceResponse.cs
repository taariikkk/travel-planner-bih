namespace TravelPlanner.Application.DTOs;

public sealed record DestinationMapPlaceResponse(
    Guid Id,
    string Name,
    string Category,
    double Latitude,
    double Longitude,
    PlaceMetadataResponse? Metadata = null, string? ImageUrl = null, ImageAttributionResponse? ImageAttribution = null);
