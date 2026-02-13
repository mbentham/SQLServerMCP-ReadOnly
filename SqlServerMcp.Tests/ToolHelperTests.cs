using System.Reflection;
using Microsoft.Data.SqlClient;
using ModelContextProtocol;
using SqlServerMcp.Tools;

namespace SqlServerMcp.Tests;

public class ToolHelperTests
{
    // ───────────────────────────────────────────────
    // ExecuteAsync — exception wrapping
    // ───────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_SqlException_WrapsAsMcpException()
    {
        var rateLimiter = new NoOpRateLimiter();
        var sqlEx = CreateSqlException("Test SQL error");

        var mcpEx = await Assert.ThrowsAsync<McpException>(
            () => ToolHelper.ExecuteAsync(rateLimiter, () => throw sqlEx, TestContext.Current.CancellationToken));

        Assert.Contains("Test SQL error", mcpEx.Message);
    }

    private static SqlException CreateSqlException(string message)
    {
        // SqlException has no public constructor; use reflection to build one for testing.
        var errorCollection = (SqlErrorCollection)Activator.CreateInstance(typeof(SqlErrorCollection), nonPublic: true)!;

        var errorCtors = typeof(SqlError).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance);
        var error = (SqlError)errorCtors[0].Invoke([50000, (byte)1, (byte)17, "server", message, "", 0, 0, null]);

        typeof(SqlErrorCollection)
            .GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(errorCollection, [error]);

        var createMethod = typeof(SqlException)
            .GetMethod("CreateException", BindingFlags.NonPublic | BindingFlags.Static,
                null, [typeof(SqlErrorCollection), typeof(string)], null)!;

        return (SqlException)createMethod.Invoke(null, [errorCollection, "16.0.0"])!;
    }

    // ───────────────────────────────────────────────
    // ParseCommaSeparatedList
    // ───────────────────────────────────────────────

    [Fact]
    public void ParseCommaSeparatedList_Null_ReturnsNull()
    {
        Assert.Null(ToolHelper.ParseCommaSeparatedList(null));
    }

    [Fact]
    public void ParseCommaSeparatedList_Empty_ReturnsNull()
    {
        Assert.Null(ToolHelper.ParseCommaSeparatedList(""));
    }

    [Fact]
    public void ParseCommaSeparatedList_Whitespace_ReturnsNull()
    {
        Assert.Null(ToolHelper.ParseCommaSeparatedList("   "));
    }

    [Fact]
    public void ParseCommaSeparatedList_SingleValue_ReturnsSingleElement()
    {
        var result = ToolHelper.ParseCommaSeparatedList("audit");

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("audit", result[0]);
    }

    [Fact]
    public void ParseCommaSeparatedList_MultipleValues_ReturnsAll()
    {
        var result = ToolHelper.ParseCommaSeparatedList("audit,staging,temp");

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("audit", result[0]);
        Assert.Equal("staging", result[1]);
        Assert.Equal("temp", result[2]);
    }

    [Fact]
    public void ParseCommaSeparatedList_TrimsWhitespace()
    {
        var result = ToolHelper.ParseCommaSeparatedList(" audit , staging ");

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("audit", result[0]);
        Assert.Equal("staging", result[1]);
    }

    [Fact]
    public void ParseCommaSeparatedList_Duplicates_CaseInsensitive_Deduplicates()
    {
        var result = ToolHelper.ParseCommaSeparatedList("audit,Audit,AUDIT");

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("audit", result[0]);
    }

    [Fact]
    public void ParseCommaSeparatedList_ConsecutiveCommas_SkipsEmptyEntries()
    {
        var result = ToolHelper.ParseCommaSeparatedList("audit,,staging,,,temp");

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("audit", result[0]);
        Assert.Equal("staging", result[1]);
        Assert.Equal("temp", result[2]);
    }

    [Fact]
    public void ParseCommaSeparatedList_OnlyCommas_ReturnsNull()
    {
        Assert.Null(ToolHelper.ParseCommaSeparatedList(",,,"));
    }
}
