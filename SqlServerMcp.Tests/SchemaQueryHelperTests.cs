using SqlServerMcp.Services;
using static SqlServerMcp.Services.SchemaQueryHelper;

namespace SqlServerMcp.Tests;

public class SchemaQueryHelperTests
{
    // ───────────────────────────────────────────────
    // BuildTableFilterCte — empty list
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildTableFilterCte_EmptyList_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            SchemaQueryHelper.BuildTableFilterCte([]));

        Assert.Contains("At least one table", ex.Message);
    }

    // ───────────────────────────────────────────────
    // BuildTableFilterCte — single table
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildTableFilterCte_SingleTable_GeneratesValidCte()
    {
        var tables = new List<TableInfo> { new("dbo", "Users") };

        var (cteSql, parameters) = SchemaQueryHelper.BuildTableFilterCte(tables);

        Assert.Contains("WITH table_filter AS", cteSql);
        Assert.Contains("(@s0, @t0)", cteSql);
        Assert.Equal(2, parameters.Length);
        Assert.Equal("@s0", parameters[0].ParameterName);
        Assert.Equal("dbo", parameters[0].Value);
        Assert.Equal("@t0", parameters[1].ParameterName);
        Assert.Equal("Users", parameters[1].Value);
    }

    // ───────────────────────────────────────────────
    // BuildTableFilterCte — multiple tables
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildTableFilterCte_MultipleTables_GeneratesAllParameters()
    {
        var tables = new List<TableInfo>
        {
            new("dbo", "Users"),
            new("sales", "Orders"),
            new("dbo", "Products"),
        };

        var (cteSql, parameters) = SchemaQueryHelper.BuildTableFilterCte(tables);

        Assert.Contains("(@s0, @t0)", cteSql);
        Assert.Contains("(@s1, @t1)", cteSql);
        Assert.Contains("(@s2, @t2)", cteSql);
        Assert.Equal(6, parameters.Length);

        Assert.Equal("sales", parameters[2].Value);
        Assert.Equal("Orders", parameters[3].Value);
    }

    [Fact]
    public void BuildTableFilterCte_MultipleTables_SeparatesWithCommas()
    {
        var tables = new List<TableInfo>
        {
            new("dbo", "A"),
            new("dbo", "B"),
        };

        var (cteSql, _) = SchemaQueryHelper.BuildTableFilterCte(tables);

        Assert.Contains("(@s0, @t0), (@s1, @t1)", cteSql);
    }

    [Fact]
    public void BuildTableFilterCte_CteSql_EndsWithClosingParenAndSpace()
    {
        var tables = new List<TableInfo> { new("dbo", "Users") };

        var (cteSql, _) = SchemaQueryHelper.BuildTableFilterCte(tables);

        Assert.EndsWith(") ", cteSql);
    }
}
