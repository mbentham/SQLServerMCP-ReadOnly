using SqlServerMcp.IntegrationTests.Fixtures;

namespace SqlServerMcp.IntegrationTests;

[Collection("Database")]
public sealed class DiagramServiceIntegrationTests
{
    private readonly SqlServerContainerFixture _fixture;
    private const string Server = SqlServerContainerFixture.ServerName;
    private const string Db = SqlServerContainerFixture.TestDatabaseName;

    public DiagramServiceIntegrationTests(SqlServerContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GenerateDiagram_AllSchemas_ContainsAllTablesAndRelationships()
    {
        var service = ServiceFactory.CreateDiagramService(_fixture.ConnectionString);

        var result = await service.GenerateDiagramAsync(Server, Db,
            includeSchemas: null, excludeSchemas: null,
            includeTables: null, excludeTables: null,
            maxTables: 100, CancellationToken.None);

        // PlantUML envelope
        Assert.StartsWith("@startuml", result);
        Assert.Contains("@enduml", result);

        // All four tables present
        Assert.Contains("Categories", result);
        Assert.Contains("Products", result);
        Assert.Contains("Orders", result);
        Assert.Contains("OrderItems", result);

        // FK relationships
        Assert.Contains("FK_Products_Categories", result);
        Assert.Contains("FK_OrderItems_Orders", result);
        Assert.Contains("FK_OrderItems_Products", result);

        // PK and IDENTITY markers on entity columns
        Assert.Contains("<<PK>>", result);
        Assert.Contains("<<IDENTITY>>", result);

        // FK markers on referencing columns
        Assert.Contains("<<FK>>", result);
    }

    [Fact]
    public async Task GenerateDiagram_IncludeSchemaFilter_RestrictsToOneSchema()
    {
        var service = ServiceFactory.CreateDiagramService(_fixture.ConnectionString);

        var result = await service.GenerateDiagramAsync(Server, Db,
            includeSchemas: ["sales"], excludeSchemas: null,
            includeTables: null, excludeTables: null,
            maxTables: 100, CancellationToken.None);

        Assert.Contains("Orders", result);
        Assert.Contains("OrderItems", result);

        // dbo tables should not appear as entities
        Assert.DoesNotContain("entity \"Categories\"", result);
        Assert.DoesNotContain("entity \"Products\"", result);
    }

    [Fact]
    public async Task GenerateDiagram_IncludeMultipleSchemas_ShowsAllSpecifiedSchemas()
    {
        var service = ServiceFactory.CreateDiagramService(_fixture.ConnectionString);

        var result = await service.GenerateDiagramAsync(Server, Db,
            includeSchemas: ["dbo", "sales"], excludeSchemas: null,
            includeTables: null, excludeTables: null,
            maxTables: 100, CancellationToken.None);

        // All four tables should be present
        Assert.Contains("Categories", result);
        Assert.Contains("Products", result);
        Assert.Contains("Orders", result);
        Assert.Contains("OrderItems", result);
    }

    [Fact]
    public async Task GenerateDiagram_ExcludeSchemasFilter_ExcludesSchema()
    {
        var service = ServiceFactory.CreateDiagramService(_fixture.ConnectionString);

        var result = await service.GenerateDiagramAsync(Server, Db,
            includeSchemas: null, excludeSchemas: ["sales"],
            includeTables: null, excludeTables: null,
            maxTables: 100, CancellationToken.None);

        Assert.Contains("Categories", result);
        Assert.Contains("Products", result);

        // sales tables should not appear as entities
        Assert.DoesNotContain("entity \"sales.Orders\"", result);
        Assert.DoesNotContain("entity \"sales.OrderItems\"", result);
    }

    [Fact]
    public async Task GenerateDiagram_MaxTablesTruncation_EmitsTruncationWarning()
    {
        var service = ServiceFactory.CreateDiagramService(_fixture.ConnectionString);

        var result = await service.GenerateDiagramAsync(Server, Db,
            includeSchemas: null, excludeSchemas: null,
            includeTables: null, excludeTables: null,
            maxTables: 2, CancellationToken.None);

        Assert.Contains("WARNING: Output truncated at 2 tables", result);
    }

    [Fact]
    public async Task GenerateDiagram_EmptyDatabase_ReturnsNoTablesFound()
    {
        var service = ServiceFactory.CreateDiagramService(_fixture.ConnectionString);

        // Use a schema name that doesn't exist to get zero tables
        var result = await service.GenerateDiagramAsync(Server, Db,
            includeSchemas: ["nonexistent_schema"], excludeSchemas: null,
            includeTables: null, excludeTables: null,
            maxTables: 100, CancellationToken.None);

        Assert.Contains("No tables found", result);
    }

    [Fact]
    public async Task GenerateDiagram_Compact_ShowsOnlyKeyColumns()
    {
        var service = ServiceFactory.CreateDiagramService(_fixture.ConnectionString);

        var result = await service.GenerateDiagramAsync(Server, Db,
            includeSchemas: null, excludeSchemas: null,
            includeTables: null, excludeTables: null,
            maxTables: 100, CancellationToken.None, compact: true);

        // PlantUML envelope
        Assert.StartsWith("@startuml", result);
        Assert.Contains("@enduml", result);

        // PK and FK stereotypes present
        Assert.Contains("<<PK>>", result);
        Assert.Contains("<<FK>>", result);

        // FK relationships preserved
        Assert.Contains("FK_Products_Categories", result);

        // No data types in compact mode
        Assert.DoesNotContain(": int", result);
        Assert.DoesNotContain(": nvarchar", result);
        Assert.DoesNotContain(": decimal", result);
    }

    [Fact]
    public async Task GenerateDiagram_IncludeTables_ShowsOnlySpecifiedTables()
    {
        var service = ServiceFactory.CreateDiagramService(_fixture.ConnectionString);

        var result = await service.GenerateDiagramAsync(Server, Db,
            includeSchemas: null, excludeSchemas: null,
            includeTables: ["Categories", "Products"], excludeTables: null,
            maxTables: 100, CancellationToken.None);

        Assert.Contains("Categories", result);
        Assert.Contains("Products", result);

        // sales tables should not appear
        Assert.DoesNotContain("entity \"sales.Orders\"", result);
        Assert.DoesNotContain("entity \"sales.OrderItems\"", result);
    }

    [Fact]
    public async Task GenerateDiagram_ExcludeTables_ExcludesSpecifiedTables()
    {
        var service = ServiceFactory.CreateDiagramService(_fixture.ConnectionString);

        var result = await service.GenerateDiagramAsync(Server, Db,
            includeSchemas: null, excludeSchemas: null,
            includeTables: null, excludeTables: ["Categories"],
            maxTables: 100, CancellationToken.None);

        // Categories should be excluded
        Assert.DoesNotContain("entity \"Categories\"", result);

        // Other tables should still appear
        Assert.Contains("Products", result);
        Assert.Contains("Orders", result);
    }

    [Fact]
    public async Task GenerateDiagram_IncludeTablesWithSchemaFilter_BothApply()
    {
        var service = ServiceFactory.CreateDiagramService(_fixture.ConnectionString);

        // Include only dbo schema AND only the Categories table
        var result = await service.GenerateDiagramAsync(Server, Db,
            includeSchemas: ["dbo"], excludeSchemas: null,
            includeTables: ["Categories"], excludeTables: null,
            maxTables: 100, CancellationToken.None);

        Assert.Contains("Categories", result);

        // Products is in dbo but not in includeTables
        Assert.DoesNotContain("entity \"Products\"", result);
        // sales tables excluded by schema filter
        Assert.DoesNotContain("entity \"sales.Orders\"", result);
    }
}
