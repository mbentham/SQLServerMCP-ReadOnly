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
        Assert.Contains(typeof(QueryPlanTool), ToolRegistry.CoreTools);
        Assert.Contains(typeof(GetPlantUMLDiagramTool), ToolRegistry.CoreTools);
        Assert.Contains(typeof(GetSchemaOverviewTool), ToolRegistry.CoreTools);
        Assert.Contains(typeof(DescribeTableTool), ToolRegistry.CoreTools);
    }

    [Fact]
    public void CoreTools_HasExactCount()
    {
        Assert.Equal(7, ToolRegistry.CoreTools.Length);
    }

    [Fact]
    public void FirstResponderKitTools_ContainsExpectedTypes()
    {
        Assert.Contains(typeof(BlitzTool), ToolRegistry.FirstResponderKitTools);
        Assert.Contains(typeof(BlitzFirstTool), ToolRegistry.FirstResponderKitTools);
        Assert.Contains(typeof(BlitzCacheTool), ToolRegistry.FirstResponderKitTools);
        Assert.Contains(typeof(BlitzIndexTool), ToolRegistry.FirstResponderKitTools);
        Assert.Contains(typeof(BlitzWhoTool), ToolRegistry.FirstResponderKitTools);
        Assert.Contains(typeof(BlitzLockTool), ToolRegistry.FirstResponderKitTools);
    }

    [Fact]
    public void FirstResponderKitTools_HasExactCount()
    {
        Assert.Equal(6, ToolRegistry.FirstResponderKitTools.Length);
    }

    [Fact]
    public void DarlingDataTools_ContainsExpectedTypes()
    {
        Assert.Contains(typeof(PressureDetectorTool), ToolRegistry.DarlingDataTools);
        Assert.Contains(typeof(QuickieStoreTool), ToolRegistry.DarlingDataTools);
        Assert.Contains(typeof(HealthParserTool), ToolRegistry.DarlingDataTools);
        Assert.Contains(typeof(LogHunterTool), ToolRegistry.DarlingDataTools);
        Assert.Contains(typeof(HumanEventsBlockViewerTool), ToolRegistry.DarlingDataTools);
        Assert.Contains(typeof(IndexCleanupTool), ToolRegistry.DarlingDataTools);
        Assert.Contains(typeof(QueryReproBuilderTool), ToolRegistry.DarlingDataTools);
    }

    [Fact]
    public void DarlingDataTools_HasExactCount()
    {
        Assert.Equal(7, ToolRegistry.DarlingDataTools.Length);
    }

    [Fact]
    public void WhoIsActiveTools_ContainsExpectedTypes()
    {
        Assert.Contains(typeof(WhoIsActiveTool), ToolRegistry.WhoIsActiveTools);
    }

    [Fact]
    public void WhoIsActiveTools_HasExactCount()
    {
        Assert.Single(ToolRegistry.WhoIsActiveTools);
    }

    [Fact]
    public void AllArrays_HaveNoOverlap()
    {
        var arrays = new[]
        {
            ToolRegistry.CoreTools,
            ToolRegistry.FirstResponderKitTools,
            ToolRegistry.DarlingDataTools,
            ToolRegistry.WhoIsActiveTools,
        };

        for (var i = 0; i < arrays.Length; i++)
        {
            for (var j = i + 1; j < arrays.Length; j++)
            {
                var overlap = arrays[i].Intersect(arrays[j]);
                Assert.Empty(overlap);
            }
        }
    }

    [Fact]
    public void AllRegisteredTools_HaveNoDuplicates()
    {
        var all = ToolRegistry.CoreTools
            .Concat(ToolRegistry.FirstResponderKitTools)
            .Concat(ToolRegistry.DarlingDataTools)
            .Concat(ToolRegistry.WhoIsActiveTools)
            .ToList();

        Assert.Equal(all.Count, all.Distinct().Count());
    }

    [Fact]
    public void GetToolTypes_AllDisabled_ReturnsCoreOnly()
    {
        var types = ToolRegistry.GetToolTypes(
            enableFirstResponderKit: false, enableDarlingData: false, enableWhoIsActive: false).ToList();

        Assert.Equal(7, types.Count);
        Assert.Contains(typeof(ListServersTool), types);
        Assert.Contains(typeof(QueryPlanTool), types);
        Assert.DoesNotContain(typeof(BlitzTool), types);
        Assert.DoesNotContain(typeof(PressureDetectorTool), types);
        Assert.DoesNotContain(typeof(WhoIsActiveTool), types);
    }

    [Fact]
    public void GetToolTypes_FirstResponderKitOnly_Returns13()
    {
        var types = ToolRegistry.GetToolTypes(
            enableFirstResponderKit: true, enableDarlingData: false, enableWhoIsActive: false).ToList();

        Assert.Equal(13, types.Count);
        Assert.Contains(typeof(BlitzTool), types);
        Assert.DoesNotContain(typeof(PressureDetectorTool), types);
        Assert.DoesNotContain(typeof(WhoIsActiveTool), types);
    }

    [Fact]
    public void GetToolTypes_DarlingDataOnly_Returns14()
    {
        var types = ToolRegistry.GetToolTypes(
            enableFirstResponderKit: false, enableDarlingData: true, enableWhoIsActive: false).ToList();

        Assert.Equal(14, types.Count);
        Assert.DoesNotContain(typeof(BlitzTool), types);
        Assert.Contains(typeof(PressureDetectorTool), types);
        Assert.DoesNotContain(typeof(WhoIsActiveTool), types);
    }

    [Fact]
    public void GetToolTypes_WhoIsActiveOnly_Returns8()
    {
        var types = ToolRegistry.GetToolTypes(
            enableFirstResponderKit: false, enableDarlingData: false, enableWhoIsActive: true).ToList();

        Assert.Equal(8, types.Count);
        Assert.DoesNotContain(typeof(BlitzTool), types);
        Assert.DoesNotContain(typeof(PressureDetectorTool), types);
        Assert.Contains(typeof(WhoIsActiveTool), types);
    }

    [Fact]
    public void GetToolTypes_AllEnabled_ReturnsAllTools()
    {
        var types = ToolRegistry.GetToolTypes(
            enableFirstResponderKit: true, enableDarlingData: true, enableWhoIsActive: true).ToList();

        Assert.Equal(21, types.Count);
    }
}
