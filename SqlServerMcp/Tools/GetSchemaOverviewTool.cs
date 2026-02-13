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
        [Description("Optional comma-separated schema names to include (e.g. 'dbo,sales'). If specified, only tables in these schemas are shown. Overrides excludeSchemas.")] string? includeSchemas = null,
        [Description("Optional comma-separated schema names to exclude (e.g. 'audit,staging'). Ignored when includeSchemas is specified.")] string? excludeSchemas = null,
        [Description("Optional comma-separated table names to include (e.g. 'Users,Orders'). If specified, only these tables are shown. Overrides excludeTables.")] string? includeTables = null,
        [Description("Optional comma-separated table names to exclude (e.g. 'AuditLog,TempData'). Ignored when includeTables is specified.")] string? excludeTables = null,
        [Description("Maximum number of tables to include (1-200, default 50)")] int maxTables = 50,
        [Description("When true, shows only primary key and foreign key columns without data types. Useful for high-level relationship maps of large databases.")] bool compact = false,
        CancellationToken cancellationToken = default)
    {
        maxTables = Math.Clamp(maxTables, 1, 200);

        return await ToolHelper.ExecuteAsync(_rateLimiter, () =>
            _schemaOverviewService.GenerateOverviewAsync(
                serverName, databaseName,
                ToolHelper.ParseCommaSeparatedList(includeSchemas), ToolHelper.ParseCommaSeparatedList(excludeSchemas),
                ToolHelper.ParseCommaSeparatedList(includeTables), ToolHelper.ParseCommaSeparatedList(excludeTables),
                maxTables, cancellationToken, compact), cancellationToken);
    }
}
