using SqlServerMcp.Services;
using static SqlServerMcp.Services.SchemaOverviewService;
using static SqlServerMcp.Services.SchemaQueryHelper;

namespace SqlServerMcp.Tests;

public class SchemaOverviewServiceHelperTests
{
    // ───────────────────────────────────────────────
    // SanitizeMarkdownCell
    // ───────────────────────────────────────────────

    [Fact]
    public void SanitizeMarkdownCell_EscapesPipeCharacters()
    {
        Assert.Equal("col\\|name", SchemaOverviewService.SanitizeMarkdownCell("col|name"));
    }

    [Fact]
    public void SanitizeMarkdownCell_RemovesCarriageReturn()
    {
        Assert.Equal("line one", SchemaOverviewService.SanitizeMarkdownCell("line\r one"));
    }

    [Fact]
    public void SanitizeMarkdownCell_ReplacesNewlineWithSpace()
    {
        Assert.Equal("line one line two", SchemaOverviewService.SanitizeMarkdownCell("line one\nline two"));
    }

    [Fact]
    public void SanitizeMarkdownCell_HandlesCrLf()
    {
        Assert.Equal("line one line two", SchemaOverviewService.SanitizeMarkdownCell("line one\r\nline two"));
    }

    [Fact]
    public void SanitizeMarkdownCell_MultiplePipes()
    {
        Assert.Equal("a\\|b\\|c", SchemaOverviewService.SanitizeMarkdownCell("a|b|c"));
    }

    [Fact]
    public void SanitizeMarkdownCell_CleanInput_Unchanged()
    {
        Assert.Equal("NormalColumn_123", SchemaOverviewService.SanitizeMarkdownCell("NormalColumn_123"));
    }

    // ───────────────────────────────────────────────
    // BuildMarkdown — basic rendering
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildMarkdown_BasicColumns_RendersHeaderAndTable()
    {
        var tables = new List<TableInfo> { new("dbo", "Users") };
        var columns = new List<ColumnInfo>
        {
            new("dbo", "Users", "Id", "int", 4, 10, 0, false, true, true, null),
            new("dbo", "Users", "Name", "nvarchar", 100, 0, 0, true, false, false, null),
        };

        var result = SchemaOverviewService.BuildMarkdown("srv", "db", null, 50,
            tables, columns, [], [], []);

        Assert.Contains("# Schema: db on srv", result);
        Assert.Contains("Tables: 1", result);
        Assert.Contains("## Users", result);
        Assert.Contains("| Column | Type | Null | Key | Extra |", result);
        Assert.Contains("| Id | int | NO | PK | IDENTITY |", result);
        Assert.Contains("| Name | nvarchar(100) | YES |", result);
    }

    // ───────────────────────────────────────────────
    // BuildMarkdown — FK annotations
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildMarkdown_ForeignKey_ShowsFKWithReference()
    {
        var tables = new List<TableInfo> { new("dbo", "Orders") };
        var columns = new List<ColumnInfo>
        {
            new("dbo", "Orders", "UserId", "int", 4, 10, 0, false, false, false, null),
        };
        var fks = new List<ForeignKeyInfo>
        {
            new("dbo", "Orders", "UserId", "dbo", "Users", "Id"),
        };

        var result = SchemaOverviewService.BuildMarkdown("srv", "db", null, 50,
            tables, columns, fks, [], []);

        Assert.Contains("FK Users.Id", result);
    }

    [Fact]
    public void BuildMarkdown_ForeignKey_NonDboRefSchema_IncludesSchema()
    {
        var tables = new List<TableInfo> { new("dbo", "Orders") };
        var columns = new List<ColumnInfo>
        {
            new("dbo", "Orders", "UserId", "int", 4, 10, 0, false, false, false, null),
        };
        var fks = new List<ForeignKeyInfo>
        {
            new("dbo", "Orders", "UserId", "auth", "Users", "Id"),
        };

        var result = SchemaOverviewService.BuildMarkdown("srv", "db", null, 50,
            tables, columns, fks, [], []);

        Assert.Contains("FK auth.Users.Id", result);
    }

    // ───────────────────────────────────────────────
    // BuildMarkdown — UQ annotations
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildMarkdown_UniqueColumn_ShowsUQ()
    {
        var tables = new List<TableInfo> { new("dbo", "Users") };
        var columns = new List<ColumnInfo>
        {
            new("dbo", "Users", "Email", "nvarchar", 256, 0, 0, false, false, false, null),
        };
        var uniques = new List<UniqueColumnInfo>
        {
            new("dbo", "Users", "Email"),
        };

        var result = SchemaOverviewService.BuildMarkdown("srv", "db", null, 50,
            tables, columns, [], [], uniques);

        Assert.Contains("UQ", result);
    }

    // ───────────────────────────────────────────────
    // BuildMarkdown — check constraint annotations
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildMarkdown_ColumnLevelCheckConstraint_ShowsCHK()
    {
        var tables = new List<TableInfo> { new("dbo", "Products") };
        var columns = new List<ColumnInfo>
        {
            new("dbo", "Products", "Price", "decimal", 0, 18, 2, false, false, false, null),
        };
        var checks = new List<CheckConstraintInfo>
        {
            new("dbo", "Products", "Price", "([Price]>(0))"),
        };

        var result = SchemaOverviewService.BuildMarkdown("srv", "db", null, 50,
            tables, columns, [], checks, []);

        Assert.Contains("CHK: ([Price]>(0))", result);
    }

    [Fact]
    public void BuildMarkdown_TableLevelCheckConstraint_ShowsCHK()
    {
        var tables = new List<TableInfo> { new("dbo", "Events") };
        var columns = new List<ColumnInfo>
        {
            new("dbo", "Events", "StartDate", "datetime", 0, 0, 0, false, false, false, null),
            new("dbo", "Events", "EndDate", "datetime", 0, 0, 0, false, false, false, null),
        };
        var checks = new List<CheckConstraintInfo>
        {
            new("dbo", "Events", null, "([EndDate]>[StartDate])"),
        };

        var result = SchemaOverviewService.BuildMarkdown("srv", "db", null, 50,
            tables, columns, [], checks, []);

        Assert.Contains("CHK: ([EndDate]>[StartDate])", result);
    }

    // ───────────────────────────────────────────────
    // BuildMarkdown — default definitions
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildMarkdown_DefaultDefinition_ShowsDEFAULT()
    {
        var tables = new List<TableInfo> { new("dbo", "Logs") };
        var columns = new List<ColumnInfo>
        {
            new("dbo", "Logs", "CreatedAt", "datetime", 0, 0, 0, false, false, false, "(getdate())"),
        };

        var result = SchemaOverviewService.BuildMarkdown("srv", "db", null, 50,
            tables, columns, [], [], []);

        Assert.Contains("DEFAULT (getdate())", result);
    }

    // ───────────────────────────────────────────────
    // BuildMarkdown — non-dbo schema
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildMarkdown_NonDboSchema_IncludesSchemaPrefix()
    {
        var tables = new List<TableInfo> { new("sales", "Orders") };
        var columns = new List<ColumnInfo>
        {
            new("sales", "Orders", "Id", "int", 4, 10, 0, false, true, false, null),
        };

        var result = SchemaOverviewService.BuildMarkdown("srv", "db", null, 50,
            tables, columns, [], [], []);

        Assert.Contains("## sales.Orders", result);
    }

    [Fact]
    public void BuildMarkdown_DboSchema_OmitsSchemaPrefix()
    {
        var tables = new List<TableInfo> { new("dbo", "Orders") };
        var columns = new List<ColumnInfo>
        {
            new("dbo", "Orders", "Id", "int", 4, 10, 0, false, true, false, null),
        };

        var result = SchemaOverviewService.BuildMarkdown("srv", "db", null, 50,
            tables, columns, [], [], []);

        Assert.Contains("## Orders", result);
        Assert.DoesNotContain("## dbo.Orders", result);
    }

    // ───────────────────────────────────────────────
    // BuildMarkdown — truncation warning
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildMarkdown_TableCountEqualsMax_ShowsTruncationWarning()
    {
        var tables = new List<TableInfo>
        {
            new("dbo", "Table1"),
            new("dbo", "Table2"),
        };
        var columns = new List<ColumnInfo>();

        var result = SchemaOverviewService.BuildMarkdown("srv", "db", null, 2,
            tables, columns, [], [], []);

        Assert.Contains("**Truncated at 2**", result);
    }

    [Fact]
    public void BuildMarkdown_TableCountBelowMax_NoTruncationWarning()
    {
        var tables = new List<TableInfo> { new("dbo", "Table1") };
        var columns = new List<ColumnInfo>();

        var result = SchemaOverviewService.BuildMarkdown("srv", "db", null, 50,
            tables, columns, [], [], []);

        Assert.DoesNotContain("Truncated", result);
    }

    // ───────────────────────────────────────────────
    // BuildMarkdown — schema filter display
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildMarkdown_WithSchemaFilter_ShowsFilterInHeader()
    {
        var tables = new List<TableInfo> { new("sales", "Orders") };
        var columns = new List<ColumnInfo>();

        var result = SchemaOverviewService.BuildMarkdown("srv", "db", ["sales"], 50,
            tables, columns, [], [], []);

        Assert.Contains("Schema: sales", result);
    }

    // ───────────────────────────────────────────────
    // BuildMarkdown — no columns for table
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildMarkdown_TableWithNoColumns_ShowsNoColumnsMessage()
    {
        var tables = new List<TableInfo> { new("dbo", "EmptyTable") };
        var columns = new List<ColumnInfo>();

        var result = SchemaOverviewService.BuildMarkdown("srv", "db", null, 50,
            tables, columns, [], [], []);

        Assert.Contains("*No columns found*", result);
    }

    // ───────────────────────────────────────────────
    // BuildMarkdown — combined key annotations
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildMarkdown_FKAndUQ_ShowsBothAnnotations()
    {
        var tables = new List<TableInfo> { new("dbo", "Profiles") };
        var columns = new List<ColumnInfo>
        {
            new("dbo", "Profiles", "UserId", "int", 4, 10, 0, false, false, false, null),
        };
        var fks = new List<ForeignKeyInfo>
        {
            new("dbo", "Profiles", "UserId", "dbo", "Users", "Id"),
        };
        var uniques = new List<UniqueColumnInfo>
        {
            new("dbo", "Profiles", "UserId"),
        };

        var result = SchemaOverviewService.BuildMarkdown("srv", "db", null, 50,
            tables, columns, fks, [], uniques);

        Assert.Contains("FK Users.Id, UQ", result);
    }

    [Fact]
    public void BuildMarkdown_PKAndFK_ShowsBothKeys()
    {
        var tables = new List<TableInfo> { new("dbo", "OrderItems") };
        var columns = new List<ColumnInfo>
        {
            new("dbo", "OrderItems", "OrderId", "int", 4, 10, 0, false, true, false, null),
        };
        var fks = new List<ForeignKeyInfo>
        {
            new("dbo", "OrderItems", "OrderId", "dbo", "Orders", "Id"),
        };

        var result = SchemaOverviewService.BuildMarkdown("srv", "db", null, 50,
            tables, columns, fks, [], []);

        Assert.Contains("PK, FK Orders.Id", result);
    }

    // ───────────────────────────────────────────────
    // BuildMarkdown — compact mode
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildMarkdown_Compact_OnlyPkAndFkColumns()
    {
        var tables = new List<TableInfo> { new("dbo", "Orders") };
        var columns = new List<ColumnInfo>
        {
            new("dbo", "Orders", "OrderId", "int", 4, 10, 0, false, true, true, null),
            new("dbo", "Orders", "UserId", "int", 4, 10, 0, false, false, false, null),
            new("dbo", "Orders", "TotalAmount", "decimal", 0, 18, 2, false, false, false, null),
            new("dbo", "Orders", "Notes", "nvarchar", 500, 0, 0, true, false, false, null),
        };
        var fks = new List<ForeignKeyInfo>
        {
            new("dbo", "Orders", "UserId", "dbo", "Users", "Id"),
        };

        var result = SchemaOverviewService.BuildMarkdown("srv", "db", null, 50,
            tables, columns, fks, [], [], compact: true);

        // PK column present
        Assert.Contains("| OrderId |", result);
        // FK column present
        Assert.Contains("| UserId |", result);
        // Non-key columns omitted
        Assert.DoesNotContain("TotalAmount", result);
        Assert.DoesNotContain("Notes", result);
    }

    [Fact]
    public void BuildMarkdown_Compact_SimplifiedTableHeader()
    {
        var tables = new List<TableInfo> { new("dbo", "Users") };
        var columns = new List<ColumnInfo>
        {
            new("dbo", "Users", "Id", "int", 4, 10, 0, false, true, false, null),
        };

        var result = SchemaOverviewService.BuildMarkdown("srv", "db", null, 50,
            tables, columns, [], [], [], compact: true);

        Assert.Contains("| Column | Key |", result);
        Assert.Contains("|--------|-----|", result);
    }

    [Fact]
    public void BuildMarkdown_Compact_NoTypeNullExtraColumns()
    {
        var tables = new List<TableInfo> { new("dbo", "Users") };
        var columns = new List<ColumnInfo>
        {
            new("dbo", "Users", "Id", "int", 4, 10, 0, false, true, true, "(1)"),
        };
        var checks = new List<CheckConstraintInfo>
        {
            new("dbo", "Users", "Id", "([Id]>(0))"),
        };

        var result = SchemaOverviewService.BuildMarkdown("srv", "db", null, 50,
            tables, columns, [], checks, [], compact: true);

        // Should not have full-mode headers
        Assert.DoesNotContain("| Type |", result);
        Assert.DoesNotContain("| Null |", result);
        Assert.DoesNotContain("| Extra |", result);
        // Should not show check constraints or defaults in compact mode
        Assert.DoesNotContain("CHK:", result);
        Assert.DoesNotContain("IDENTITY", result);
        Assert.DoesNotContain("DEFAULT", result);
    }

    [Fact]
    public void BuildMarkdown_Compact_PreservesFkReferences()
    {
        var tables = new List<TableInfo> { new("dbo", "Orders") };
        var columns = new List<ColumnInfo>
        {
            new("dbo", "Orders", "UserId", "int", 4, 10, 0, false, false, false, null),
        };
        var fks = new List<ForeignKeyInfo>
        {
            new("dbo", "Orders", "UserId", "auth", "Users", "Id"),
        };

        var result = SchemaOverviewService.BuildMarkdown("srv", "db", null, 50,
            tables, columns, fks, [], [], compact: true);

        Assert.Contains("FK auth.Users.Id", result);
    }

    [Fact]
    public void BuildMarkdown_Compact_False_UnchangedOutput()
    {
        var tables = new List<TableInfo> { new("dbo", "Users") };
        var columns = new List<ColumnInfo>
        {
            new("dbo", "Users", "Id", "int", 4, 10, 0, false, true, true, null),
            new("dbo", "Users", "Name", "nvarchar", 100, 0, 0, true, false, false, null),
        };

        var defaultResult = SchemaOverviewService.BuildMarkdown("srv", "db", null, 50,
            tables, columns, [], [], []);
        var explicitResult = SchemaOverviewService.BuildMarkdown("srv", "db", null, 50,
            tables, columns, [], [], [], compact: false);

        Assert.Equal(defaultResult, explicitResult);
    }
}
