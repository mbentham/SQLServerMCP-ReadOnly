using SqlServerMcp.Configuration;
using SqlServerMcp.Tools;

namespace SqlServerMcp.Tests;

public class ToolRegistryTests
{
    [Fact]
    public void CoreTools_ContainsExpectedTypes()
    {
        Assert.Contains(typeof(ListServersTool), ToolRegistry.CoreTools);
        Assert.Contains(typeof(ListDatabasesTool), ToolRegistry.CoreTools);
        Assert.Contains(typeof(ReadDataTool), ToolRegistry.CoreTools);
        Assert.Contains(typeof(GetDiagramTool), ToolRegistry.CoreTools);
    }

    [Fact]
    public void CoreTools_HasExactCount()
    {
        Assert.Equal(4, ToolRegistry.CoreTools.Length);
    }

    [Fact]
    public void CoreTools_DoesNotContainDbaTools()
    {
        Assert.DoesNotContain(typeof(BlitzTool), ToolRegistry.CoreTools);
        Assert.DoesNotContain(typeof(DescribeTableTool), ToolRegistry.CoreTools);
        Assert.DoesNotContain(typeof(WhoIsActiveTool), ToolRegistry.CoreTools);
        Assert.DoesNotContain(typeof(PressureDetectorTool), ToolRegistry.CoreTools);
    }

    [Fact]
    public void DbaTools_ContainsExpectedTypes()
    {
        Assert.Contains(typeof(DescribeTableTool), ToolRegistry.DbaTools);
        Assert.Contains(typeof(QueryPlanTool), ToolRegistry.DbaTools);
        Assert.Contains(typeof(BlitzTool), ToolRegistry.DbaTools);
        Assert.Contains(typeof(BlitzFirstTool), ToolRegistry.DbaTools);
        Assert.Contains(typeof(BlitzCacheTool), ToolRegistry.DbaTools);
        Assert.Contains(typeof(BlitzIndexTool), ToolRegistry.DbaTools);
        Assert.Contains(typeof(BlitzWhoTool), ToolRegistry.DbaTools);
        Assert.Contains(typeof(BlitzLockTool), ToolRegistry.DbaTools);
        Assert.Contains(typeof(PressureDetectorTool), ToolRegistry.DbaTools);
        Assert.Contains(typeof(QuickieStoreTool), ToolRegistry.DbaTools);
        Assert.Contains(typeof(HealthParserTool), ToolRegistry.DbaTools);
        Assert.Contains(typeof(LogHunterTool), ToolRegistry.DbaTools);
        Assert.Contains(typeof(HumanEventsBlockViewerTool), ToolRegistry.DbaTools);
        Assert.Contains(typeof(IndexCleanupTool), ToolRegistry.DbaTools);
        Assert.Contains(typeof(QueryReproBuilderTool), ToolRegistry.DbaTools);
        Assert.Contains(typeof(WhoIsActiveTool), ToolRegistry.DbaTools);
    }

    [Fact]
    public void DbaTools_HasExactCount()
    {
        Assert.Equal(16, ToolRegistry.DbaTools.Length);
    }

    [Fact]
    public void CoreAndDbaTools_HaveNoOverlap()
    {
        var overlap = ToolRegistry.CoreTools.Intersect(ToolRegistry.DbaTools);
        Assert.Empty(overlap);
    }

    [Fact]
    public void AllRegisteredTools_HaveNoDuplicates()
    {
        var all = ToolRegistry.CoreTools.Concat(ToolRegistry.DbaTools).ToList();
        Assert.Equal(all.Count, all.Distinct().Count());
    }

    [Fact]
    public void GetToolTypes_WhenDbaDisabled_ReturnsCoreOnly()
    {
        var types = ToolRegistry.GetToolTypes(enableDbaTools: false).ToList();

        Assert.Equal(4, types.Count);
        Assert.Contains(typeof(ListServersTool), types);
        Assert.Contains(typeof(ListDatabasesTool), types);
        Assert.Contains(typeof(ReadDataTool), types);
        Assert.Contains(typeof(GetDiagramTool), types);
    }

    [Fact]
    public void GetToolTypes_WhenDbaDisabled_ExcludesDbaTools()
    {
        var types = ToolRegistry.GetToolTypes(enableDbaTools: false).ToList();

        Assert.DoesNotContain(typeof(BlitzTool), types);
        Assert.DoesNotContain(typeof(DescribeTableTool), types);
        Assert.DoesNotContain(typeof(WhoIsActiveTool), types);
        Assert.DoesNotContain(typeof(QueryPlanTool), types);
    }

    [Fact]
    public void GetToolTypes_WhenDbaEnabled_ReturnsAllTools()
    {
        var types = ToolRegistry.GetToolTypes(enableDbaTools: true).ToList();

        Assert.Equal(20, types.Count);
    }

    [Fact]
    public void GetToolTypes_WhenDbaEnabled_IncludesCoreTools()
    {
        var types = ToolRegistry.GetToolTypes(enableDbaTools: true).ToList();

        Assert.Contains(typeof(ListServersTool), types);
        Assert.Contains(typeof(ListDatabasesTool), types);
        Assert.Contains(typeof(ReadDataTool), types);
        Assert.Contains(typeof(GetDiagramTool), types);
    }

    [Fact]
    public void GetToolTypes_WhenDbaEnabled_IncludesDbaTools()
    {
        var types = ToolRegistry.GetToolTypes(enableDbaTools: true).ToList();

        Assert.Contains(typeof(BlitzTool), types);
        Assert.Contains(typeof(DescribeTableTool), types);
        Assert.Contains(typeof(QueryPlanTool), types);
        Assert.Contains(typeof(WhoIsActiveTool), types);
        Assert.Contains(typeof(PressureDetectorTool), types);
    }
}
