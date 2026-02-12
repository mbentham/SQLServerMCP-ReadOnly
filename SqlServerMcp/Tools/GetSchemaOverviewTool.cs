using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using SqlServerMcp.Services;

namespace SqlServerMcp.Tools;

[McpServerToolType]
public sealed class GetSchemaOverviewTool
{
    private readonly ISchemaOverviewService _schemaOverviewService;
    private readonly IRateLimitingService _rateLimiter;

    public GetSchemaOverviewTool(ISchemaOverviewService schemaOverviewService, IRateLimitingService rateLimiter)
    {
        _schemaOverviewService = schemaOverviewService;
        _rateLimiter = rateLimiter;
    }

    [McpServerTool(
        Name = "get_schema_overview",
        Title = "Get Database Schema Overview",
        ReadOnly = true,
        Idempotent = true)]
    [Description("Returns a concise Markdown overview of the database schema: tables, columns with data types, primary keys, foreign key references, unique constraints, check constraints, and defaults. Designed for quick context loading — use get_plantuml_diagram for visual PlantUML output or describe_table for full single-table detail.")]
    public async Task<string> GetSchemaOverview(
        [Description("Name of the SQL Server to query (use list_servers to see available names)")] string serverName,
        [Description("Name of the database (use list_databases to see available databases)")] string databaseName,
        [Description("Optional schema name to include (e.g. 'dbo'). If specified, only tables in this schema are shown. Overrides excludeSchemas.")] string? includeSchema = null,
        [Description("Optional comma-separated schema names to exclude (e.g. 'audit,staging'). Ignored when includeSchema is specified.")] string? excludeSchemas = null,
        [Description("Maximum number of tables to include (1-200, default 50)")] int maxTables = 50,
        CancellationToken cancellationToken = default)
    {
        maxTables = Math.Clamp(maxTables, 1, 200);

        return await ToolHelper.ExecuteAsync(_rateLimiter, () =>
            _schemaOverviewService.GenerateOverviewAsync(
                serverName, databaseName, includeSchema, ToolHelper.ParseExcludeSchemas(excludeSchemas),
                maxTables, cancellationToken), cancellationToken);
    }
}
