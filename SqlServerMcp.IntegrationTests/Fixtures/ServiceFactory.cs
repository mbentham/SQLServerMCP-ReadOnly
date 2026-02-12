using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SqlServerMcp.Configuration;
using SqlServerMcp.Services;

namespace SqlServerMcp.IntegrationTests.Fixtures;

internal static class ServiceFactory
{
    internal static SqlServerMcpOptions BuildOptions(string connectionString, int maxRows = 1000)
    {
        return new SqlServerMcpOptions
        {
            Servers = new Dictionary<string, SqlServerConnection>
            {
                [SqlServerContainerFixture.ServerName] = new() { ConnectionString = connectionString }
            },
            MaxRows = maxRows,
            CommandTimeoutSeconds = 30
        };
    }

    internal static SqlServerService CreateSqlServerService(string connectionString, int maxRows = 1000)
    {
        var options = Options.Create(BuildOptions(connectionString, maxRows));
        return new SqlServerService(options, NullLogger<SqlServerService>.Instance);
    }

    internal static DiagramService CreateDiagramService(string connectionString)
    {
        var options = Options.Create(BuildOptions(connectionString));
        return new DiagramService(options, NullLogger<DiagramService>.Instance);
    }

    internal static SchemaOverviewService CreateSchemaOverviewService(string connectionString)
    {
        var options = Options.Create(BuildOptions(connectionString));
        return new SchemaOverviewService(options, NullLogger<SchemaOverviewService>.Instance);
    }

    internal static TableDescribeService CreateTableDescribeService(string connectionString)
    {
        var options = Options.Create(BuildOptions(connectionString));
        return new TableDescribeService(options, NullLogger<TableDescribeService>.Instance);
    }
}
