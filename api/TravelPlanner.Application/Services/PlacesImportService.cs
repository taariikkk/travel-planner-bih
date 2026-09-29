using System.Globalization;
using TravelPlanner.Application.DTOs;
using TravelPlanner.Application.Interfaces;

namespace TravelPlanner.Application.Services;

public sealed class PlacesImportService(IPlacesImportRepository repository, IPlacesProvider provider,
    PlacesImportOptions options, TimeProvider clock, IPlacesImportDiagnostics diagnostics) : IPlacesImportService
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
        var lease = new PlacesImportLease(Guid.NewGuid(), signature);
        try
        {
            var acquisition = await repository.TryAcquireAsync(target.Id, lease, clock.GetUtcNow(),
                TimeSpan.FromHours(options.TtlHours), cancellationToken);
            if (acquisition.Status != PlacesLeaseStatus.Acquired)
                return new(acquisition.Status == PlacesLeaseStatus.Cached ? PlacesImportStatus.Cached : PlacesImportStatus.Deferred,
                    acquisition.RetryAt);
            if (acquisition.Lease != lease)
                throw new InvalidOperationException("Acquired import lease does not match its candidate token.");

            var data = await provider.GetPlacesAsync(new(target.Latitude.Value, target.Longitude.Value, options.RadiusMeters), cancellationToken);
            var committed = await repository.CompleteAsync(target.Id, lease, data, clock.GetUtcNow(), cancellationToken);
            return new(committed ? PlacesImportStatus.Refreshed : PlacesImportStatus.Deferred);
        }
        catch (PlacesProviderException exception)
        {
            var next = exception.Deferred ? clock.GetUtcNow() : clock.GetUtcNow().AddMinutes(options.FailureCooldownMinutes);
            if (exception.RetryAt > next) next = exception.RetryAt.Value;
            await ReleaseLeaseAsync(target.Id, lease, next, "provider failure");
            return new(PlacesImportStatus.Deferred, next);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            var next = clock.GetUtcNow().AddMinutes(options.FailureCooldownMinutes);
            await ReleaseLeaseAsync(target.Id, lease, next, "refresh timeout");
            return new(PlacesImportStatus.Deferred, next);
        }
        catch (Exception)
        {
            await ReleaseLeaseAsync(target.Id, lease, clock.GetUtcNow(), "unexpected import failure");
            throw;
        }
    }

    private async Task ReleaseLeaseAsync(Guid destinationId, PlacesImportLease lease,
        DateTimeOffset nextAttemptAt, string reason)
    {
        using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(options.CleanupTimeoutSeconds));
        try
        {
            await repository.FailAsync(destinationId, lease, nextAttemptAt, cleanup.Token);
        }
        catch (Exception exception)
        {
            diagnostics.CleanupFailed(destinationId, lease.Token, reason, exception);
        }
    }
}
