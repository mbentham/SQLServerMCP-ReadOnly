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
    public static async Task<string> ExecuteAsync(IRateLimitingService rateLimiter, Func<Task<string>> operation,
        CancellationToken cancellationToken = default)
    {
        using var lease = await rateLimiter.AcquireAsync(cancellationToken);
        try
        {
            return await operation();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            throw new McpException(ex.Message);
        }
    }

    /// <summary>
    /// Parses a comma-separated exclude-schemas string into a deduplicated list.
    /// Returns null if the input is null, empty, or contains only whitespace/commas.
    /// </summary>
    public static IReadOnlyList<string>? ParseExcludeSchemas(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var schemas = input.Split(',')
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return schemas.Count > 0 ? schemas : null;
    }
}
