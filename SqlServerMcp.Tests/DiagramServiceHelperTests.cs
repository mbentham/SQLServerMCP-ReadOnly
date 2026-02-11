using SqlServerMcp.Services;
using static SqlServerMcp.Services.DiagramService;
using static SqlServerMcp.Services.SchemaQueryHelper;

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
    public void SanitizePlantUmlText_RemovesExclamationMark()
    {
        Assert.Equal("include /etc/passwd", DiagramService.SanitizePlantUmlText("!include /etc/passwd"));
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

    [Fact]
    public void FormatDataType_BinaryWithLength()
    {
        Assert.Equal("varbinary(100)", SchemaQueryHelper.FormatDataType("varbinary", 100, 0, 0));
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

    // ───────────────────────────────────────────────
    // FK Cardinality Detection
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildPlantUml_FKCardinality_OneToOneMandatory()
    {
        // IsUnique=true, IsNullable=false → || --|{
        var tables = new List<TableInfo>
        {
            new("dbo", "Parent"),
            new("dbo", "Child")
        };
        var columns = new List<ColumnInfo>
        {
            new("dbo", "Parent", "Id", "int", 0, 0, 0, false, true, false),
            new("dbo", "Child", "ParentId", "int", 0, 0, 0, false, false, false)
        };
        var fks = new List<ForeignKeyInfo>
        {
            new("FK_Child_Parent", "dbo", "Child", "ParentId", "dbo", "Parent", "Id",
                IsNullable: false, IsUnique: true)
        };

        var result = DiagramService.BuildPlantUml("srv", "db", null, 10, tables, columns, fks);

        // One-to-one mandatory: referenced side || and FK side ||
        Assert.Contains("dbo_Parent ||--|| dbo_Child", result);
    }

    [Fact]
    public void BuildPlantUml_FKCardinality_OneToOneOptional()
    {
        // IsUnique=true, IsNullable=true → || --o|
        var tables = new List<TableInfo>
        {
            new("dbo", "Parent"),
            new("dbo", "Child")
        };
        var columns = new List<ColumnInfo>
        {
            new("dbo", "Parent", "Id", "int", 0, 0, 0, false, true, false),
            new("dbo", "Child", "ParentId", "int", 0, 0, 0, true, false, false)
        };
        var fks = new List<ForeignKeyInfo>
        {
            new("FK_Child_Parent", "dbo", "Child", "ParentId", "dbo", "Parent", "Id",
                IsNullable: true, IsUnique: true)
        };

        var result = DiagramService.BuildPlantUml("srv", "db", null, 10, tables, columns, fks);

        // One-to-one optional: referenced side || and FK side o|
        Assert.Contains("dbo_Parent ||--o| dbo_Child", result);
    }

    [Fact]
    public void BuildPlantUml_FKCardinality_OneToManyMandatory()
    {
        // IsUnique=false, IsNullable=false → || --|{
        var tables = new List<TableInfo>
        {
            new("dbo", "Parent"),
            new("dbo", "Child")
        };
        var columns = new List<ColumnInfo>
        {
            new("dbo", "Parent", "Id", "int", 0, 0, 0, false, true, false),
            new("dbo", "Child", "ParentId", "int", 0, 0, 0, false, false, false)
        };
        var fks = new List<ForeignKeyInfo>
        {
            new("FK_Child_Parent", "dbo", "Child", "ParentId", "dbo", "Parent", "Id",
                IsNullable: false, IsUnique: false)
        };

        var result = DiagramService.BuildPlantUml("srv", "db", null, 10, tables, columns, fks);

        // One-to-many mandatory: referenced side || and FK side |{
        Assert.Contains("dbo_Parent ||--|{ dbo_Child", result);
    }

    [Fact]
    public void BuildPlantUml_FKCardinality_OneToManyOptional()
    {
        // IsUnique=false, IsNullable=true → || --o{
        var tables = new List<TableInfo>
        {
            new("dbo", "Parent"),
            new("dbo", "Child")
        };
        var columns = new List<ColumnInfo>
        {
            new("dbo", "Parent", "Id", "int", 0, 0, 0, false, true, false),
            new("dbo", "Child", "ParentId", "int", 0, 0, 0, true, false, false)
        };
        var fks = new List<ForeignKeyInfo>
        {
            new("FK_Child_Parent", "dbo", "Child", "ParentId", "dbo", "Parent", "Id",
                IsNullable: true, IsUnique: false)
        };

        var result = DiagramService.BuildPlantUml("srv", "db", null, 10, tables, columns, fks);

        // One-to-many optional: referenced side || and FK side o{
        Assert.Contains("dbo_Parent ||--o{ dbo_Child", result);
    }

    [Fact]
    public void BuildPlantUml_FKCardinality_CompositeFKDeduplication()
    {
        // Composite FK with same FkName should only emit one relationship line
        var tables = new List<TableInfo>
        {
            new("dbo", "Parent"),
            new("dbo", "Child")
        };
        var columns = new List<ColumnInfo>
        {
            new("dbo", "Parent", "Id1", "int", 0, 0, 0, false, true, false),
            new("dbo", "Parent", "Id2", "int", 0, 0, 0, false, true, false),
            new("dbo", "Child", "ParentId1", "int", 0, 0, 0, false, false, false),
            new("dbo", "Child", "ParentId2", "int", 0, 0, 0, false, false, false)
        };
        var fks = new List<ForeignKeyInfo>
        {
            new("FK_Child_Parent", "dbo", "Child", "ParentId1", "dbo", "Parent", "Id1", false, false),
            new("FK_Child_Parent", "dbo", "Child", "ParentId2", "dbo", "Parent", "Id2", false, false)
        };

        var result = DiagramService.BuildPlantUml("srv", "db", null, 10, tables, columns, fks);

        // Should only contain one relationship line for the composite FK
        var relationshipCount = System.Text.RegularExpressions.Regex.Matches(result, @"dbo_Parent \|\|--\|\{ dbo_Child").Count;
        Assert.Equal(1, relationshipCount);
    }

    // ───────────────────────────────────────────────
    // Index Grouping and Column Stereotypes
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildPlantUml_CompositePrimaryKey_AllMarkedAsPK()
    {
        var tables = new List<TableInfo> { new("dbo", "Orders") };
        var columns = new List<ColumnInfo>
        {
            new("dbo", "Orders", "CustomerId", "int", 0, 0, 0, false, IsPrimaryKey: true, false),
            new("dbo", "Orders", "OrderId", "int", 0, 0, 0, false, IsPrimaryKey: true, false),
            new("dbo", "Orders", "Amount", "decimal", 0, 18, 2, false, false, false)
        };
        var fks = new List<ForeignKeyInfo>();

        var result = DiagramService.BuildPlantUml("srv", "db", null, 10, tables, columns, fks);

        // Both PK columns should have <<PK>> stereotype and be above separator
        Assert.Contains("* CustomerId : int <<PK>>", result);
        Assert.Contains("* OrderId : int <<PK>>", result);

        // Should have separator between PK and non-PK columns
        Assert.Contains("--", result);
    }

    [Fact]
    public void BuildPlantUml_IdentityColumn_MarkedWithIdentity()
    {
        var tables = new List<TableInfo> { new("dbo", "Users") };
        var columns = new List<ColumnInfo>
        {
            new("dbo", "Users", "Id", "int", 0, 0, 0, false, IsPrimaryKey: true, IsIdentity: true),
            new("dbo", "Users", "Name", "nvarchar", 100, 0, 0, false, false, false)
        };
        var fks = new List<ForeignKeyInfo>();

        var result = DiagramService.BuildPlantUml("srv", "db", null, 10, tables, columns, fks);

        // PK with IDENTITY should have both stereotypes
        Assert.Contains("* Id : int <<PK>> <<IDENTITY>>", result);
    }

    [Fact]
    public void BuildPlantUml_ForeignKeyColumn_MarkedWithFK()
    {
        var tables = new List<TableInfo>
        {
            new("dbo", "Parent"),
            new("dbo", "Child")
        };
        var columns = new List<ColumnInfo>
        {
            new("dbo", "Parent", "Id", "int", 0, 0, 0, false, true, false),
            new("dbo", "Child", "Id", "int", 0, 0, 0, false, true, false),
            new("dbo", "Child", "ParentId", "int", 0, 0, 0, false, false, false)
        };
        var fks = new List<ForeignKeyInfo>
        {
            new("FK_Child_Parent", "dbo", "Child", "ParentId", "dbo", "Parent", "Id", false, false)
        };

        var result = DiagramService.BuildPlantUml("srv", "db", null, 10, tables, columns, fks);

        // FK column should have <<FK>> stereotype
        Assert.Contains("ParentId : int <<FK>>", result);
    }

    [Fact]
    public void BuildPlantUml_NullableVsMandatory_CorrectPrefix()
    {
        var tables = new List<TableInfo> { new("dbo", "Products") };
        var columns = new List<ColumnInfo>
        {
            new("dbo", "Products", "Id", "int", 0, 0, 0, false, true, false),
            new("dbo", "Products", "Name", "nvarchar", 100, 0, 0, IsNullable: false, false, false),
            new("dbo", "Products", "Description", "nvarchar", 500, 0, 0, IsNullable: true, false, false)
        };
        var fks = new List<ForeignKeyInfo>();

        var result = DiagramService.BuildPlantUml("srv", "db", null, 10, tables, columns, fks);

        // Mandatory columns (NOT NULL) have * prefix
        Assert.Contains("  * Name : nvarchar(100)", result);

        // Nullable columns have just spaces (no *)
        Assert.Contains("   Description : nvarchar(500)", result);
        Assert.DoesNotContain("  * Description", result);
    }

    [Fact]
    public void BuildPlantUml_ColumnOrdering_PKsFirstThenNonPKs()
    {
        var tables = new List<TableInfo> { new("dbo", "Items") };
        var columns = new List<ColumnInfo>
        {
            new("dbo", "Items", "Name", "nvarchar", 100, 0, 0, false, false, false),
            new("dbo", "Items", "Id", "int", 0, 0, 0, false, true, false),
            new("dbo", "Items", "Price", "decimal", 0, 18, 2, false, false, false)
        };
        var fks = new List<ForeignKeyInfo>();

        var result = DiagramService.BuildPlantUml("srv", "db", null, 10, tables, columns, fks);

        // PK column should appear before separator, non-PK columns after
        var idIndex = result.IndexOf("* Id : int <<PK>>");
        var separatorIndex = result.IndexOf("--");
        var nameIndex = result.IndexOf("Name : nvarchar(100)");
        var priceIndex = result.IndexOf("Price : decimal(18,2)");

        Assert.True(idIndex < separatorIndex, "PK should appear before separator");
        Assert.True(separatorIndex < nameIndex, "Separator should appear before non-PK columns");
        Assert.True(separatorIndex < priceIndex, "Separator should appear before non-PK columns");
    }

    [Fact]
    public void BuildPlantUml_NonDboSchema_IncludesSchemaInDisplayName()
    {
        var tables = new List<TableInfo> { new("sales", "Orders") };
        var columns = new List<ColumnInfo>
        {
            new("sales", "Orders", "Id", "int", 0, 0, 0, false, true, false)
        };
        var fks = new List<ForeignKeyInfo>();

        var result = DiagramService.BuildPlantUml("srv", "db", null, 10, tables, columns, fks);

        // Non-dbo schema should be prefixed in display name
        Assert.Contains("entity \"sales.Orders\" as sales_Orders", result);
    }

    [Fact]
    public void BuildPlantUml_DboSchema_OmitsSchemaInDisplayName()
    {
        var tables = new List<TableInfo> { new("dbo", "Orders") };
        var columns = new List<ColumnInfo>
        {
            new("dbo", "Orders", "Id", "int", 0, 0, 0, false, true, false)
        };
        var fks = new List<ForeignKeyInfo>();

        var result = DiagramService.BuildPlantUml("srv", "db", null, 10, tables, columns, fks);

        // dbo schema should be omitted from display name but included in alias
        Assert.Contains("entity \"Orders\" as dbo_Orders", result);
        Assert.DoesNotContain("entity \"dbo.Orders\"", result);
    }

    // ───────────────────────────────────────────────
    // Table with no columns
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildPlantUml_TableWithNoColumns_RendersEmptyEntity()
    {
        var tables = new List<TableInfo> { new("dbo", "EmptyTable") };
        var columns = new List<ColumnInfo>();
        var fks = new List<ForeignKeyInfo>();

        var result = DiagramService.BuildPlantUml("srv", "db", null, 10, tables, columns, fks);

        Assert.Contains("entity \"EmptyTable\" as dbo_EmptyTable {", result);
        Assert.Contains("}", result);
        // Should not contain separator or column lines
        Assert.DoesNotContain("<<PK>>", result);
    }
}
