using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlServerMcp.Configuration;

namespace SqlServerMcp.Services;

public sealed class DarlingDataService : IDarlingDataService
{
    private readonly SqlServerMcpOptions _options;
    private readonly ILogger<DarlingDataService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    internal static readonly HashSet<string> AllowedProcedures = new(StringComparer.OrdinalIgnoreCase)
    {
        "sp_PressureDetector", "sp_QuickieStore", "sp_HealthParser",
        "sp_LogHunter", "sp_HumanEventsBlockViewer", "sp_IndexCleanup",
        "sp_QueryReproBuilder"
    };

    internal static readonly string[] BlockedParameters =
    [
        "@log_to_table",
        "@log_database_name",
        "@log_schema_name",
        "@log_table_name_prefix",
        "@log_retention_days",
        "@output_database_name",
        "@output_schema_name",
        "@delete_retention_days"
    ];

    public DarlingDataService(
        IOptions<SqlServerMcpOptions> options,
        ILogger<DarlingDataService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> ExecutePressureDetectorAsync(
        string serverName,
        string? whatToCheck,
        bool? skipQueries,
        bool? skipPlanXml,
        int? minimumDiskLatencyMs,
        int? cpuUtilizationThreshold,
        bool? skipWaits,
        bool? skipPerfmon,
        int? sampleSeconds,
        bool? troubleshootBlocking,
        bool? gimmeDanger,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, object?>();
        AddIfNotNull(parameters, "@what_to_check", whatToCheck);
        AddBoolParam(parameters, "@skip_queries", skipQueries);
        AddBoolParam(parameters, "@skip_plan_xml", skipPlanXml);
        AddIfNotNull(parameters, "@minimum_disk_latency_ms", minimumDiskLatencyMs);
        AddIfNotNull(parameters, "@cpu_utilization_threshold", cpuUtilizationThreshold);
        AddBoolParam(parameters, "@skip_waits", skipWaits);
        AddBoolParam(parameters, "@skip_perfmon", skipPerfmon);
        AddIfNotNull(parameters, "@sample_seconds", sampleSeconds);
        AddBoolParam(parameters, "@troubleshoot_blocking", troubleshootBlocking);
        AddBoolParam(parameters, "@gimme_danger", gimmeDanger);

        return await ExecuteProcedureAsync(serverName, "sp_PressureDetector", parameters, cancellationToken);
    }

    public async Task<string> ExecuteQuickieStoreAsync(
        string serverName,
        string? databaseName,
        string? sortOrder,
        int? top,
        DateTime? startDate,
        DateTime? endDate,
        int? executionCount,
        int? durationMs,
        string? procedureSchema,
        string? procedureName,
        string? includeQueryIds,
        string? includeQueryHashes,
        string? ignorePlanIds,
        string? ignoreQueryIds,
        string? queryTextSearch,
        string? queryTextSearchNot,
        string? waitFilter,
        string? queryType,
        bool? expertMode,
        bool? formatOutput,
        bool? getAllDatabases,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, object?>();
        AddIfNotNull(parameters, "@database_name", databaseName);
        AddIfNotNull(parameters, "@sort_order", sortOrder);
        AddIfNotNull(parameters, "@top", top);
        AddIfNotNull(parameters, "@start_date", startDate);
        AddIfNotNull(parameters, "@end_date", endDate);
        AddIfNotNull(parameters, "@execution_count", executionCount);
        AddIfNotNull(parameters, "@duration_ms", durationMs);
        AddIfNotNull(parameters, "@procedure_schema", procedureSchema);
        AddIfNotNull(parameters, "@procedure_name", procedureName);
        AddIfNotNull(parameters, "@include_query_ids", includeQueryIds);
        AddIfNotNull(parameters, "@include_query_hashes", includeQueryHashes);
        AddIfNotNull(parameters, "@ignore_plan_ids", ignorePlanIds);
        AddIfNotNull(parameters, "@ignore_query_ids", ignoreQueryIds);
        AddIfNotNull(parameters, "@query_text_search", queryTextSearch);
        AddIfNotNull(parameters, "@query_text_search_not", queryTextSearchNot);
        AddIfNotNull(parameters, "@wait_filter", waitFilter);
        AddIfNotNull(parameters, "@query_type", queryType);
        AddBoolParam(parameters, "@expert_mode", expertMode);
        AddBoolParam(parameters, "@format_output", formatOutput);
        AddBoolParam(parameters, "@get_all_databases", getAllDatabases);

        return await ExecuteProcedureAsync(serverName, "sp_QuickieStore", parameters, cancellationToken);
    }

    public async Task<string> ExecuteHealthParserAsync(
        string serverName,
        string? whatToCheck,
        DateTime? startDate,
        DateTime? endDate,
        bool? warningsOnly,
        string? databaseName,
        int? waitDurationMs,
        int? waitRoundIntervalMinutes,
        bool? skipLocks,
        int? pendingTaskThreshold,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, object?>();
        AddIfNotNull(parameters, "@what_to_check", whatToCheck);
        AddIfNotNull(parameters, "@start_date", startDate);
        AddIfNotNull(parameters, "@end_date", endDate);
        AddBoolParam(parameters, "@warnings_only", warningsOnly);
        AddIfNotNull(parameters, "@database_name", databaseName);
        AddIfNotNull(parameters, "@wait_duration_ms", waitDurationMs);
        AddIfNotNull(parameters, "@wait_round_interval_minutes", waitRoundIntervalMinutes);
        AddBoolParam(parameters, "@skip_locks", skipLocks);
        AddIfNotNull(parameters, "@pending_task_threshold", pendingTaskThreshold);

        return await ExecuteProcedureAsync(serverName, "sp_HealthParser", parameters, cancellationToken);
    }

    public async Task<string> ExecuteLogHunterAsync(
        string serverName,
        int? daysBack,
        DateTime? startDate,
        DateTime? endDate,
        string? customMessage,
        bool? customMessageOnly,
        bool? firstLogOnly,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, object?>();
        AddIfNotNull(parameters, "@days_back", daysBack);
        AddIfNotNull(parameters, "@start_date", startDate);
        AddIfNotNull(parameters, "@end_date", endDate);
        AddIfNotNull(parameters, "@custom_message", customMessage);
        AddBoolParam(parameters, "@custom_message_only", customMessageOnly);
        AddBoolParam(parameters, "@first_log_only", firstLogOnly);

        return await ExecuteProcedureAsync(serverName, "sp_LogHunter", parameters, cancellationToken);
    }

    public async Task<string> ExecuteHumanEventsBlockViewerAsync(
        string serverName,
        string? sessionName,
        string? targetType,
        DateTime? startDate,
        DateTime? endDate,
        string? databaseName,
        string? objectName,
        int? maxBlockingEvents,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, object?>();
        AddIfNotNull(parameters, "@session_name", sessionName);
        AddIfNotNull(parameters, "@target_type", targetType);
        AddIfNotNull(parameters, "@start_date", startDate);
        AddIfNotNull(parameters, "@end_date", endDate);
        AddIfNotNull(parameters, "@database_name", databaseName);
        AddIfNotNull(parameters, "@object_name", objectName);
        AddIfNotNull(parameters, "@max_blocking_events", maxBlockingEvents);

        return await ExecuteProcedureAsync(serverName, "sp_HumanEventsBlockViewer", parameters, cancellationToken);
    }

    public async Task<string> ExecuteIndexCleanupAsync(
        string serverName,
        string? databaseName,
        string? schemaName,
        string? tableName,
        int? minReads,
        int? minWrites,
        int? minSizeGb,
        int? minRows,
        bool? dedupeOnly,
        bool? getAllDatabases,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, object?>();
        AddIfNotNull(parameters, "@database_name", databaseName);
        AddIfNotNull(parameters, "@schema_name", schemaName);
        AddIfNotNull(parameters, "@table_name", tableName);
        AddIfNotNull(parameters, "@min_reads", minReads);
        AddIfNotNull(parameters, "@min_writes", minWrites);
        AddIfNotNull(parameters, "@min_size_gb", minSizeGb);
        AddIfNotNull(parameters, "@min_rows", minRows);
        AddBoolParam(parameters, "@dedupe_only", dedupeOnly);
        AddBoolParam(parameters, "@get_all_databases", getAllDatabases);

        return await ExecuteProcedureAsync(serverName, "sp_IndexCleanup", parameters, cancellationToken);
    }

    public async Task<string> ExecuteQueryReproBuilderAsync(
        string serverName,
        string? databaseName,
        DateTime? startDate,
        DateTime? endDate,
        string? includePlanIds,
        string? includeQueryIds,
        string? ignorePlanIds,
        string? ignoreQueryIds,
        string? procedureSchema,
        string? procedureName,
        string? queryTextSearch,
        string? queryTextSearchNot,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, object?>();
        AddIfNotNull(parameters, "@database_name", databaseName);
        AddIfNotNull(parameters, "@start_date", startDate);
        AddIfNotNull(parameters, "@end_date", endDate);
        AddIfNotNull(parameters, "@include_plan_ids", includePlanIds);
        AddIfNotNull(parameters, "@include_query_ids", includeQueryIds);
        AddIfNotNull(parameters, "@ignore_plan_ids", ignorePlanIds);
        AddIfNotNull(parameters, "@ignore_query_ids", ignoreQueryIds);
        AddIfNotNull(parameters, "@procedure_schema", procedureSchema);
        AddIfNotNull(parameters, "@procedure_name", procedureName);
        AddIfNotNull(parameters, "@query_text_search", queryTextSearch);
        AddIfNotNull(parameters, "@query_text_search_not", queryTextSearchNot);

        return await ExecuteProcedureAsync(serverName, "sp_QueryReproBuilder", parameters, cancellationToken);
    }

    private async Task<string> ExecuteProcedureAsync(
        string serverName,
        string procedureName,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        if (!AllowedProcedures.Contains(procedureName))
            throw new InvalidOperationException(
                $"Procedure '{procedureName}' is not in the allowed list.");

        foreach (var paramName in parameters.Keys)
        {
            if (BlockedParameters.Any(blocked =>
                paramName.Equals(blocked, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    $"Parameter '{paramName}' is not allowed (output/logging parameters are blocked).");
            }
        }

        var serverConfig = ResolveServer(serverName);

        await using var connection = new SqlConnection(serverConfig.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(procedureName, connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = _options.CommandTimeoutSeconds
        };

        foreach (var (name, value) in parameters)
        {
            if (value is not null)
                command.Parameters.AddWithValue(name, value);
        }

        _logger.LogInformation("Executing {Procedure} on server {Server}", procedureName, serverName);

        try
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await FormatResultSetsAsync(reader, serverName, procedureName, cancellationToken);
        }
        catch (SqlException ex) when (ex.Number == 2812)
        {
            throw new InvalidOperationException(
                $"Stored procedure '{procedureName}' not found on server '{serverName}'. " +
                "The DarlingData toolkit must be installed. " +
                "See: https://github.com/erikdarlingdata/DarlingData");
        }
    }

    private async Task<string> FormatResultSetsAsync(
        SqlDataReader reader,
        string serverName,
        string procedureName,
        CancellationToken cancellationToken)
    {
        var resultSets = new List<Dictionary<string, object?>>();

        do
        {
            if (reader.FieldCount == 0)
                continue;

            var columns = new List<Dictionary<string, string>>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                columns.Add(new Dictionary<string, string>
                {
                    ["name"] = reader.GetName(i),
                    ["type"] = reader.GetFieldType(i)?.Name ?? "Unknown"
                });
            }

            var rows = new List<Dictionary<string, object?>>();
            var truncated = false;
            while (await reader.ReadAsync(cancellationToken))
            {
                if (rows.Count >= _options.MaxRows)
                {
                    truncated = true;
                    break;
                }

                var row = new Dictionary<string, object?>();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var name = reader.GetName(i);
                    var value = reader.IsDBNull(i) ? null : FormatValue(reader.GetValue(i));
                    row[name] = value;
                }
                rows.Add(row);
            }

            resultSets.Add(new Dictionary<string, object?>
            {
                ["columns"] = columns,
                ["rows"] = rows,
                ["rowCount"] = rows.Count,
                ["truncated"] = truncated
            });

        } while (await reader.NextResultAsync(cancellationToken));

        var response = new Dictionary<string, object?>
        {
            ["server"] = serverName,
            ["procedureName"] = procedureName,
            ["resultSets"] = resultSets
        };

        return JsonSerializer.Serialize(response, JsonOptions);
    }

    private SqlServerConnection ResolveServer(string serverName)
    {
        if (!_options.Servers.TryGetValue(serverName, out var serverConfig))
        {
            var available = string.Join(", ", _options.Servers.Keys.OrderBy(k => k));
            throw new ArgumentException(
                $"Server '{serverName}' not found. Available servers: {available}");
        }
        return serverConfig;
    }

    private static void AddIfNotNull(Dictionary<string, object?> parameters, string name, object? value)
    {
        if (value is not null)
            parameters[name] = value;
    }

    private static void AddBoolParam(Dictionary<string, object?> parameters, string name, bool? value)
    {
        if (value.HasValue)
            parameters[name] = value.Value ? 1 : 0;
    }

    private static object FormatValue(object value) => value switch
    {
        DateTime dt => dt.ToString("O"),
        DateTimeOffset dto => dto.ToString("O"),
        byte[] bytes => Convert.ToBase64String(bytes),
        _ => value
    };
}
