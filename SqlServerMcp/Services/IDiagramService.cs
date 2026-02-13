namespace SqlServerMcp.Services;

public interface IDiagramService
{
    Task<string> GenerateDiagramAsync(string serverName, string databaseName,
        IReadOnlyList<string>? includeSchemas, IReadOnlyList<string>? excludeSchemas,
        IReadOnlyList<string>? includeTables, IReadOnlyList<string>? excludeTables,
        int maxTables, CancellationToken cancellationToken, bool compact = false);
}
