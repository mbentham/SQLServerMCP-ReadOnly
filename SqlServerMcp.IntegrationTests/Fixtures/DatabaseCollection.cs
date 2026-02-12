namespace SqlServerMcp.IntegrationTests.Fixtures;

[CollectionDefinition("Database")]
public sealed class DatabaseCollection : ICollectionFixture<SqlServerContainerFixture>;
