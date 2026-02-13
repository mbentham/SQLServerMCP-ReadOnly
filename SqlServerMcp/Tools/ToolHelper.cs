using Microsoft.Data.SqlClient;
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
    /// Converts ArgumentException, InvalidOperationException, and SqlException to McpException.
    /// </summary>
    public static async Task<string> ExecuteAsync(IRateLimitingService rateLimiter, Func<Task<string>> operation,
        CancellationToken cancellationToken = default)
    {
        using var lease = await rateLimiter.AcquireAsync(cancellationToken);
        try
        {
            return await operation();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or SqlException)
        {
            throw new McpException(ex.Message);
        }
    }

    /// <summary>
    /// Parses a comma-separated string into a deduplicated list.
    /// Returns null if the input is null, empty, or contains only whitespace/commas.
    /// </summary>
    public static IReadOnlyList<string>? ParseCommaSeparatedList(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var items = input.Split(',')
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return items.Count > 0 ? items : null;
    }
}
