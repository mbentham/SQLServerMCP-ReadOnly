using ModelContextProtocol;
using SqlServerMcp.Services;

namespace SqlServerMcp.Tools;

/// <summary>
/// Helper methods for tool implementations.
/// </summary>
internal static class ToolHelper
{
    /// <summary>
    /// Executes an async tool operation with rate limiting and standardized exception handling.
    /// Acquires a rate limit lease before executing, and releases the concurrency slot on completion.
    /// Converts ArgumentException and InvalidOperationException to McpException.
    /// </summary>
    public static async Task<string> ExecuteAsync(IRateLimitingService rateLimiter, Func<Task<string>> operation)
    {
        using var lease = await rateLimiter.AcquireAsync(default);
        try
        {
            return await operation();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            throw new McpException(ex.Message);
        }
    }
}
