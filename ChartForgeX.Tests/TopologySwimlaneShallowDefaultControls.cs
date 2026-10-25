using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologySwimlaneShallowDefaultControls {
    [Theory]
    [InlineData(TopologyLayoutDirection.LeftToRight)] [InlineData(TopologyLayoutDirection.RightToLeft)]
    [InlineData(TopologyLayoutDirection.TopToBottom)] [InlineData(TopologyLayoutDirection.BottomToTop)]
    public void ShallowExistingPublicApiCapturesExactSvgPngAndPortableDefaults(TopologyLayoutDirection direction) {
        var chart = TopologyChart.Create().WithTitle("").WithViewport(800, 500, 24).WithLayout(TopologyLayoutMode.Swimlane, direction)
            .AddAutoGroup("first", "First", subtitle: "Direct lane", iconId: "common:server").AddAutoGroup("second", "Second")
            .AddAutoNode("free", "External", groupId: null, width: 108, height: 48)
            .AddAutoNode("a", "Accept", groupId: "first", width: 144, height: 64)
            .AddAutoNode("b", "Build", groupId: "first", width: 144, height: 64)
            .AddAutoNode("c", "Check", groupId: "second", width: 144, height: 64);
        for (var index = 0; index < chart.Nodes.Count; index++) {
            chart.Nodes[index].Metadata["layer"] = (index % 3).ToString(System.Globalization.CultureInfo.InvariantCulture);
            chart.Nodes[index].DisplayMode = TopologyNodeDisplayMode.Card; chart.Nodes[index].PreserveDisplayModeSize = true;
        }
        var prepared = chart.Prepare(new TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false });
        var svg = prepared.ToSvg(); var png = prepared.ToPng(); var saved = prepared.ToInterchangeEnvelope(); var json = saved.ToJson();
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var path = Path.Combine(output, "swimlane-shallow-default-" + direction);
            File.WriteAllText(path + ".svg", svg); File.WriteAllBytes(path + ".png", png); File.WriteAllText(path + ".json", json);
        }
        Assert.All(chart.Groups, group => Assert.Null(group.ParentGroupId)); Assert.Equal(2, saved.Groups.Count); Assert.Equal(4, saved.Nodes.Count);
        Assert.True(png.Length > 64); Assert.Contains("First", svg); Assert.Contains("Second", svg);
    }
}
