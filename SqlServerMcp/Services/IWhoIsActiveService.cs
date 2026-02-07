namespace SqlServerMcp.Services;

public interface IWhoIsActiveService
{
    Task<string> ExecuteWhoIsActiveAsync(
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
        CancellationToken cancellationToken);
}
