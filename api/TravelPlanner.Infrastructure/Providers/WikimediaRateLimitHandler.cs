namespace TravelPlanner.Infrastructure.Providers;

public sealed class WikimediaRateLimitHandler(IWikimediaRequestGate requestGate) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await requestGate.WaitAsync(cancellationToken);
        return await base.SendAsync(request, cancellationToken);
    }
}
