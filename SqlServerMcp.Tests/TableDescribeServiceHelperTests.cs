using SqlServerMcp.Services;
using static SqlServerMcp.Services.SchemaQueryHelper;
using static SqlServerMcp.Services.TableDescribeService;

namespace SqlServerMcp.Tests;

public class TableDescribeServiceHelperTests
{
    // ───────────────────────────────────────────────
    // FormatAction
    // ───────────────────────────────────────────────

    [Fact]
    public void FormatAction_NoAction()
    {
        Assert.Equal("NO ACTION", TableDescribeService.FormatAction("NO_ACTION"));
    }

    [Fact]
    public void FormatAction_Cascade()
    {
        Assert.Equal("CASCADE", TableDescribeService.FormatAction("CASCADE"));
    }

    [Fact]
    public void FormatAction_SetNull()
    {
        Assert.Equal("SET NULL", TableDescribeService.FormatAction("SET_NULL"));
    }

    [Fact]
    public void FormatAction_SetDefault()
    {
        Assert.Equal("SET DEFAULT", TableDescribeService.FormatAction("SET_DEFAULT"));
    }

    [Fact]
    public void FormatAction_Unknown_PassThrough()
    {
        Assert.Equal("RESTRICT", TableDescribeService.FormatAction("RESTRICT"));
    }

    // ───────────────────────────────────────────────
    // FormatDataType
    // ───────────────────────────────────────────────

    [Fact]
    public void FormatDataType_VarcharWithLength()
    {
        Assert.Equal("varchar(50)", SchemaQueryHelper.FormatDataType("varchar", 50, 0, 0));
    }

    [Fact]
    public void FormatDataType_NvarcharMax()
    {
        Assert.Equal("nvarchar(MAX)", SchemaQueryHelper.FormatDataType("nvarchar", -1, 0, 0));
    }

    [Fact]
    public void FormatDataType_DecimalPrecisionScale()
    {
        Assert.Equal("decimal(18,2)", SchemaQueryHelper.FormatDataType("decimal", 0, 18, 2));
    }

    [Fact]
    public void FormatDataType_Int_NoSuffix()
    {
        Assert.Equal("int", SchemaQueryHelper.FormatDataType("int", 4, 10, 0));
    }

    // ───────────────────────────────────────────────
    // BuildMarkdown
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildMarkdown_EmptyCollections_HasHeader()
    {
        var result = TableDescribeService.BuildMarkdown(
            "srv", "db", "dbo", "Users",
            new List<ColumnInfo>(), new List<IndexInfo>(),
            new List<ForeignKeyInfo>(), new List<CheckConstraintInfo>());

        Assert.Contains("# Table: [dbo].[Users]", result);
        Assert.Contains("## Columns", result);
    }

    [Fact]
    public void BuildMarkdown_WithColumns_RendersTable()
    {
        var columns = new List<ColumnInfo>
        {
            new(1, "Id", "int", 4, 10, 0, false, null, null, true, 1, 1, false, null, false),
            new(2, "Name", "nvarchar", 100, 0, 0, true, null, null, false, 0, 0, false, null, false)
        };

        var result = TableDescribeService.BuildMarkdown(
            "srv", "db", "dbo", "Users",
            columns, new List<IndexInfo>(),
            new List<ForeignKeyInfo>(), new List<CheckConstraintInfo>());

        Assert.Contains("| 1 | Id |", result);
        Assert.Contains("| 2 | Name |", result);
        Assert.Contains("IDENTITY(1,1)", result);
    }

    [Fact]
    public void BuildMarkdown_WithForeignKeys_RendersFkSection()
    {
        var fks = new List<ForeignKeyInfo>
        {
            new("FK_Order_User", "UserId", "dbo", "Users", "Id", "NO_ACTION", "NO_ACTION")
        };

        var result = TableDescribeService.BuildMarkdown(
            "srv", "db", "dbo", "Orders",
            new List<ColumnInfo>(), new List<IndexInfo>(),
            fks, new List<CheckConstraintInfo>());

        Assert.Contains("## Foreign Keys", result);
        Assert.Contains("FK_Order_User", result);
        Assert.Contains("[dbo].[Users]", result);
    }

    // ───────────────────────────────────────────────
    // BuildMarkdown — Primary Key
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildMarkdown_WithPrimaryKeyIndex_RendersPKSection()
    {
        var indexes = new List<IndexInfo>
        {
            new("PK_Users", "CLUSTERED", true, true, "Id", false, 1, null),
        };

        var result = TableDescribeService.BuildMarkdown(
            "srv", "db", "dbo", "Users",
            new List<ColumnInfo>(), indexes,
            new List<ForeignKeyInfo>(), new List<CheckConstraintInfo>());

        Assert.Contains("## Primary Key", result);
        Assert.Contains("**PK_Users**: Id", result);
    }

    // ───────────────────────────────────────────────
    // BuildMarkdown — Indexes (non-PK)
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildMarkdown_WithNonPKIndex_RendersIndexSection()
    {
        var indexes = new List<IndexInfo>
        {
            new("IX_Users_Email", "NONCLUSTERED", true, false, "Email", false, 1, null),
        };

        var result = TableDescribeService.BuildMarkdown(
            "srv", "db", "dbo", "Users",
            new List<ColumnInfo>(), indexes,
            new List<ForeignKeyInfo>(), new List<CheckConstraintInfo>());

        Assert.Contains("## Indexes", result);
        Assert.Contains("IX_Users_Email", result);
        Assert.Contains("YES", result); // Unique
    }

    [Fact]
    public void BuildMarkdown_WithIndexIncludedColumns_ShowsIncluded()
    {
        var indexes = new List<IndexInfo>
        {
            new("IX_Users_Name", "NONCLUSTERED", false, false, "Name", false, 1, null),
            new("IX_Users_Name", "NONCLUSTERED", false, false, "Email", true, 0, null),
        };

        var result = TableDescribeService.BuildMarkdown(
            "srv", "db", "dbo", "Users",
            new List<ColumnInfo>(), indexes,
            new List<ForeignKeyInfo>(), new List<CheckConstraintInfo>());

        Assert.Contains("Name", result);
        Assert.Contains("Email", result);
    }

    [Fact]
    public void BuildMarkdown_WithFilteredIndex_ShowsFilter()
    {
        var indexes = new List<IndexInfo>
        {
            new("IX_Users_Active", "NONCLUSTERED", false, false, "IsActive", false, 1, "([IsActive]=(1))"),
        };

        var result = TableDescribeService.BuildMarkdown(
            "srv", "db", "dbo", "Users",
            new List<ColumnInfo>(), indexes,
            new List<ForeignKeyInfo>(), new List<CheckConstraintInfo>());

        Assert.Contains("([IsActive]=(1))", result);
    }

    // ───────────────────────────────────────────────
    // BuildMarkdown — Check Constraints
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildMarkdown_WithCheckConstraints_RendersSection()
    {
        var checks = new List<CheckConstraintInfo>
        {
            new("CK_Price_Positive", "([Price]>(0))"),
        };

        var result = TableDescribeService.BuildMarkdown(
            "srv", "db", "dbo", "Products",
            new List<ColumnInfo>(), new List<IndexInfo>(),
            new List<ForeignKeyInfo>(), checks);

        Assert.Contains("## Check Constraints", result);
        Assert.Contains("CK_Price_Positive", result);
        Assert.Contains("([Price]>(0))", result);
    }

    // ───────────────────────────────────────────────
    // BuildMarkdown — Default Constraints
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildMarkdown_WithDefaultConstraints_RendersSection()
    {
        var columns = new List<ColumnInfo>
        {
            new(1, "CreatedAt", "datetime", 0, 0, 0, false,
                "DF_Logs_CreatedAt", "(getdate())", false, 0, 0, false, null, false),
        };

        var result = TableDescribeService.BuildMarkdown(
            "srv", "db", "dbo", "Logs",
            columns, new List<IndexInfo>(),
            new List<ForeignKeyInfo>(), new List<CheckConstraintInfo>());

        Assert.Contains("## Default Constraints", result);
        Assert.Contains("DF_Logs_CreatedAt", result);
        Assert.Contains("CreatedAt", result);
        Assert.Contains("(getdate())", result);
    }

    [Fact]
    public void BuildMarkdown_ColumnsWithoutDefaults_NoDefaultSection()
    {
        var columns = new List<ColumnInfo>
        {
            new(1, "Id", "int", 4, 10, 0, false, null, null, false, 0, 0, false, null, false),
        };

        var result = TableDescribeService.BuildMarkdown(
            "srv", "db", "dbo", "Users",
            columns, new List<IndexInfo>(),
            new List<ForeignKeyInfo>(), new List<CheckConstraintInfo>());

        Assert.DoesNotContain("## Default Constraints", result);
    }

    // ───────────────────────────────────────────────
    // BuildMarkdown — Computed Columns
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildMarkdown_WithComputedColumn_ShowsComputed()
    {
        var columns = new List<ColumnInfo>
        {
            new(1, "FirstName", "nvarchar", 50, 0, 0, false, null, null, false, 0, 0, false, null, false),
            new(2, "LastName", "nvarchar", 50, 0, 0, false, null, null, false, 0, 0, false, null, false),
            new(3, "FullName", "nvarchar", 101, 0, 0, true, null, null, false, 0, 0,
                true, "[FirstName]+' '+[LastName]", false),
        };

        var result = TableDescribeService.BuildMarkdown(
            "srv", "db", "dbo", "Users",
            columns, new List<IndexInfo>(),
            new List<ForeignKeyInfo>(), new List<CheckConstraintInfo>());

        Assert.Contains("COMPUTED: [FirstName]+' '+[LastName]", result);
    }

    [Fact]
    public void BuildMarkdown_WithPersistedComputedColumn_ShowsPersisted()
    {
        var columns = new List<ColumnInfo>
        {
            new(1, "Total", "decimal", 0, 18, 2, true, null, null, false, 0, 0,
                true, "[Qty]*[Price]", true),
        };

        var result = TableDescribeService.BuildMarkdown(
            "srv", "db", "dbo", "OrderItems",
            columns, new List<IndexInfo>(),
            new List<ForeignKeyInfo>(), new List<CheckConstraintInfo>());

        Assert.Contains("COMPUTED: [Qty]*[Price], PERSISTED", result);
    }
}
