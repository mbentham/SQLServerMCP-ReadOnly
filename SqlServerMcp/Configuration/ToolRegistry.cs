using SqlServerMcp.Tools;

namespace SqlServerMcp.Configuration;

internal static class ToolRegistry
{
    internal static readonly Type[] CoreTools =
    [
        typeof(ListServersTool),
        typeof(ListDatabasesTool),
        typeof(ReadDataTool),
        typeof(GetDiagramTool),
    ];

    internal static readonly Type[] DbaTools =
    [
        // Table / query analysis
        typeof(DescribeTableTool),
        typeof(QueryPlanTool),

        // First Responder Kit
        typeof(BlitzTool),
        typeof(BlitzFirstTool),
        typeof(BlitzCacheTool),
        typeof(BlitzIndexTool),
        typeof(BlitzWhoTool),
        typeof(BlitzLockTool),

        // DarlingData
        typeof(PressureDetectorTool),
        typeof(QuickieStoreTool),
        typeof(HealthParserTool),
        typeof(LogHunterTool),
        typeof(HumanEventsBlockViewerTool),
        typeof(IndexCleanupTool),
        typeof(QueryReproBuilderTool),

        // sp_WhoIsActive
        typeof(WhoIsActiveTool),
    ];

    internal static IEnumerable<Type> GetToolTypes(bool enableDbaTools)
    {
        if (enableDbaTools)
            return CoreTools.Concat(DbaTools);

        return CoreTools;
    }
}
