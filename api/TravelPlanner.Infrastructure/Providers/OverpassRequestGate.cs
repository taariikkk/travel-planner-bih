using Microsoft.EntityFrameworkCore;
using TravelPlanner.Api.Infrastructure.Persistence;
using TravelPlanner.Application.DTOs;

namespace TravelPlanner.Infrastructure.Providers;

public interface IOverpassRequestGate
{
    Task<Guid> AcquireAsync(bool fallback, CancellationToken cancellationToken);
    Task ReleaseAsync(Guid token, DateTimeOffset? retryAfter, CancellationToken cancellationToken);
}

public sealed class OverpassRequestGate(ApplicationDbContext context, OverpassOptions options, TimeProvider clock) : IOverpassRequestGate
{
    public async Task<Guid> AcquireAsync(bool fallback, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "OverpassRequestState" ("Id", "BudgetDay", "Attempts") VALUES (1, {now.UtcDateTime.Date}, 0)
            ON CONFLICT DO NOTHING
            """, cancellationToken);
        var state = await context.OverpassRequestStates.FromSqlRaw("""SELECT * FROM "OverpassRequestState" WHERE "Id" = 1 FOR UPDATE""")
            .SingleAsync(cancellationToken);
        // Raw SQL queries must refresh any tracked copy after a previous attempt in this scope.
        await context.Entry(state).ReloadAsync(cancellationToken);
        var retry = new[] { state.LeaseUntil, state.RetryAfter, fallback ? null : state.NextImportAt }
            .Where(t => t > now).Max();
        if (state.BudgetDay.Date == now.UtcDateTime.Date && state.Attempts >= options.DailyAttemptLimit)
            retry = new DateTimeOffset(now.UtcDateTime.Date.AddDays(1));
        if (retry is not null) throw new PlacesProviderException("Overpass request budget/cooldown active.", retry, deferred: true);
        if (state.BudgetDay.Date != now.UtcDateTime.Date) { state.BudgetDay = now.UtcDateTime.Date; state.Attempts = 0; }
        var token = Guid.NewGuid();
        state.Attempts++;
        state.LeaseToken = token;
        state.LeaseUntil = now.AddSeconds(options.HttpTimeoutSeconds + 10);
        if (!fallback) state.NextImportAt = now.AddSeconds(options.MinImportIntervalSeconds);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return token;
    }

    public async Task ReleaseAsync(Guid token, DateTimeOffset? retryAfter, CancellationToken cancellationToken)
    {
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE "OverpassRequestState" SET "LeaseToken" = NULL, "LeaseUntil" = NULL,
            "RetryAfter" = GREATEST("RetryAfter", {retryAfter}) WHERE "Id" = 1 AND "LeaseToken" = {token}
            """, cancellationToken);
    }
}
