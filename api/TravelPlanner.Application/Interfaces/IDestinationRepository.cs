using TravelPlanner.Api.Domain.Entities;

namespace TravelPlanner.Application.Interfaces;

public interface IDestinationRepository
{
    Task<IReadOnlyList<Destination>> GetAllWithTranslationsAsync(CancellationToken cancellationToken);
}
