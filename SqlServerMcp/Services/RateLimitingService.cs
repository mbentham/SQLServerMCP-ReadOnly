using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;
using SqlServerMcp.Configuration;

namespace SqlServerMcp.Services;

public sealed class RateLimitingService : IRateLimitingService, IDisposable
{
    private readonly ConcurrencyLimiter _concurrencyLimiter;
    private readonly TokenBucketRateLimiter _throughputLimiter;

    public RateLimitingService(IOptions<SqlServerMcpOptions> options)
    {
        var opts = options.Value;

        _concurrencyLimiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
        {
            PermitLimit = opts.MaxConcurrentQueries,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = opts.MaxConcurrentQueries * 2
        });

        _throughputLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = opts.MaxQueriesPerMinute,
            ReplenishmentPeriod = TimeSpan.FromMinutes(1),
            TokensPerPeriod = opts.MaxQueriesPerMinute,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0,
            AutoReplenishment = true
        });
    }

    public async Task<IDisposable> AcquireAsync(CancellationToken cancellationToken)
    {
        // Check throughput first (fail fast, no queuing)
        var throughputLease = _throughputLimiter.AttemptAcquire(1);
        if (!throughputLease.IsAcquired)
        {
            throughputLease.Dispose();
            throw new InvalidOperationException(
                "Rate limit exceeded. Too many queries per minute. Please wait and try again.");
        }
        throughputLease.Dispose();

        // Then acquire concurrency slot (queues up to QueueLimit)
        var concurrencyLease = await _concurrencyLimiter.AcquireAsync(1, cancellationToken);
        if (!concurrencyLease.IsAcquired)
        {
            concurrencyLease.Dispose();
            throw new InvalidOperationException(
                "Too many concurrent queries. Please wait and try again.");
        }

        return concurrencyLease;
    }

    public void Dispose()
    {
        _concurrencyLimiter.Dispose();
        _throughputLimiter.Dispose();
    }
}
