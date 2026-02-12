namespace SqlServerMcp.Services;

public interface IDiagramService
{
    Task<string> GenerateDiagramAsync(string serverName, string databaseName,
        string? includeSchema, IReadOnlyList<string>? excludeSchemas, int maxTables, CancellationToken cancellationToken);
}
