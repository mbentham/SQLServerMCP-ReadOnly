namespace SqlServerMcp.Services;

public interface IDiagramService
{
    Task<string> GenerateDiagramAsync(string serverName, string databaseName,
        string? schemaFilter, int maxTables, CancellationToken cancellationToken);
}
