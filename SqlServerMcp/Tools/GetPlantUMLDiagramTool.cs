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
        [Description("Optional comma-separated schema names to include (e.g. 'dbo,sales'). If specified, only tables in these schemas are shown. Overrides excludeSchemas.")] string? includeSchemas = null,
        [Description("Optional comma-separated schema names to exclude (e.g. 'audit,staging'). Ignored when includeSchemas is specified.")] string? excludeSchemas = null,
        [Description("Optional comma-separated table names to include (e.g. 'Users,Orders'). If specified, only these tables are shown. Overrides excludeTables.")] string? includeTables = null,
        [Description("Optional comma-separated table names to exclude (e.g. 'AuditLog,TempData'). Ignored when includeTables is specified.")] string? excludeTables = null,
        [Description("Maximum number of tables to include (1-200, default 50)")] int maxTables = 50,
        [Description("When true, shows only primary key and foreign key columns without data types. Useful for high-level relationship maps of large databases.")] bool compact = false,
        CancellationToken cancellationToken = default)
    {
        maxTables = Math.Clamp(maxTables, 1, 200);

        var puml = await ToolHelper.ExecuteAsync(_rateLimiter, () =>
            _diagramService.GenerateDiagramAsync(
                serverName, databaseName,
                ToolHelper.ParseCommaSeparatedList(includeSchemas), ToolHelper.ParseCommaSeparatedList(excludeSchemas),
                ToolHelper.ParseCommaSeparatedList(includeTables), ToolHelper.ParseCommaSeparatedList(excludeTables),
                maxTables, cancellationToken, compact), cancellationToken);

        var fullPath = Path.GetFullPath(outputPath);
        if (!fullPath.EndsWith(".puml", StringComparison.OrdinalIgnoreCase))
            throw new McpException("Output path must have a .puml file extension.");

        var directory = Path.GetDirectoryName(fullPath);
        if (directory is not null)
            Directory.CreateDirectory(directory);

        await File.WriteAllTextAsync(fullPath, puml, cancellationToken);

        var lineCount = puml.AsSpan().Count('\n');
        return $"PlantUML diagram saved to {fullPath} ({lineCount} lines)";
    }
}
