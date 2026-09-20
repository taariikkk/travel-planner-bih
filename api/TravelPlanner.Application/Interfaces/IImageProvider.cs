using TravelPlanner.Application.DTOs;

namespace TravelPlanner.Application.Interfaces;

public interface IImageProvider
{
    Task<DestinationImageResult> GetImageAsync(string? imageName, CancellationToken cancellationToken);
}
