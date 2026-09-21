using TravelPlanner.Api.Domain.Entities;
using TravelPlanner.Application.DTOs;
using TravelPlanner.Application.Interfaces;
using TravelPlanner.Application.Services;
using Xunit;

namespace TravelPlanner.Api.Tests.Application;

public sealed class DestinationImportServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Import_maps_data_defaults_attribution_and_diacritic_free_slug()
    {
        var repository = new ImportRepository();
        var service = CreateService(repository, Data(name: "Đurđevik Čaršija"));

        var result = await service.ImportAsync("Q123", default);

        Assert.True(result.IsSuccess);
        Assert.Equal("djurdjevik-carsija", result.Slug);
        var destination = Assert.Single(repository.Items);
        Assert.Equal("grad", destination.Type);
        Assert.Equal("Kanton test", destination.Region);
        Assert.Equal(518, destination.ElevationM);
        Assert.Equal(43.859, destination.Latitude);
        Assert.Equal(18.429, destination.Longitude);
        Assert.Equal(275524, destination.Population);
        Assert.Equal("Bosanski dovoljno dug opis", destination.Description);
        Assert.Equal("English sufficient description", destination.DescriptionEn);
        Assert.Equal("https://bs.wikipedia.test/Test", destination.DescriptionSourceUrl);
        Assert.Equal("CC BY-SA", destination.DescriptionLicense);
        Assert.Equal("CC BY-SA", destination.DescriptionEnLicense);
        Assert.Equal("https://images.test/test.jpg", destination.ImageUrl);
        Assert.Equal("wikidata", destination.Source);
        Assert.Equal("Q123", destination.ExternalId);
        Assert.Equal(Now, destination.ImportedAt);
        Assert.Equal(["grad", "kultura"], destination.Tags);
        Assert.Equal("standard", destination.BudgetTier);
        Assert.True(destination.IsRecommendationEligible);
    }

    [Fact]
    public async Task Slug_collision_uses_region_then_qid_and_existing_slug_is_stable()
    {
        var repository = new ImportRepository(
        [ExistingDestination("Q1", "bjelasnica"), ExistingDestination("Q2", "bjelasnica-kanton-test")]);
        var service = CreateService(repository, Data(name: "Bjelašnica"));

        var result = await service.ImportAsync("Q333", default);

        Assert.Equal("bjelasnica-q333", result.Slug);
        var imported = repository.Items.Single(item => item.ExternalId == "Q333");
        imported.ImportedAt = Now.AddDays(-30);
        imported.Name = "Promijenjen naziv";
        await service.ImportAsync("Q333", default);
        Assert.Equal("bjelasnica-q333", imported.Slug);
    }

    [Fact]
    public async Task Refresh_never_overwrites_manual_fields_including_english_description()
    {
        var existing = ExistingDestination("Q444", "rucni-slug");
        existing.Source = "manual";
        existing.ImportedAt = Now.AddDays(-30);
        existing.Description = "Ručni opis";
        existing.DescriptionEn = "Manual description";
        existing.DescriptionSourceUrl = "https://manual.test/bs";
        existing.DescriptionEnSourceUrl = "https://manual.test/en";
        existing.DescriptionLicense = "Ručna licenca";
        existing.DescriptionEnLicense = "Manual license";
        existing.Type = "planina";
        existing.Latitude = 44.1;
        existing.Longitude = 17.1;
        existing.Region = "Stara regija";
        existing.ManualOverrideFields =
        [
            nameof(Destination.Description), nameof(Destination.DescriptionSourceUrl),
            nameof(Destination.DescriptionLicense), nameof(Destination.DescriptionEn),
            nameof(Destination.DescriptionEnSourceUrl), nameof(Destination.DescriptionEnLicense), nameof(Destination.Type),
            nameof(Destination.Latitude), nameof(Destination.Longitude)
        ];
        var repository = new ImportRepository([existing]);
        var service = CreateService(repository, Data());

        await service.ImportAsync("Q444", default);

        Assert.Equal("Ručni opis", existing.Description);
        Assert.Equal("Manual description", existing.DescriptionEn);
        Assert.Equal("https://manual.test/bs", existing.DescriptionSourceUrl);
        Assert.Equal("https://manual.test/en", existing.DescriptionEnSourceUrl);
        Assert.Equal("Ručna licenca", existing.DescriptionLicense);
        Assert.Equal("Manual license", existing.DescriptionEnLicense);
        Assert.Equal("planina", existing.Type);
        Assert.Equal(44.1, existing.Latitude);
        Assert.Equal(17.1, existing.Longitude);
        Assert.Equal("Kanton test", existing.Region);
        Assert.Equal("manual", existing.Source);
    }

    [Fact]
    public async Task Fresh_import_within_TTL_returns_without_provider_calls()
    {
        var existing = ExistingDestination("Q555", "postojeci");
        existing.ImportedAt = Now.AddHours(-1);
        var repository = new ImportRepository([existing]);
        var wikidata = new DestinationProvider(Data());
        var service = CreateService(repository, wikidata: wikidata);

        var result = await service.ImportAsync("Q555", default);

        Assert.Equal("postojeci", result.Slug);
        Assert.Equal(0, wikidata.CallCount);
    }

    [Fact]
    public async Task Import_at_the_TTL_boundary_refreshes_provider_data()
    {
        var existing = ExistingDestination("Q556", "istekao");
        existing.ImportedAt = Now.AddHours(-24);
        var repository = new ImportRepository([existing]);
        var wikidata = new DestinationProvider(Data());
        var service = CreateService(repository, wikidata: wikidata);

        await service.ImportAsync("Q556", default);

        Assert.Equal(1, wikidata.CallCount);
    }

    [Fact]
    public async Task Import_succeeds_without_an_image_and_keeps_the_destination_usable()
    {
        var repository = new ImportRepository();
        var service = new DestinationImportService(
            new DestinationProvider(Data()), new SummaryProvider(true), new UnavailableImageProvider(), repository,
            new DestinationImportOptions { TtlHours = 24, DefaultBudgetTier = "standard", DefaultSuggestedStayMinDays = 1, DefaultSuggestedStayMaxDays = 3 },
            new FixedTimeProvider(Now));

        var result = await service.ImportAsync("Q888", default);

        Assert.True(result.IsSuccess);
        var destination = Assert.Single(repository.Items);
        Assert.Null(destination.ImageUrl);
        Assert.True(destination.IsRecommendationEligible);
    }

    [Fact]
    public async Task Import_succeeds_without_Wikipedia_and_excludes_the_destination_from_recommendations()
    {
        var repository = new ImportRepository();
        var service = new DestinationImportService(
            new DestinationProvider(Data()), new UnavailableSummaryProvider(), new ImageProvider(), repository,
            new DestinationImportOptions { TtlHours = 24, DefaultBudgetTier = "standard", DefaultSuggestedStayMinDays = 1, DefaultSuggestedStayMaxDays = 3 },
            new FixedTimeProvider(Now));

        var result = await service.ImportAsync("Q889", default);

        Assert.True(result.IsSuccess);
        var destination = Assert.Single(repository.Items);
        Assert.Empty(destination.Description);
        Assert.False(destination.IsRecommendationEligible);
        Assert.Equal("https://images.test/test.jpg", destination.ImageUrl);
    }

    [Fact]
    public async Task Insufficient_description_is_stored_but_excluded_from_recommendations()
    {
        var repository = new ImportRepository();
        var wikipedia = new SummaryProvider(false);
        var service = CreateService(repository, wikipedia: wikipedia);

        await service.ImportAsync("Q666", default);

        Assert.False(Assert.Single(repository.Items).IsRecommendationEligible);
    }

    [Fact]
    public async Task Concurrent_import_of_same_qid_fetches_and_inserts_once()
    {
        var repository = new ImportRepository();
        var wikidata = new DestinationProvider(Data(), TimeSpan.FromMilliseconds(40));
        var service = CreateService(repository, wikidata: wikidata);

        var results = await Task.WhenAll(
            service.ImportAsync("Q777", default),
            service.ImportAsync("Q777", default),
            service.ImportAsync("Q777", default));

        Assert.All(results, result => Assert.True(result.IsSuccess));
        Assert.Single(repository.Items);
        Assert.Equal(1, wikidata.CallCount);
    }

    private static DestinationImportService CreateService(
        ImportRepository repository,
        DestinationData? data = null,
        DestinationProvider? wikidata = null,
        SummaryProvider? wikipedia = null) => new(
            wikidata ?? new DestinationProvider(data ?? Data()),
            wikipedia ?? new SummaryProvider(true),
            new ImageProvider(),
            repository,
            new DestinationImportOptions
            {
                TtlHours = 24,
                DefaultBudgetTier = "standard",
                DefaultSuggestedStayMinDays = 1,
                DefaultSuggestedStayMaxDays = 3,
                DefaultBestTimeToVisit = "proljeće-jesen",
                DefaultBestSeasons = ["spring", "summer", "autumn"],
                TagsByType = new Dictionary<string, List<string>> { ["grad"] = ["grad", "kultura"] }
            },
            new FixedTimeProvider(Now));

    private static DestinationData Data(string name = "Test Grad") => new(
        "Q123", name, "Test City", 43.859, 18.429, "grad", 518, 275524, "Test.jpg",
        "Test_bs", "Test_en", new DestinationDataRegion("Kanton test", "Test Canton"));

    private static Destination ExistingDestination(string qid, string slug) => new()
    {
        Id = Guid.NewGuid(), ExternalId = qid, Slug = slug, Name = "Postojeći", Source = "wikidata",
        Region = "BiH", Description = "Opis", BestTimeToVisit = "ljeto", BudgetTier = "standard",
        SuggestedStayMinDays = 1, SuggestedStayMaxDays = 3, BestSeasons = ["summer"], Tags = ["grad"]
    };

    private sealed class DestinationProvider(DestinationData data, TimeSpan? delay = null) : IDestinationDataProvider
    {
        private int callCount;
        public int CallCount => callCount;
        public Task<DestinationDataSearchResult> SearchAsync(string searchText, CancellationToken cancellationToken) => throw new NotSupportedException();
        public async Task<DestinationDataResult> GetByQidAsync(string qid, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref callCount);
            if (delay is { } wait) await Task.Delay(wait, cancellationToken);
            return DestinationDataResult.Success(data with { Qid = qid });
        }
    }

    private sealed class SummaryProvider(bool sufficient) : IWikipediaSummaryProvider
    {
        public Task<WikipediaSummaryResult> GetSummariesAsync(string? bosnianArticle, string? englishArticle, CancellationToken cancellationToken) =>
            Task.FromResult(WikipediaSummaryResult.Success(
                new WikipediaSummary("Bosanski dovoljno dug opis", "https://bs.wikipedia.test/Test", "CC BY-SA", sufficient),
                new WikipediaSummary("English sufficient description", "https://en.wikipedia.test/Test", "CC BY-SA", true)));
    }

    private sealed class UnavailableSummaryProvider : IWikipediaSummaryProvider
    {
        public Task<WikipediaSummaryResult> GetSummariesAsync(string? bosnianArticle, string? englishArticle, CancellationToken cancellationToken) =>
            Task.FromResult(WikipediaSummaryResult.Failure("Wikipedia unavailable"));
    }

    private sealed class ImageProvider : IImageProvider
    {
        public Task<DestinationImageResult> GetImageAsync(string? imageName, CancellationToken cancellationToken) =>
            Task.FromResult(DestinationImageResult.Success(new DestinationImage(
                "https://images.test/test.jpg", "Autor", "CC BY-SA 4.0", "https://commons.test/Test")));
    }

    private sealed class UnavailableImageProvider : IImageProvider
    {
        public Task<DestinationImageResult> GetImageAsync(string? imageName, CancellationToken cancellationToken) =>
            Task.FromResult(DestinationImageResult.Failure("Image unavailable"));
    }

    private sealed class ImportRepository(IEnumerable<Destination>? seed = null) : IDestinationImportRepository
    {
        private readonly object mutex = new();
        public List<Destination> Items { get; } = seed?.ToList() ?? [];

        public Task<Destination?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken)
        {
            lock (mutex) return Task.FromResult(Items.SingleOrDefault(item => item.ExternalId == externalId));
        }

        public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken)
        {
            lock (mutex) return Task.FromResult(Items.Any(item => item.Slug == slug));
        }

        public Task<Destination> AddAsync(Destination destination, CancellationToken cancellationToken)
        {
            lock (mutex)
            {
                var existing = Items.SingleOrDefault(item => item.ExternalId == destination.ExternalId);
                if (existing is not null) return Task.FromResult(existing);
                Items.Add(destination);
                return Task.FromResult(destination);
            }
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
