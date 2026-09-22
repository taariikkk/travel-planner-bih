using TravelPlanner.Application.DTOs;
using TravelPlanner.Application.Interfaces;
using TravelPlanner.Application.Services;
using Xunit;

namespace TravelPlanner.Api.Tests.Application;

public sealed class PlacesImportServiceTests
{
    [Fact]
    public async Task Unknown_or_unlocated_destination_never_calls_provider()
    {
        var repo = new Repository(); var provider = new Provider();
        var service = Service(repo, provider);
        await service.RefreshAsync("missing", default);
        repo.Target = new(Guid.NewGuid(), null, null);
        await service.RefreshAsync("unlocated", default);
        Assert.Equal(0, provider.Calls);
    }
    [Fact]
    public async Task Fresh_or_locked_destination_does_not_call_provider()
    {
        var repo = new Repository { Target = new(Guid.NewGuid(), 43, 18), Grant = false };
        var provider = new Provider();
        await Service(repo, provider).RefreshAsync("test", default);
        Assert.Equal(0, provider.Calls);
    }
    [Fact]
    public async Task Empty_result_completes_and_signature_includes_coordinates_radius_and_version()
    {
        var repo = new Repository { Target = new(Guid.NewGuid(), 43, 18) };
        var provider = new Provider();
        await Service(repo, provider).RefreshAsync("test", default);
        Assert.Equal(1, provider.Calls);
        Assert.True(repo.Completed);
        Assert.Equal("osm-bih-v1:43:18:10000", repo.Signature);
        Assert.Equal(TimeSpan.FromDays(7), repo.Ttl);
    }
    [Fact]
    public async Task Failure_does_not_complete_and_preserves_larger_retry_after()
    {
        var retryAt = Now.AddHours(1);
        var repo = new Repository { Target = new(Guid.NewGuid(), 43, 18) };
        await Service(repo, new Provider { Error = new PlacesProviderException("429", retryAt) }).RefreshAsync("test", default);
        Assert.False(repo.Completed);
        Assert.Equal(retryAt, repo.NextAttempt);
    }
    [Fact]
    public async Task Normal_failure_backs_off_fifteen_minutes_but_budget_deferral_does_not()
    {
        var repo = new Repository { Target = new(Guid.NewGuid(), 43, 18) };
        await Service(repo, new Provider { Error = new PlacesProviderException("error") }).RefreshAsync("test", default);
        Assert.Equal(Now.AddMinutes(15), repo.NextAttempt);
        await Service(repo, new Provider { Error = new PlacesProviderException("budget", Now.AddSeconds(30), true) }).RefreshAsync("test", default);
        Assert.Equal(Now.AddSeconds(30), repo.NextAttempt);
    }
    private static readonly DateTimeOffset Now = new(2026, 9, 21, 12, 0, 0, TimeSpan.Zero);
    private static PlacesImportService Service(Repository repo, Provider provider) => new(repo, provider, new(), new Clock());
    private sealed class Clock : TimeProvider { public override DateTimeOffset GetUtcNow() => Now; }
    private sealed class Provider : IPlacesProvider
    {
        public int Calls { get; private set; }
        public Exception? Error { get; init; }
        public Task<PlacesData> GetPlacesAsync(PlacesQuery query, CancellationToken cancellationToken)
        { Calls++; return Error is null ? Task.FromResult(new PlacesData([])) : Task.FromException<PlacesData>(Error); }
    }
    private sealed class Repository : IPlacesImportRepository
    {
        public PlacesImportTarget? Target { get; set; }
        public bool Grant { get; init; } = true;
        public bool Completed { get; private set; }
        public string? Signature { get; private set; }
        public TimeSpan Ttl { get; private set; }
        public DateTimeOffset? NextAttempt { get; private set; }
        public Task<PlacesImportTarget?> GetTargetAsync(string slug, CancellationToken cancellationToken) => Task.FromResult(Target);
        public Task<PlacesImportLease?> TryAcquireAsync(Guid id, string signature, DateTimeOffset now, TimeSpan ttl, CancellationToken cancellationToken)
        { Signature = signature; Ttl = ttl; return Task.FromResult(Grant ? new PlacesImportLease(Guid.NewGuid(), signature) : null); }
        public Task CompleteAsync(Guid id, PlacesImportLease lease, PlacesData data, DateTimeOffset now, CancellationToken cancellationToken)
        { Completed = true; return Task.CompletedTask; }
        public Task FailAsync(Guid id, PlacesImportLease lease, DateTimeOffset next, CancellationToken cancellationToken)
        { NextAttempt = next; return Task.CompletedTask; }
    }
}
