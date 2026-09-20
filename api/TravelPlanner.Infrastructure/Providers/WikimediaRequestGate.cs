using Microsoft.Extensions.Options;

namespace TravelPlanner.Infrastructure.Providers;

public interface IWikimediaRequestGate
{
    Task WaitAsync(CancellationToken cancellationToken);
}

public sealed class WikimediaRequestGate(IOptions<WikimediaOptions> options, TimeProvider timeProvider) : IWikimediaRequestGate
{
    private readonly SemaphoreSlim mutex = new(1, 1);
    private readonly TimeSpan minimumInterval = TimeSpan.FromSeconds(1 / options.Value.RequestsPerSecond);
    private DateTimeOffset nextRequestAt = DateTimeOffset.MinValue;

    public async Task WaitAsync(CancellationToken cancellationToken)
    {
        await mutex.WaitAsync(cancellationToken);
        try
        {
            var now = timeProvider.GetUtcNow();
            if (nextRequestAt > now)
                await Task.Delay(nextRequestAt - now, timeProvider, cancellationToken);
            nextRequestAt = timeProvider.GetUtcNow() + minimumInterval;
        }
        finally
        {
            mutex.Release();
        }
    }
}
