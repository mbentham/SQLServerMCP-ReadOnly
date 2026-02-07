using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlServerMcp.Configuration;

namespace SqlServerMcp.Services;

public sealed class FirstResponderService : IFirstResponderService
{
    private readonly SqlServerMcpOptions _options;
    private readonly ILogger<FirstResponderService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    internal static readonly HashSet<string> AllowedProcedures = new(StringComparer.OrdinalIgnoreCase)
    {
        "sp_Blitz", "sp_BlitzFirst", "sp_BlitzCache",
        "sp_BlitzIndex", "sp_BlitzWho", "sp_BlitzLock"
    };

    internal static readonly string[] BlockedParameterPrefixes =
    [
        "@OutputDatabaseName", "@OutputSchemaName", "@OutputTableName",
        "@OutputServerName", "@OutputTableNameFileStats",
        "@OutputTableNamePerfmonStats", "@OutputTableNameWaitStats",
        "@OutputTableRetentionDays"
    ];

    public FirstResponderService(
        IOptions<SqlServerMcpOptions> options,
        ILogger<FirstResponderService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> ExecuteBlitzAsync(
        string serverName,
        bool? checkUserDatabaseObjects,
        bool? checkServerInfo,
        int? ignorePrioritiesAbove,
        bool? bringThePain,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, object?>();
        AddBoolParam(parameters, "@CheckUserDatabaseObjects", checkUserDatabaseObjects);
        AddBoolParam(parameters, "@CheckServerInfo", checkServerInfo);
        AddIfNotNull(parameters, "@IgnorePrioritiesAbove", ignorePrioritiesAbove);
        AddBoolParam(parameters, "@BringThePain", bringThePain);

        return await ExecuteProcedureAsync(serverName, "sp_Blitz", parameters, cancellationToken);
    }

    public async Task<string> ExecuteBlitzFirstAsync(
        string serverName,
        int? seconds,
        bool? expertMode,
        bool? showSleepingSpids,
        bool? sinceStartup,
        int? fileLatencyThresholdMs,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, object?>();
        AddIfNotNull(parameters, "@Seconds", seconds);
        AddBoolParam(parameters, "@ExpertMode", expertMode);
        AddBoolParam(parameters, "@ShowSleepingSPIDs", showSleepingSpids);
        AddBoolParam(parameters, "@SinceStartup", sinceStartup);
        AddIfNotNull(parameters, "@FileLatencyThresholdMS", fileLatencyThresholdMs);

        return await ExecuteProcedureAsync(serverName, "sp_BlitzFirst", parameters, cancellationToken);
    }

    public async Task<string> ExecuteBlitzCacheAsync(
        string serverName,
        string? sortOrder,
        int? top,
        bool? expertMode,
        string? databaseName,
        string? slowlySearchPlansFor,
        bool? exportToExcel,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, object?>();
        AddIfNotNull(parameters, "@SortOrder", sortOrder);
        AddIfNotNull(parameters, "@Top", top);
        AddBoolParam(parameters, "@ExpertMode", expertMode);
        AddIfNotNull(parameters, "@DatabaseName", databaseName);
        AddIfNotNull(parameters, "@SlowlySearchPlansFor", slowlySearchPlansFor);
        AddBoolParam(parameters, "@ExportToExcel", exportToExcel);

        return await ExecuteProcedureAsync(serverName, "sp_BlitzCache", parameters, cancellationToken);
    }

    public async Task<string> ExecuteBlitzIndexAsync(
        string serverName,
        string? databaseName,
        string? schemaName,
        string? tableName,
        bool? getAllDatabases,
        int? mode,
        int? thresholdMb,
        int? filter,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, object?>();
        AddIfNotNull(parameters, "@DatabaseName", databaseName);
        AddIfNotNull(parameters, "@SchemaName", schemaName);
        AddIfNotNull(parameters, "@TableName", tableName);
        AddBoolParam(parameters, "@GetAllDatabases", getAllDatabases);
        AddIfNotNull(parameters, "@Mode", mode);
        AddIfNotNull(parameters, "@ThresholdMB", thresholdMb);
        AddIfNotNull(parameters, "@Filter", filter);

        return await ExecuteProcedureAsync(serverName, "sp_BlitzIndex", parameters, cancellationToken);
    }

    public async Task<string> ExecuteBlitzWhoAsync(
        string serverName,
        bool? expertMode,
        bool? showSleepingSpids,
        int? minElapsedSeconds,
        int? minCpuTime,
        int? minLogicalReads,
        int? minBlockingSeconds,
        int? minTempdbMb,
        bool? showActualParameters,
        bool? getLiveQueryPlan,
        string? sortOrder,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, object?>();
        AddBoolParam(parameters, "@ExpertMode", expertMode);
        AddBoolParam(parameters, "@ShowSleepingSPIDs", showSleepingSpids);
        AddIfNotNull(parameters, "@MinElapsedSeconds", minElapsedSeconds);
        AddIfNotNull(parameters, "@MinCPUTime", minCpuTime);
        AddIfNotNull(parameters, "@MinLogicalReads", minLogicalReads);
        AddIfNotNull(parameters, "@MinBlockingSeconds", minBlockingSeconds);
        AddIfNotNull(parameters, "@MinTempdbMB", minTempdbMb);
        AddBoolParam(parameters, "@ShowActualParameters", showActualParameters);
        AddBoolParam(parameters, "@GetLiveQueryPlan", getLiveQueryPlan);
        AddIfNotNull(parameters, "@SortOrder", sortOrder);

        return await ExecuteProcedureAsync(serverName, "sp_BlitzWho", parameters, cancellationToken);
    }

    public async Task<string> ExecuteBlitzLockAsync(
        string serverName,
        string? databaseName,
        DateTime? startDate,
        DateTime? endDate,
        string? objectName,
        string? storedProcName,
        string? appName,
        string? hostName,
        string? loginName,
        bool? victimsOnly,
        string? eventSessionName,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, object?>();
        AddIfNotNull(parameters, "@DatabaseName", databaseName);
        AddIfNotNull(parameters, "@StartDate", startDate);
        AddIfNotNull(parameters, "@EndDate", endDate);
        AddIfNotNull(parameters, "@ObjectName", objectName);
        AddIfNotNull(parameters, "@StoredProcName", storedProcName);
        AddIfNotNull(parameters, "@AppName", appName);
        AddIfNotNull(parameters, "@HostName", hostName);
        AddIfNotNull(parameters, "@LoginName", loginName);
        AddBoolParam(parameters, "@VictimsOnly", victimsOnly);
        AddIfNotNull(parameters, "@EventSessionName", eventSessionName);

        return await ExecuteProcedureAsync(serverName, "sp_BlitzLock", parameters, cancellationToken);
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
            if (BlockedParameterPrefixes.Any(blocked =>
                paramName.StartsWith(blocked, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    $"Parameter '{paramName}' is not allowed (output table parameters are blocked).");
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
                "The First Responder Kit must be installed. " +
                "See: https://github.com/BrentOzarULTD/SQL-Server-First-Responder-Kit");
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
