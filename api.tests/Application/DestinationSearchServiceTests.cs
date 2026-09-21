using TravelPlanner.Application.DTOs;
using TravelPlanner.Application.Interfaces;
using TravelPlanner.Application.Services;
using Xunit;

namespace TravelPlanner.Api.Tests.Application;

public sealed class DestinationSearchServiceTests
{
    [Fact]
    public async Task Sufficient_local_results_do_not_call_Wikidata()
    {
        var repository = new Repository(Items(6));
        var provider = new Provider(DestinationDataSearchResult.Success([]));
        var importer = new Importer();
        var service = new DestinationSearchService(repository, provider, importer);

        var result = await service.SearchAsync("Mostar", null, null, "bs", default);

        Assert.False(result.UsedFallback);
        Assert.Equal(6, result.Items.Count);
        Assert.Equal(0, provider.Calls);
        Assert.Empty(importer.Qids);
    }

    [Fact]
    public async Task Sparse_local_results_import_filtered_unique_Wikidata_candidates_and_requery()
    {
        var repository = new Repository(Items(1), Items(3));
        var provider = new Provider(DestinationDataSearchResult.Success([
            Data("Q1", "grad", "Hercegovina"), Data("Q1", "grad", "Hercegovina"),
            Data("Q2", "planina", "Hercegovina"), Data("Q3", "grad", "Krajina")
        ]));
        var importer = new Importer();
        var service = new DestinationSearchService(repository, provider, importer);

        var result = await service.SearchAsync("Trebinje", "grad", "Hercegovina", "bs", default);

        Assert.True(result.UsedFallback);
        Assert.Equal(["Q1"], importer.Qids);
        Assert.Equal(2, repository.SearchCalls);
        Assert.Equal(3, result.Items.Count);
    }

    [Fact]
    public async Task Wikidata_failure_preserves_local_results()
    {
        var local = Items(2);
        var service = new DestinationSearchService(
            new Repository(local), new Provider(DestinationDataSearchResult.Failure("unavailable")), new Importer());

        var result = await service.SearchAsync("Jajce", null, null, "en", default);

        Assert.True(result.UsedFallback);
        Assert.Equal(local, result.Items);
    }

    private static IReadOnlyList<DestinationSearchItemResponse> Items(int count) => Enumerable.Range(1, count)
        .Select(index => new DestinationSearchItemResponse(Guid.NewGuid(), $"place-{index}", $"Place {index}", "grad", "BiH", "Opis", "manual", null, null, null))
        .ToArray();

    private static DestinationData Data(string qid, string type, string region) =>
        new(qid, qid, qid, 43, 18, type, null, null, null, null, null, new(region, region));

    private sealed class Repository(params IReadOnlyList<DestinationSearchItemResponse>[] responses) : IDestinationSearchRepository
    {
        public int SearchCalls { get; private set; }
        public Task<IReadOnlyList<DestinationSearchItemResponse>> SearchAsync(string query, string? type, string? region, string language, int limit, CancellationToken cancellationToken)
        {
            var response = responses[Math.Min(SearchCalls, responses.Length - 1)];
            SearchCalls++;
            return Task.FromResult(response);
        }

        public Task<DestinationSearchFiltersResponse> GetFiltersAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new DestinationSearchFiltersResponse(["grad"], ["BiH"]));
    }

    private sealed class Provider(DestinationDataSearchResult result) : IDestinationDataProvider
    {
        public int Calls { get; private set; }
        public Task<DestinationDataSearchResult> SearchAsync(string searchText, CancellationToken cancellationToken) { Calls++; return Task.FromResult(result); }
        public Task<DestinationDataResult> GetByQidAsync(string qid, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class Importer : IDestinationImportService
    {
        public List<string> Qids { get; } = [];
        public Task<DestinationImportResult> ImportAsync(string qid, CancellationToken cancellationToken) { Qids.Add(qid); return Task.FromResult(DestinationImportResult.Success(qid.ToLowerInvariant())); }
    }
}
