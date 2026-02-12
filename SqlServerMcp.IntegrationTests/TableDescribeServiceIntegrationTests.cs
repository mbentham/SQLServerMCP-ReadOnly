using SqlServerMcp.IntegrationTests.Fixtures;

namespace SqlServerMcp.IntegrationTests;

[Collection("Database")]
public sealed class TableDescribeServiceIntegrationTests
{
    private readonly SqlServerContainerFixture _fixture;
    private const string Server = SqlServerContainerFixture.ServerName;
    private const string Db = SqlServerContainerFixture.TestDatabaseName;

    public TableDescribeServiceIntegrationTests(SqlServerContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task DescribeTable_Products_ReturnsFullDescription()
    {
        var service = ServiceFactory.CreateTableDescribeService(_fixture.ConnectionString);

        var result = await service.DescribeTableAsync(Server, Db,
            "dbo", "Products", CancellationToken.None);

        // Header
        Assert.Contains("# Table: [dbo].[Products]", result);
        Assert.Contains($"**Database:** {Db}", result);

        // Columns section
        Assert.Contains("## Columns", result);
        Assert.Contains("ProductId", result);
        Assert.Contains("Name", result);
        Assert.Contains("CategoryId", result);
        Assert.Contains("Price", result);
        Assert.Contains("CreatedAt", result);

        // Identity
        Assert.Contains("IDENTITY(1,1)", result);

        // Primary key
        Assert.Contains("## Primary Key", result);
        Assert.Contains("PK_Products", result);

        // Indexes
        Assert.Contains("## Indexes", result);
        Assert.Contains("IX_Products_CategoryId", result);

        // Foreign keys
        Assert.Contains("## Foreign Keys", result);
        Assert.Contains("FK_Products_Categories", result);
        Assert.Contains("[dbo].[Categories]", result);

        // Check constraints
        Assert.Contains("## Check Constraints", result);
        Assert.Contains("CK_Products_Price", result);

        // Default constraints
        Assert.Contains("## Default Constraints", result);
        Assert.Contains("DF_Products_CreatedAt", result);
    }

    [Fact]
    public async Task DescribeTable_OrderItems_ShowsCascadeDeleteAndIndex()
    {
        var service = ServiceFactory.CreateTableDescribeService(_fixture.ConnectionString);

        var result = await service.DescribeTableAsync(Server, Db,
            "sales", "OrderItems", CancellationToken.None);

        // Header
        Assert.Contains("# Table: [sales].[OrderItems]", result);

        // Foreign keys with cascade
        Assert.Contains("FK_OrderItems_Orders", result);
        Assert.Contains("CASCADE", result);

        // Cross-schema FK
        Assert.Contains("FK_OrderItems_Products", result);

        // Composite index
        Assert.Contains("IX_OrderItems_OrderId_ProductId", result);

        // Check constraint
        Assert.Contains("CK_OrderItems_Quantity", result);
    }

    [Fact]
    public async Task DescribeTable_NonexistentTable_ThrowsArgumentException()
    {
        var service = ServiceFactory.CreateTableDescribeService(_fixture.ConnectionString);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.DescribeTableAsync(Server, Db,
                "dbo", "NonexistentTable", CancellationToken.None));

        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task DescribeTable_Categories_ShowsUniqueConstraintAndCheckConstraint()
    {
        var service = ServiceFactory.CreateTableDescribeService(_fixture.ConnectionString);

        var result = await service.DescribeTableAsync(Server, Db,
            "dbo", "Categories", CancellationToken.None);

        // Unique constraint shows as an index
        Assert.Contains("UQ_Categories_Name", result);
        Assert.Contains("YES", result); // IsUnique = YES

        // Check constraint
        Assert.Contains("CK_Categories_Name", result);

        // Default
        Assert.Contains("DF_Categories_IsActive", result);
    }
}
