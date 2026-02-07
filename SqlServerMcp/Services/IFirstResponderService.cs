namespace SqlServerMcp.Services;

public interface IFirstResponderService
{
    Task<string> ExecuteBlitzAsync(
        string serverName,
        bool? checkUserDatabaseObjects,
        bool? checkServerInfo,
        int? ignorePrioritiesAbove,
        bool? bringThePain,
        CancellationToken cancellationToken);

    Task<string> ExecuteBlitzFirstAsync(
        string serverName,
        int? seconds,
        bool? expertMode,
        bool? showSleepingSpids,
        bool? sinceStartup,
        int? fileLatencyThresholdMs,
        CancellationToken cancellationToken);

    Task<string> ExecuteBlitzCacheAsync(
        string serverName,
        string? sortOrder,
        int? top,
        bool? expertMode,
        string? databaseName,
        string? slowlySearchPlansFor,
        bool? exportToExcel,
        CancellationToken cancellationToken);

    Task<string> ExecuteBlitzIndexAsync(
        string serverName,
        string? databaseName,
        string? schemaName,
        string? tableName,
        bool? getAllDatabases,
        int? mode,
        int? thresholdMb,
        int? filter,
        CancellationToken cancellationToken);

    Task<string> ExecuteBlitzWhoAsync(
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
        CancellationToken cancellationToken);

    Task<string> ExecuteBlitzLockAsync(
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
        CancellationToken cancellationToken);
}
