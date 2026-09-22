using System.Globalization;
using TravelPlanner.Application.DTOs;
using TravelPlanner.Application.Interfaces;

namespace TravelPlanner.Application.Services;

public sealed class PlacesImportService(IPlacesImportRepository repository, IPlacesProvider provider,
    PlacesImportOptions options, TimeProvider clock) : IPlacesImportService
{
    public async Task RefreshAsync(string slug, CancellationToken cancellationToken)
    {
        var target = await repository.GetTargetAsync(slug, cancellationToken);
        if (target is not { Latitude: not null, Longitude: not null }
            || !double.IsFinite(target.Latitude.Value) || !double.IsFinite(target.Longitude.Value)
            || Math.Abs(target.Latitude.Value) > 90 || Math.Abs(target.Longitude.Value) > 180) return;
        var signature = string.Create(CultureInfo.InvariantCulture,
            $"{PlacesImportOptions.QueryVersion}:{target.Latitude:R}:{target.Longitude:R}:{options.RadiusMeters}");
        var lease = await repository.TryAcquireAsync(target.Id, signature, clock.GetUtcNow(),
            TimeSpan.FromHours(options.TtlHours), cancellationToken);
        if (lease is null) return;
        try
        {
            var data = await provider.GetPlacesAsync(new(target.Latitude.Value, target.Longitude.Value, options.RadiusMeters), cancellationToken);
            await repository.CompleteAsync(target.Id, lease, data, clock.GetUtcNow(), cancellationToken);
        }
        catch (PlacesProviderException exception)
        {
            var next = exception.Deferred ? clock.GetUtcNow() : clock.GetUtcNow().AddMinutes(options.FailureCooldownMinutes);
            if (exception.RetryAt > next) next = exception.RetryAt.Value;
            await repository.FailAsync(target.Id, lease, next, cancellationToken);
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
