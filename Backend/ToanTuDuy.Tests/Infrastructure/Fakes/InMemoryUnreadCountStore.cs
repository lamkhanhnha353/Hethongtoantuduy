using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using ToanTuDuy.Data;
using ToanTuDuy.Services.Notifications;

namespace ToanTuDuy.Tests.Infrastructure.Fakes;

public sealed class InMemoryUnreadCountState
{
    public ConcurrentDictionary<int, int> Counts { get; } = new();

    public void Reset() => Counts.Clear();
}

public sealed class InMemoryUnreadCountStore(
    InMemoryUnreadCountState state,
    ApplicationDbContext dbContext) : IUnreadCountStore
{
    public async Task<int> GetAsync(int accountId, CancellationToken cancellationToken = default)
    {
        if (state.Counts.TryGetValue(accountId, out var count))
        {
            return Math.Max(0, count);
        }

        var dbCount = await dbContext.UserInboxes
            .AsNoTracking()
            .CountAsync(x => x.AccountId == accountId && !x.IsRead, cancellationToken);

        state.Counts[accountId] = dbCount;
        return dbCount;
    }

    public Task DecrementAsync(int accountId, CancellationToken cancellationToken = default)
    {
        state.Counts.AddOrUpdate(accountId, _ => 0, (_, current) => Math.Max(0, current - 1));
        return Task.CompletedTask;
    }

    public Task IncrementAsync(int accountId, int delta, CancellationToken cancellationToken = default)
    {
        if (delta > 0)
        {
            state.Counts.AddOrUpdate(accountId, delta, (_, current) => current + delta);
        }
        return Task.CompletedTask;
    }

    public Task ResetAsync(int accountId, CancellationToken cancellationToken = default)
    {
        state.Counts[accountId] = 0;
        return Task.CompletedTask;
    }
}
