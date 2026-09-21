using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace TravelPlanner.Api.Infrastructure.Persistence;

public sealed class SeedWikidataIdsSeeder(ApplicationDbContext context, IHostEnvironment environment)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var path = Path.Combine(environment.ContentRootPath, "SeedData", "seed-wikidata-ids.json");
        if (!File.Exists(path))
            throw new InvalidOperationException($"Nedostaje konfiguracijski fajl: {path}");

        var mappings = SeedWikidataIds.Parse(await File.ReadAllTextAsync(path, cancellationToken));
        if (mappings.Count == 0)
            return;

        var destinations = await context.Destinations.ToListAsync(cancellationToken);
        SeedWikidataIds.Apply(mappings, destinations);
        await context.SaveChangesAsync(cancellationToken);
    }
}
