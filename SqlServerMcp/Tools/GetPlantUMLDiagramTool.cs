using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using SqlServerMcp.Services;

namespace SqlServerMcp.Tools;

[McpServerToolType]
public sealed class GetPlantUMLDiagramTool
{
    private readonly IDiagramService _diagramService;
    private readonly IRateLimitingService _rateLimiter;

    public GetPlantUMLDiagramTool(IDiagramService diagramService, IRateLimitingService rateLimiter)
    {
        _diagramService = diagramService;
        _rateLimiter = rateLimiter;
    }

    [McpServerTool(
        Name = "get_plantuml_diagram",
        Title = "Get Database ER Diagram",
        ReadOnly = false,
        Idempotent = false)]
    [Description("Generate a PlantUML ER diagram for a SQL Server database. Saves PlantUML text to the specified file path showing tables, columns, primary keys, and foreign key relationships with smart cardinality.")]
    public async Task<string> GetDiagram(
        [Description("Name of the SQL Server to query (use list_servers to see available names)")] string serverName,
        [Description("Name of the database to diagram (use list_databases to see available databases)")] string databaseName,
        [Description("File path to save the PlantUML diagram output (e.g. '/tmp/diagram.puml')")] string outputPath,
        [Description("Optional schema name to filter tables (e.g. 'dbo'). If omitted, all user schemas are included.")] string? schemaFilter = null,
        [Description("Maximum number of tables to include (1-200, default 50)")] int maxTables = 50,
        CancellationToken cancellationToken = default)
    {
        maxTables = Math.Clamp(maxTables, 1, 200);

        var puml = await ToolHelper.ExecuteAsync(_rateLimiter, () =>
            _diagramService.GenerateDiagramAsync(
                serverName, databaseName, schemaFilter, maxTables, cancellationToken), cancellationToken);

        var fullPath = Path.GetFullPath(outputPath);
        var directory = Path.GetDirectoryName(fullPath);
        if (directory is not null)
            Directory.CreateDirectory(directory);

        await File.WriteAllTextAsync(fullPath, puml, cancellationToken);

        var lineCount = puml.AsSpan().Count('\n');
        return $"PlantUML diagram saved to {fullPath} ({lineCount} lines)";
    }
}
