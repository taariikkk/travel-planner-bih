namespace TravelPlanner.Application.DTOs;

public sealed record DestinationImage(string Url, string? Author, string? License, string? SourceUrl);

public sealed record DestinationImageResult(bool IsSuccess, DestinationImage? Image, string? Error)
{
    public static DestinationImageResult Success(DestinationImage? image) => new(true, image, null);
    public static DestinationImageResult Failure(string error) => new(false, null, error);
}
