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

    // ───────────────────────────────────────────────
    // BuildTableQuery — no filters
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildTableQuery_NoFilters_BaseQueryOnly()
    {
        var (sql, parameters) = SchemaQueryHelper.BuildTableQuery(null, null);

        Assert.Contains("sys.tables", sql);
        Assert.Contains("is_ms_shipped = 0", sql);
        Assert.Contains("temporal_type <> 2", sql);
        Assert.DoesNotContain("@inclSchema", sql);
        Assert.DoesNotContain("NOT IN", sql);
        Assert.Empty(parameters);
    }

    // ───────────────────────────────────────────────
    // BuildTableQuery — includeSchemas
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildTableQuery_IncludeSchemas_AddsInClause()
    {
        var (sql, parameters) = SchemaQueryHelper.BuildTableQuery(["dbo"], null);

        Assert.Contains("AND s.name IN (@inclSchema0)", sql);
        Assert.Single(parameters);
        Assert.Equal("@inclSchema0", parameters[0].ParameterName);
        Assert.Equal("dbo", parameters[0].Value);
    }

    [Fact]
    public void BuildTableQuery_MultipleIncludeSchemas_AddsInClause()
    {
        var (sql, parameters) = SchemaQueryHelper.BuildTableQuery(["dbo", "sales"], null);

        Assert.Contains("AND s.name IN (@inclSchema0, @inclSchema1)", sql);
        Assert.Equal(2, parameters.Count);
        Assert.Equal("dbo", parameters[0].Value);
        Assert.Equal("sales", parameters[1].Value);
    }

    // ───────────────────────────────────────────────
    // BuildTableQuery — excludeSchemas
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildTableQuery_SingleExcludeSchema_AddsNotInWithOneParam()
    {
        var (sql, parameters) = SchemaQueryHelper.BuildTableQuery(null, ["audit"]);

        Assert.Contains("AND s.name NOT IN (@excl0)", sql);
        Assert.Single(parameters);
        Assert.Equal("@excl0", parameters[0].ParameterName);
        Assert.Equal("audit", parameters[0].Value);
    }

    [Fact]
    public void BuildTableQuery_MultipleExcludeSchemas_AddsNotInWithAllParams()
    {
        var (sql, parameters) = SchemaQueryHelper.BuildTableQuery(null, ["audit", "staging", "temp"]);

        Assert.Contains("AND s.name NOT IN (@excl0, @excl1, @excl2)", sql);
        Assert.Equal(3, parameters.Count);
        Assert.Equal("audit", parameters[0].Value);
        Assert.Equal("staging", parameters[1].Value);
        Assert.Equal("temp", parameters[2].Value);
    }

    // ───────────────────────────────────────────────
    // BuildTableQuery — includeSchemas takes precedence
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildTableQuery_BothIncludeAndExclude_IncludeWins()
    {
        var (sql, parameters) = SchemaQueryHelper.BuildTableQuery(["dbo"], ["audit", "staging"]);

        Assert.Contains("AND s.name IN (@inclSchema0)", sql);
        Assert.DoesNotContain("NOT IN", sql);
        Assert.Single(parameters);
        Assert.Equal("@inclSchema0", parameters[0].ParameterName);
    }

    // ───────────────────────────────────────────────
    // BuildTableQuery — empty excludeSchemas
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildTableQuery_EmptyExcludeList_NoFilter()
    {
        var (sql, parameters) = SchemaQueryHelper.BuildTableQuery(null, []);

        Assert.DoesNotContain("NOT IN", sql);
        Assert.DoesNotContain("@inclSchema", sql);
        Assert.Empty(parameters);
    }

    // ───────────────────────────────────────────────
    // BuildTableQuery — includeTables
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildTableQuery_SingleIncludeTable_AddsInClause()
    {
        var (sql, parameters) = SchemaQueryHelper.BuildTableQuery(null, null, ["Users"]);

        Assert.Contains("AND t.name IN (@incTbl0)", sql);
        Assert.Single(parameters);
        Assert.Equal("@incTbl0", parameters[0].ParameterName);
        Assert.Equal("Users", parameters[0].Value);
    }

    [Fact]
    public void BuildTableQuery_MultipleIncludeTables_AddsInClause()
    {
        var (sql, parameters) = SchemaQueryHelper.BuildTableQuery(null, null, ["Users", "Orders", "Products"]);

        Assert.Contains("AND t.name IN (@incTbl0, @incTbl1, @incTbl2)", sql);
        Assert.Equal(3, parameters.Count);
        Assert.Equal("Users", parameters[0].Value);
        Assert.Equal("Orders", parameters[1].Value);
        Assert.Equal("Products", parameters[2].Value);
    }

    // ───────────────────────────────────────────────
    // BuildTableQuery — excludeTables
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildTableQuery_SingleExcludeTable_AddsNotInClause()
    {
        var (sql, parameters) = SchemaQueryHelper.BuildTableQuery(null, null, null, ["AuditLog"]);

        Assert.Contains("AND t.name NOT IN (@exclTbl0)", sql);
        Assert.Single(parameters);
        Assert.Equal("@exclTbl0", parameters[0].ParameterName);
        Assert.Equal("AuditLog", parameters[0].Value);
    }

    [Fact]
    public void BuildTableQuery_MultipleExcludeTables_AddsNotInClause()
    {
        var (sql, parameters) = SchemaQueryHelper.BuildTableQuery(null, null, null, ["AuditLog", "TempData"]);

        Assert.Contains("AND t.name NOT IN (@exclTbl0, @exclTbl1)", sql);
        Assert.Equal(2, parameters.Count);
        Assert.Equal("AuditLog", parameters[0].Value);
        Assert.Equal("TempData", parameters[1].Value);
    }

    // ───────────────────────────────────────────────
    // BuildTableQuery — includeTables takes precedence
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildTableQuery_BothIncludeAndExcludeTables_IncludeWins()
    {
        var (sql, parameters) = SchemaQueryHelper.BuildTableQuery(null, null, ["Users"], ["AuditLog"]);

        Assert.Contains("AND t.name IN (@incTbl0)", sql);
        Assert.DoesNotContain("@exclTbl", sql);
        Assert.Single(parameters);
    }

    // ───────────────────────────────────────────────
    // BuildTableQuery — empty includeTables
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildTableQuery_EmptyIncludeTablesList_NoFilter()
    {
        var (sql, parameters) = SchemaQueryHelper.BuildTableQuery(null, null, []);

        Assert.DoesNotContain("@incTbl", sql);
        Assert.DoesNotContain("@exclTbl", sql);
        Assert.Empty(parameters);
    }

    // ───────────────────────────────────────────────
    // BuildTableQuery — schema and table filters compose
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildTableQuery_SchemaAndTableFiltersCompose()
    {
        var (sql, parameters) = SchemaQueryHelper.BuildTableQuery(["dbo"], null, ["Users", "Orders"]);

        Assert.Contains("AND s.name IN (@inclSchema0)", sql);
        Assert.Contains("AND t.name IN (@incTbl0, @incTbl1)", sql);
        Assert.Equal(3, parameters.Count);
    }

    [Fact]
    public void BuildTableQuery_ExcludeSchemaAndExcludeTablesCompose()
    {
        var (sql, parameters) = SchemaQueryHelper.BuildTableQuery(null, ["audit"], null, ["TempData"]);

        Assert.Contains("AND s.name NOT IN (@excl0)", sql);
        Assert.Contains("AND t.name NOT IN (@exclTbl0)", sql);
        Assert.Equal(2, parameters.Count);
    }

    // ───────────────────────────────────────────────
    // BuildTableQuery — ORDER BY always present
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildTableQuery_AlwaysEndsWithOrderBy()
    {
        var (sql1, _) = SchemaQueryHelper.BuildTableQuery(null, null);
        var (sql2, _) = SchemaQueryHelper.BuildTableQuery(["dbo"], null);
        var (sql3, _) = SchemaQueryHelper.BuildTableQuery(null, ["audit"]);
        var (sql4, _) = SchemaQueryHelper.BuildTableQuery(null, null, ["Users"]);
        var (sql5, _) = SchemaQueryHelper.BuildTableQuery(null, null, null, ["AuditLog"]);

        Assert.EndsWith("ORDER BY s.name, t.name", sql1);
        Assert.EndsWith("ORDER BY s.name, t.name", sql2);
        Assert.EndsWith("ORDER BY s.name, t.name", sql3);
        Assert.EndsWith("ORDER BY s.name, t.name", sql4);
        Assert.EndsWith("ORDER BY s.name, t.name", sql5);
    }
}
