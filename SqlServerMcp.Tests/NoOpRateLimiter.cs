using SqlServerMcp.Services;

namespace SqlServerMcp.Tests;

/// <summary>
/// A no-op rate limiter for unit tests that always permits requests immediately.
/// </summary>
internal sealed class NoOpRateLimiter : IRateLimitingService
{
    public Task<IDisposable> AcquireAsync(CancellationToken cancellationToken)
        => Task.FromResult<IDisposable>(NoOpDisposable.Instance);

    private sealed class NoOpDisposable : IDisposable
    {
        public static readonly NoOpDisposable Instance = new();
        public void Dispose() { }
    }
}
