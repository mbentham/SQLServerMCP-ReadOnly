using SqlServerMcp.IntegrationTests.Fixtures;

namespace SqlServerMcp.IntegrationTests;

[Collection("Database")]
public sealed class SchemaOverviewServiceIntegrationTests
{
    private readonly SqlServerContainerFixture _fixture;
    private const string Server = SqlServerContainerFixture.ServerName;
    private const string Db = SqlServerContainerFixture.TestDatabaseName;

    public SchemaOverviewServiceIntegrationTests(SqlServerContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GenerateOverview_AllSchemas_ContainsAllTablesWithMetadata()
    {
        var service = ServiceFactory.CreateSchemaOverviewService(_fixture.ConnectionString);

        var result = await service.GenerateOverviewAsync(Server, Db,
            includeSchemas: null, excludeSchemas: null,
            includeTables: null, excludeTables: null,
            maxTables: 100, CancellationToken.None);

        // Header
        Assert.Contains($"# Schema: {Db}", result);

        // All four tables present as markdown headers
        Assert.Contains("## Categories", result);
        Assert.Contains("## Products", result);
        Assert.Contains("## sales.Orders", result);
        Assert.Contains("## sales.OrderItems", result);

        // Primary keys
        Assert.Contains("PK", result);

        // Foreign keys
        Assert.Contains("FK", result);
        Assert.Contains("Categories.CategoryId", result);

        // Unique constraint on Categories.Name
        Assert.Contains("UQ", result);

        // Check constraints
        Assert.Contains("CHK:", result);

        // Default constraints
        Assert.Contains("DEFAULT", result);

        // Identity column
        Assert.Contains("IDENTITY", result);
    }

    [Fact]
    public async Task GenerateOverview_IncludeSchemaFilter_RestrictsOutput()
    {
        var service = ServiceFactory.CreateSchemaOverviewService(_fixture.ConnectionString);

        var result = await service.GenerateOverviewAsync(Server, Db,
            includeSchemas: ["dbo"], excludeSchemas: null,
            includeTables: null, excludeTables: null,
            maxTables: 100, CancellationToken.None);

        Assert.Contains("## Categories", result);
        Assert.Contains("## Products", result);

        // sales schema tables should not appear
        Assert.DoesNotContain("## sales.Orders", result);
        Assert.DoesNotContain("## sales.OrderItems", result);
    }

    [Fact]
    public async Task GenerateOverview_IncludeMultipleSchemas_ShowsAllSpecifiedSchemas()
    {
        var service = ServiceFactory.CreateSchemaOverviewService(_fixture.ConnectionString);

        var result = await service.GenerateOverviewAsync(Server, Db,
            includeSchemas: ["dbo", "sales"], excludeSchemas: null,
            includeTables: null, excludeTables: null,
            maxTables: 100, CancellationToken.None);

        // All four tables should be present
        Assert.Contains("## Categories", result);
        Assert.Contains("## Products", result);
        Assert.Contains("## sales.Orders", result);
        Assert.Contains("## sales.OrderItems", result);
    }

    [Fact]
    public async Task GenerateOverview_ExcludeSchemasFilter_ExcludesSchema()
    {
        var service = ServiceFactory.CreateSchemaOverviewService(_fixture.ConnectionString);

        var result = await service.GenerateOverviewAsync(Server, Db,
            includeSchemas: null, excludeSchemas: ["dbo"],
            includeTables: null, excludeTables: null,
            maxTables: 100, CancellationToken.None);

        Assert.Contains("## sales.Orders", result);
        Assert.Contains("## sales.OrderItems", result);

        // dbo tables should not appear
        Assert.DoesNotContain("## Categories", result);
        Assert.DoesNotContain("## Products", result);
    }

    [Fact]
    public async Task GenerateOverview_NonexistentSchema_ReturnsNoTablesFound()
    {
        var service = ServiceFactory.CreateSchemaOverviewService(_fixture.ConnectionString);

        var result = await service.GenerateOverviewAsync(Server, Db,
            includeSchemas: ["nonexistent_schema"], excludeSchemas: null,
            includeTables: null, excludeTables: null,
            maxTables: 100, CancellationToken.None);

        Assert.Contains("No tables found", result);
    }

    [Fact]
    public async Task GenerateOverview_Compact_ShowsOnlyKeyColumns()
    {
        var service = ServiceFactory.CreateSchemaOverviewService(_fixture.ConnectionString);

        var result = await service.GenerateOverviewAsync(Server, Db,
            includeSchemas: null, excludeSchemas: null,
            includeTables: null, excludeTables: null,
            maxTables: 100, CancellationToken.None, compact: true);

        // Header still present
        Assert.Contains($"# Schema: {Db}", result);

        // Compact table header
        Assert.Contains("| Column | Key |", result);

        // PK and FK annotations
        Assert.Contains("PK", result);
        Assert.Contains("FK", result);

        // Should not have full-mode columns
        Assert.DoesNotContain("| Type |", result);
        Assert.DoesNotContain("| Null |", result);
        Assert.DoesNotContain("| Extra |", result);

        // No check constraints, defaults, or identity in compact mode
        Assert.DoesNotContain("CHK:", result);
        Assert.DoesNotContain("DEFAULT", result);
        Assert.DoesNotContain("IDENTITY", result);
    }

    [Fact]
    public async Task GenerateOverview_IncludeTables_ShowsOnlySpecifiedTables()
    {
        var service = ServiceFactory.CreateSchemaOverviewService(_fixture.ConnectionString);

        var result = await service.GenerateOverviewAsync(Server, Db,
            includeSchemas: null, excludeSchemas: null,
            includeTables: ["Categories", "Products"], excludeTables: null,
            maxTables: 100, CancellationToken.None);

        Assert.Contains("## Categories", result);
        Assert.Contains("## Products", result);

        // sales tables should not appear
        Assert.DoesNotContain("## sales.Orders", result);
        Assert.DoesNotContain("## sales.OrderItems", result);
    }

    [Fact]
    public async Task GenerateOverview_ExcludeTables_ExcludesSpecifiedTables()
    {
        var service = ServiceFactory.CreateSchemaOverviewService(_fixture.ConnectionString);

        var result = await service.GenerateOverviewAsync(Server, Db,
            includeSchemas: null, excludeSchemas: null,
            includeTables: null, excludeTables: ["Categories"],
            maxTables: 100, CancellationToken.None);

        // Categories should be excluded
        Assert.DoesNotContain("## Categories", result);

        // Other tables should still appear
        Assert.Contains("## Products", result);
        Assert.Contains("## sales.Orders", result);
    }

    [Fact]
    public async Task GenerateOverview_IncludeTablesWithSchemaFilter_BothApply()
    {
        var service = ServiceFactory.CreateSchemaOverviewService(_fixture.ConnectionString);

        // Include only dbo schema AND only the Categories table
        var result = await service.GenerateOverviewAsync(Server, Db,
            includeSchemas: ["dbo"], excludeSchemas: null,
            includeTables: ["Categories"], excludeTables: null,
            maxTables: 100, CancellationToken.None);

        Assert.Contains("## Categories", result);

        // Products is in dbo but not in includeTables
        Assert.DoesNotContain("## Products", result);
        // sales tables excluded by schema filter
        Assert.DoesNotContain("## sales.Orders", result);
    }
}
