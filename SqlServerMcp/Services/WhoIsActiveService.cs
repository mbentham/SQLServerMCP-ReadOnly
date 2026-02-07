using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlServerMcp.Configuration;

namespace SqlServerMcp.Services;

public sealed class WhoIsActiveService : IWhoIsActiveService
{
    private readonly SqlServerMcpOptions _options;
    private readonly ILogger<WhoIsActiveService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    internal static readonly HashSet<string> AllowedProcedures = new(StringComparer.OrdinalIgnoreCase)
    {
        "sp_WhoIsActive"
    };

    internal static readonly string[] BlockedParameters =
    [
        "@destination_table",
        "@return_schema",
        "@schema",
        "@help"
    ];

    public WhoIsActiveService(
        IOptions<SqlServerMcpOptions> options,
        ILogger<WhoIsActiveService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> ExecuteWhoIsActiveAsync(
        string serverName,
        string? filter,
        string? filterType,
        string? notFilter,
        string? notFilterType,
        bool? showOwnSpid,
        bool? showSystemSpids,
        int? showSleepingSpids,
        bool? getFullInnerText,
        int? getPlans,
        bool? getOuterCommand,
        bool? getTransactionInfo,
        int? getTaskInfo,
        bool? getLocks,
        bool? getAvgTime,
        bool? getAdditionalInfo,
        bool? getMemoryInfo,
        bool? findBlockLeaders,
        int? deltaInterval,
        string? sortOrder,
        bool? formatOutput,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, object?>();
        AddIfNotNull(parameters, "@filter", filter);
        AddIfNotNull(parameters, "@filter_type", filterType);
        AddIfNotNull(parameters, "@not_filter", notFilter);
        AddIfNotNull(parameters, "@not_filter_type", notFilterType);
        AddBoolParam(parameters, "@show_own_spid", showOwnSpid);
        AddBoolParam(parameters, "@show_system_spids", showSystemSpids);
        AddIfNotNull(parameters, "@show_sleeping_spids", showSleepingSpids);
        AddBoolParam(parameters, "@get_full_inner_text", getFullInnerText);
        AddIfNotNull(parameters, "@get_plans", getPlans);
        AddBoolParam(parameters, "@get_outer_command", getOuterCommand);
        AddBoolParam(parameters, "@get_transaction_info", getTransactionInfo);
        AddIfNotNull(parameters, "@get_task_info", getTaskInfo);
        AddBoolParam(parameters, "@get_locks", getLocks);
        AddBoolParam(parameters, "@get_avg_time", getAvgTime);
        AddBoolParam(parameters, "@get_additional_info", getAdditionalInfo);
        AddBoolParam(parameters, "@get_memory_info", getMemoryInfo);
        AddBoolParam(parameters, "@find_block_leaders", findBlockLeaders);
        AddIfNotNull(parameters, "@delta_interval", deltaInterval);
        AddIfNotNull(parameters, "@sort_order", sortOrder);
        AddBoolParam(parameters, "@format_output", formatOutput);

        return await ExecuteProcedureAsync(serverName, "sp_WhoIsActive", parameters, cancellationToken);
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
                    $"Parameter '{paramName}' is not allowed (output/schema parameters are blocked).");
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
                "sp_WhoIsActive must be installed. " +
                "See: https://github.com/amachanic/sp_whoisactive");
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
