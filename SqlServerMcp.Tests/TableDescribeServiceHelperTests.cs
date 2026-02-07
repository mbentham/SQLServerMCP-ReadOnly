using SqlServerMcp.Services;
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
        var col = new ColumnInfo(1, "c", "varchar", 50, 0, 0, true, null, null, false, 0, 0, false, null, false);
        Assert.Equal("varchar(50)", TableDescribeService.FormatDataType(col));
    }

    [Fact]
    public void FormatDataType_NvarcharMax()
    {
        var col = new ColumnInfo(1, "c", "nvarchar", -1, 0, 0, true, null, null, false, 0, 0, false, null, false);
        Assert.Equal("nvarchar(MAX)", TableDescribeService.FormatDataType(col));
    }

    [Fact]
    public void FormatDataType_DecimalPrecisionScale()
    {
        var col = new ColumnInfo(1, "c", "decimal", 0, 18, 2, false, null, null, false, 0, 0, false, null, false);
        Assert.Equal("decimal(18,2)", TableDescribeService.FormatDataType(col));
    }

    [Fact]
    public void FormatDataType_Int_NoSuffix()
    {
        var col = new ColumnInfo(1, "c", "int", 4, 10, 0, false, null, null, false, 0, 0, false, null, false);
        Assert.Equal("int", TableDescribeService.FormatDataType(col));
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
}
