using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using ToanTuDuy.Data;

namespace ToanTuDuy.Services.Notifications
{
    public interface IUnreadCountStore
    {
        Task<int> GetAsync(int accountId, CancellationToken cancellationToken = default);

        Task DecrementAsync(int accountId, CancellationToken cancellationToken = default);

        Task IncrementAsync(int accountId, int delta, CancellationToken cancellationToken = default);

        Task ResetAsync(int accountId, CancellationToken cancellationToken = default);
    }

    public sealed class RedisUnreadCountStore : IUnreadCountStore
    {
        private const string DecrementScript = @"
if redis.call('EXISTS', KEYS[1]) == 1 then
    local value = redis.call('DECRBY', KEYS[1], ARGV[1])
    if value < 0 then
        redis.call('SET', KEYS[1], 0)
        return 0
    end
    return value
end
return -1";

        private const string IncrementScript = @"
if redis.call('EXISTS', KEYS[1]) == 1 then
    return redis.call('INCRBY', KEYS[1], ARGV[1])
end
return -1";

        private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(7);

        private readonly IConnectionMultiplexer _redis;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RedisUnreadCountStore> _logger;

        public RedisUnreadCountStore(
            IConnectionMultiplexer redis,
            ApplicationDbContext context,
            ILogger<RedisUnreadCountStore> logger)
        {
            _redis = redis;
            _context = context;
            _logger = logger;
        }

        public async Task<int> GetAsync(int accountId, CancellationToken cancellationToken = default)
        {
            try
            {
                var database = _redis.GetDatabase();
                var key = BuildKey(accountId);
                var value = await database.StringGetAsync(key);
                if (value.HasValue && int.TryParse(value.ToString(), out var cached))
                {
                    return Math.Max(0, cached);
                }

                var count = await CountFromDatabaseAsync(accountId, cancellationToken);
                await database.StringSetAsync(key, count, CacheTtl);
                return count;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Không đọc được unread count từ Redis cho account {AccountId}, dùng SQL fallback.",
                    accountId);
                return await CountFromDatabaseAsync(accountId, cancellationToken);
            }
        }

        public async Task DecrementAsync(int accountId, CancellationToken cancellationToken = default)
        {
            try
            {
                var database = _redis.GetDatabase();
                await database.ScriptEvaluateAsync(
                    DecrementScript,
                    new RedisKey[] { BuildKey(accountId) },
                    new RedisValue[] { 1 });
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Không giảm được unread count trên Redis cho account {AccountId}.",
                    accountId);
            }
        }

        public async Task IncrementAsync(int accountId, int delta, CancellationToken cancellationToken = default)
        {
            if (delta <= 0)
            {
                return;
            }

            try
            {
                var database = _redis.GetDatabase();
                await database.ScriptEvaluateAsync(
                    IncrementScript,
                    new RedisKey[] { BuildKey(accountId) },
                    new RedisValue[] { delta });
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Không tăng được unread count trên Redis cho account {AccountId}.",
                    accountId);
            }
        }

        public async Task ResetAsync(int accountId, CancellationToken cancellationToken = default)
        {
            try
            {
                var database = _redis.GetDatabase();
                await database.StringSetAsync(BuildKey(accountId), 0, CacheTtl);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Không reset được unread count trên Redis cho account {AccountId}.",
                    accountId);
            }
        }

        private Task<int> CountFromDatabaseAsync(int accountId, CancellationToken cancellationToken)
        {
            return _context.UserInboxes
                .AsNoTracking()
                .CountAsync(inbox => inbox.AccountId == accountId && !inbox.IsRead, cancellationToken);
        }

        private static RedisKey BuildKey(int accountId) => $"user:{accountId}:unread_count";
    }
}
