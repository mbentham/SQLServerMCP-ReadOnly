using SqlServerMcp.Services;
using static SqlServerMcp.Services.DiagramService;

namespace SqlServerMcp.Tests;

public class DiagramServiceHelperTests
{
    // ───────────────────────────────────────────────
    // SanitizePlantUmlText
    // ───────────────────────────────────────────────

    [Fact]
    public void SanitizePlantUmlText_RemovesNewlines()
    {
        Assert.Equal("hello world", DiagramService.SanitizePlantUmlText("hello\r\n world"));
    }

    [Fact]
    public void SanitizePlantUmlText_RemovesAtSign()
    {
        Assert.Equal("startuml", DiagramService.SanitizePlantUmlText("@startuml"));
    }

    [Fact]
    public void SanitizePlantUmlText_RemovesQuotesAndBraces()
    {
        Assert.Equal("abc", DiagramService.SanitizePlantUmlText("\"a{b}c\""));
    }

    [Fact]
    public void SanitizePlantUmlText_PlainText_Unchanged()
    {
        Assert.Equal("NormalText_123", DiagramService.SanitizePlantUmlText("NormalText_123"));
    }

    // ───────────────────────────────────────────────
    // SanitizeAlias
    // ───────────────────────────────────────────────

    [Fact]
    public void SanitizeAlias_ReplacesDots()
    {
        Assert.Equal("dbo_Users", DiagramService.SanitizeAlias("dbo.Users"));
    }

    [Fact]
    public void SanitizeAlias_ReplacesSpacesAndDashes()
    {
        Assert.Equal("my_table_name", DiagramService.SanitizeAlias("my table-name"));
    }

    [Fact]
    public void SanitizeAlias_PlainText_Unchanged()
    {
        Assert.Equal("simple_alias", DiagramService.SanitizeAlias("simple_alias"));
    }

    // ───────────────────────────────────────────────
    // FormatDataType
    // ───────────────────────────────────────────────

    [Fact]
    public void FormatDataType_VarcharWithLength()
    {
        var col = new ColumnInfo("dbo", "t", "c", "varchar", 50, 0, 0, true, false, false);
        Assert.Equal("varchar(50)", DiagramService.FormatDataType(col));
    }

    [Fact]
    public void FormatDataType_NvarcharMax()
    {
        var col = new ColumnInfo("dbo", "t", "c", "nvarchar", -1, 0, 0, true, false, false);
        Assert.Equal("nvarchar(MAX)", DiagramService.FormatDataType(col));
    }

    [Fact]
    public void FormatDataType_DecimalPrecisionScale()
    {
        var col = new ColumnInfo("dbo", "t", "c", "decimal", 0, 18, 2, false, false, false);
        Assert.Equal("decimal(18,2)", DiagramService.FormatDataType(col));
    }

    [Fact]
    public void FormatDataType_Int_NoSuffix()
    {
        var col = new ColumnInfo("dbo", "t", "c", "int", 4, 10, 0, false, false, false);
        Assert.Equal("int", DiagramService.FormatDataType(col));
    }

    [Fact]
    public void FormatDataType_BinaryWithLength()
    {
        var col = new ColumnInfo("dbo", "t", "c", "varbinary", 100, 0, 0, true, false, false);
        Assert.Equal("varbinary(100)", DiagramService.FormatDataType(col));
    }

    // ───────────────────────────────────────────────
    // GenerateEmptyDiagram
    // ───────────────────────────────────────────────

    [Fact]
    public void GenerateEmptyDiagram_ContainsStartEndUml()
    {
        var result = DiagramService.GenerateEmptyDiagram("srv", "db", null);
        Assert.Contains("@startuml", result);
        Assert.Contains("@enduml", result);
    }

    [Fact]
    public void GenerateEmptyDiagram_ContainsNoTablesNote()
    {
        var result = DiagramService.GenerateEmptyDiagram("srv", "db", null);
        Assert.Contains("No tables found", result);
    }

    [Fact]
    public void GenerateEmptyDiagram_SanitizesInputs()
    {
        var result = DiagramService.GenerateEmptyDiagram("srv@evil", "db@inject", null);
        Assert.DoesNotContain("@evil", result);
        Assert.DoesNotContain("@inject", result);
    }

    // ───────────────────────────────────────────────
    // BuildPlantUml
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildPlantUml_TruncationWarning()
    {
        var tables = new List<TableInfo>
        {
            new("dbo", "Table1"),
            new("dbo", "Table2")
        };
        var columns = new List<ColumnInfo>();
        var fks = new List<ForeignKeyInfo>();

        // maxTables == tables.Count triggers the warning
        var result = DiagramService.BuildPlantUml("srv", "db", null, 2, tables, columns, fks);

        Assert.Contains("WARNING", result);
    }
}
