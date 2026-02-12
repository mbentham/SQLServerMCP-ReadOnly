namespace SqlServerMcp.Services;

public interface ISchemaOverviewService
{
    Task<string> GenerateOverviewAsync(string serverName, string databaseName,
        string? includeSchema, IReadOnlyList<string>? excludeSchemas, int maxTables, CancellationToken cancellationToken);
}
