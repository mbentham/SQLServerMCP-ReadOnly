namespace SqlServerMcp.Services;

public interface IRateLimitingService
{
    /// <summary>
    /// Acquires a rate limit lease. The returned IDisposable releases the concurrency slot when disposed.
    /// Throws InvalidOperationException if the rate limit or concurrency limit is exceeded.
    /// </summary>
    Task<IDisposable> AcquireAsync(CancellationToken cancellationToken);
}
