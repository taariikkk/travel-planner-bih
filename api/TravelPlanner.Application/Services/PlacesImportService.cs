using System.Globalization;
using TravelPlanner.Application.DTOs;
using TravelPlanner.Application.Interfaces;

namespace TravelPlanner.Application.Services;

public sealed class PlacesImportService(IPlacesImportRepository repository, IPlacesProvider provider,
    PlacesImportOptions options, TimeProvider clock) : IPlacesImportService
{
    public async Task<PlacesImportResult> RefreshAsync(string slug, CancellationToken cancellationToken)
    {
        var target = await repository.GetTargetAsync(slug, cancellationToken);
        if (target is null) return new(PlacesImportStatus.NotFound);
        if (target is not { Latitude: not null, Longitude: not null }
            || !double.IsFinite(target.Latitude.Value) || !double.IsFinite(target.Longitude.Value)
            || Math.Abs(target.Latitude.Value) > 90 || Math.Abs(target.Longitude.Value) > 180) return new(PlacesImportStatus.Deferred);
        var signature = string.Create(CultureInfo.InvariantCulture,
            $"{PlacesImportOptions.QueryVersion}:{target.Latitude:R}:{target.Longitude:R}:{options.RadiusMeters}");
        var acquisition = await repository.TryAcquireAsync(target.Id, signature, clock.GetUtcNow(),
            TimeSpan.FromHours(options.TtlHours), cancellationToken);
        if (acquisition.Status != PlacesLeaseStatus.Acquired)
            return new(acquisition.Status == PlacesLeaseStatus.Cached ? PlacesImportStatus.Cached : PlacesImportStatus.Deferred,
                acquisition.RetryAt);
        var lease = acquisition.Lease ?? throw new InvalidOperationException("Acquired import lease is missing.");
        try
        {
            var data = await provider.GetPlacesAsync(new(target.Latitude.Value, target.Longitude.Value, options.RadiusMeters), cancellationToken);
            var committed = await repository.CompleteAsync(target.Id, lease, data, clock.GetUtcNow(), cancellationToken);
            return new(committed ? PlacesImportStatus.Refreshed : PlacesImportStatus.Deferred);
        }
        catch (PlacesProviderException exception)
        {
            var next = exception.Deferred ? clock.GetUtcNow() : clock.GetUtcNow().AddMinutes(options.FailureCooldownMinutes);
            if (exception.RetryAt > next) next = exception.RetryAt.Value;
            await repository.FailAsync(target.Id, lease, next, cancellationToken);
            return new(PlacesImportStatus.Deferred, next);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // A short independent cleanup releases the lease even after the HTTP client disconnects.
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await repository.FailAsync(target.Id, lease, clock.GetUtcNow(), cleanup.Token);
            throw;
        }
    }
}
