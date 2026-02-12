using SqlServerMcp.Tools;

namespace SqlServerMcp.Tests;

public class ToolHelperTests
{
    // ───────────────────────────────────────────────
    // ParseExcludeSchemas
    // ───────────────────────────────────────────────

    [Fact]
    public void ParseExcludeSchemas_Null_ReturnsNull()
    {
        Assert.Null(ToolHelper.ParseExcludeSchemas(null));
    }

    [Fact]
    public void ParseExcludeSchemas_Empty_ReturnsNull()
    {
        Assert.Null(ToolHelper.ParseExcludeSchemas(""));
    }

    [Fact]
    public void ParseExcludeSchemas_Whitespace_ReturnsNull()
    {
        Assert.Null(ToolHelper.ParseExcludeSchemas("   "));
    }

    [Fact]
    public void ParseExcludeSchemas_SingleSchema_ReturnsSingleElement()
    {
        var result = ToolHelper.ParseExcludeSchemas("audit");

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("audit", result[0]);
    }

    [Fact]
    public void ParseExcludeSchemas_MultipleSchemas_ReturnsAll()
    {
        var result = ToolHelper.ParseExcludeSchemas("audit,staging,temp");

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("audit", result[0]);
        Assert.Equal("staging", result[1]);
        Assert.Equal("temp", result[2]);
    }

    [Fact]
    public void ParseExcludeSchemas_TrimsWhitespace()
    {
        var result = ToolHelper.ParseExcludeSchemas(" audit , staging ");

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("audit", result[0]);
        Assert.Equal("staging", result[1]);
    }

    [Fact]
    public void ParseExcludeSchemas_Duplicates_CaseInsensitive_Deduplicates()
    {
        var result = ToolHelper.ParseExcludeSchemas("audit,Audit,AUDIT");

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("audit", result[0]);
    }

    [Fact]
    public void ParseExcludeSchemas_ConsecutiveCommas_SkipsEmptyEntries()
    {
        var result = ToolHelper.ParseExcludeSchemas("audit,,staging,,,temp");

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("audit", result[0]);
        Assert.Equal("staging", result[1]);
        Assert.Equal("temp", result[2]);
    }

    [Fact]
    public void ParseExcludeSchemas_OnlyCommas_ReturnsNull()
    {
        Assert.Null(ToolHelper.ParseExcludeSchemas(",,,"));
    }
}
