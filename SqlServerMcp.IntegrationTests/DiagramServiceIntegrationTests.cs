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
            includeSchema: null, excludeSchemas: null, maxTables: 100,
            CancellationToken.None);

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
            includeSchema: "sales", excludeSchemas: null, maxTables: 100,
            CancellationToken.None);

        Assert.Contains("Orders", result);
        Assert.Contains("OrderItems", result);

        // dbo tables should not appear as entities
        Assert.DoesNotContain("entity \"Categories\"", result);
        Assert.DoesNotContain("entity \"Products\"", result);
    }

    [Fact]
    public async Task GenerateDiagram_ExcludeSchemasFilter_ExcludesSchema()
    {
        var service = ServiceFactory.CreateDiagramService(_fixture.ConnectionString);

        var result = await service.GenerateDiagramAsync(Server, Db,
            includeSchema: null, excludeSchemas: ["sales"], maxTables: 100,
            CancellationToken.None);

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
            includeSchema: null, excludeSchemas: null, maxTables: 2,
            CancellationToken.None);

        Assert.Contains("WARNING: Output truncated at 2 tables", result);
    }

    [Fact]
    public async Task GenerateDiagram_EmptyDatabase_ReturnsNoTablesFound()
    {
        var service = ServiceFactory.CreateDiagramService(_fixture.ConnectionString);

        // Use a schema name that doesn't exist to get zero tables
        var result = await service.GenerateDiagramAsync(Server, Db,
            includeSchema: "nonexistent_schema", excludeSchemas: null, maxTables: 100,
            CancellationToken.None);

        Assert.Contains("No tables found", result);
    }
}
