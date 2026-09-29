using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using TravelPlanner.Api.Domain.Entities;
using TravelPlanner.Api.Infrastructure.Persistence;
using TravelPlanner.Application.DTOs;
using TravelPlanner.Application.Interfaces;
using TravelPlanner.Application.Services;

namespace TravelPlanner.Infrastructure.Repositories;

public sealed class PlacesImportRepository(ApplicationDbContext context, ILogger<PlacesImportRepository> logger) : IPlacesImportRepository
{
    // Shared with the manual seeder: serialize short write transactions, never provider HTTP calls.
    internal const long WriteLock = 72401913;

    public Task<PlacesImportTarget?> GetTargetAsync(string slug, CancellationToken cancellationToken) =>
        context.Destinations.AsNoTracking().Where(d => d.Slug == slug)
            .Select(d => new PlacesImportTarget(d.Id, d.Latitude, d.Longitude)).SingleOrDefaultAsync(cancellationToken);

    public async Task<PlacesLeaseResult> TryAcquireAsync(Guid destinationId, PlacesImportLease candidate, DateTimeOffset now,
        TimeSpan ttl, CancellationToken cancellationToken)
    {
        var until = now.AddSeconds(PlacesImportOptions.DestinationLeaseSeconds);
        var freshAfter = now - ttl;
        var count = await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "PlacesImportState" ("DestinationId", "LeaseToken", "LeaseUntil")
            VALUES ({destinationId}, {candidate.Token}, {until})
            ON CONFLICT ("DestinationId") DO UPDATE
            SET "LeaseToken" = {candidate.Token}, "LeaseUntil" = {until}
            WHERE ("PlacesImportState"."LeaseUntil" IS NULL OR "PlacesImportState"."LeaseUntil" <= {now})
              AND ("PlacesImportState"."NextAttemptAt" IS NULL OR "PlacesImportState"."NextAttemptAt" <= {now})
              AND ("PlacesImportState"."QuerySignature" IS DISTINCT FROM {candidate.Signature}
                   OR "PlacesImportState"."SucceededAt" IS NULL OR "PlacesImportState"."SucceededAt" <= {freshAfter})
            """, cancellationToken);
        logger.LogInformation("Places import {DestinationId}: {CacheState}", destinationId, count == 1 ? "refresh acquired" : "cached or deferred");
        if (count == 1) return new(PlacesLeaseStatus.Acquired, candidate);
        // Classification is informational; the conditional upsert above remains the atomic lease authority.
        var state = await context.PlacesImportStates.AsNoTracking()
            .Where(item => item.DestinationId == destinationId)
            .Select(item => new { item.LeaseUntil, item.NextAttemptAt, item.QuerySignature, item.SucceededAt })
            .SingleAsync(cancellationToken);
        var retryAt = new[] { state.LeaseUntil, state.NextAttemptAt }.Where(value => value > now).Max();
        if (retryAt is not null) return new(PlacesLeaseStatus.Deferred, RetryAt: retryAt);
        return state.QuerySignature == candidate.Signature && state.SucceededAt > freshAfter
            ? new(PlacesLeaseStatus.Cached)
            : new(PlacesLeaseStatus.Deferred);
    }

    public async Task<bool> CompleteAsync(Guid destinationId, PlacesImportLease lease, PlacesData data,
        DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({WriteLock})", cancellationToken);
        var state = await context.PlacesImportStates.FromSqlInterpolated($"""
            SELECT * FROM "PlacesImportState" WHERE "DestinationId" = {destinationId} FOR UPDATE
            """).SingleAsync(cancellationToken);
        await context.Entry(state).ReloadAsync(cancellationToken);
        if (state.LeaseToken != lease.Token || state.LeaseUntil <= now) return false;
        var ids = data.Places.Select(p => p.ExternalId).Distinct().ToArray();
        var existing = await context.Places.Where(p => p.ExternalId != null && ids.Contains(p.ExternalId))
            .ToDictionaryAsync(p => p.ExternalId!, cancellationToken);
        var links = await context.DestinationPlaces.Where(l => l.DestinationId == destinationId).ToListAsync(cancellationToken);
        var linkedPlaceIds = links.Select(link => link.PlaceId).ToHashSet();
        var seen = new HashSet<Guid>();
        foreach (var item in data.Places.DistinctBy(p => p.ExternalId))
        {
            if (!existing.TryGetValue(item.ExternalId, out var place))
            {
                place = new Place { Id = Guid.NewGuid(), ExternalId = item.ExternalId, Source = "osm", ImportedAt = now };
                context.Places.Add(place);
            }
            ApplyImported(place, item);
            place.ImportedAt ??= now;
            place.LastVerifiedAt = now;
            place.SourceUrl = $"https://www.openstreetmap.org/{item.ExternalId}";
            seen.Add(place.Id);
            if (linkedPlaceIds.Add(place.Id))
                context.DestinationPlaces.Add(new() { DestinationId = destinationId, Place = place });
        }
        context.DestinationPlaces.RemoveRange(links.Where(l => !l.IsManual && !seen.Contains(l.PlaceId)));
        state.SucceededAt = now;
        state.QuerySignature = lease.Signature;
        state.NextAttemptAt = null;
        state.LeaseToken = null;
        state.LeaseUntil = null;
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Places import {DestinationId} saved {Count} places; skipped {Skipped}", destinationId, seen.Count, data.SkippedCount);
        return true;
    }

    public async Task FailAsync(Guid destinationId, PlacesImportLease lease, DateTimeOffset nextAttemptAt, CancellationToken cancellationToken)
    {
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE "PlacesImportState" SET "LeaseToken" = NULL, "LeaseUntil" = NULL, "NextAttemptAt" = {nextAttemptAt}
            WHERE "DestinationId" = {destinationId} AND "LeaseToken" = {lease.Token}
            """, cancellationToken);
        logger.LogWarning("Places import {DestinationId} deferred until {NextAttemptAt}", destinationId, nextAttemptAt);
    }

    public static void ApplyImported(Place place, PlaceData data)
    {
        void Set(string field, Action apply) { if (!place.ManualOverrideFields.Contains(field)) apply(); }
        Set(nameof(Place.Name), () => place.Name = data.Name);
        Set(nameof(Place.Category), () => place.Category = data.Category);
        Set(nameof(Place.Location), () => place.Location = new Point(data.Longitude, data.Latitude) { SRID = 4326 });
        Set(nameof(Place.DescriptionBs), () => place.DescriptionBs = data.DescriptionBs);
        Set(nameof(Place.DescriptionEn), () => place.DescriptionEn = data.DescriptionEn);
        Set(nameof(Place.Description), () => place.Description = data.Description);
        Set(nameof(Place.DescriptionLanguage), () => place.DescriptionLanguage = data.DescriptionLanguage);
        Set(nameof(Place.Address), () => place.Address = data.Address);
        Set(nameof(Place.Cuisine), () => place.Cuisine = data.Cuisine);
        Set(nameof(Place.Website), () => place.Website = data.Website);
        Set(nameof(Place.Phone), () => place.Phone = data.Phone);
        Set(nameof(Place.Email), () => place.Email = data.Email);
    }
}
