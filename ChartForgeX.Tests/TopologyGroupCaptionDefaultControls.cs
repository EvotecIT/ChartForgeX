using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyGroupCaptionDefaultControls {
    [Theory]
    [InlineData(TopologyLayoutMode.DenseGrouped)]
    [InlineData(TopologyLayoutMode.Layered)]
    public void ExistingDefaultCaptionSceneProvidesBeforeAfterSvgPngAndJsonBytes(TopologyLayoutMode mode) {
        var chart = TopologyChart.Create().WithTitle("").WithViewport(600, 400, 24).WithLayout(mode, TopologyLayoutDirection.LeftToRight)
            .AddAutoGroup("scope", "Scope", subtitle: "Default detail", iconId: "common:server")
            .AddAutoNode("node", "Node", groupId: "scope", width: 144, height: 64);
        chart.Groups[0].LayoutDirection = TopologyLayoutDirection.LeftToRight;
        chart.Groups[0].LayoutPolicy = TopologyGroupLayoutPolicy.VerticalStack;
        chart.Nodes[0].DisplayMode = TopologyNodeDisplayMode.Card; chart.Nodes[0].PreserveDisplayModeSize = true;
        var prepared = chart.Prepare(new TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false });
        var svg = prepared.ToSvg(); var png = prepared.ToPng(); var json = prepared.ToInterchangeEnvelope().ToJson();
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var path = Path.Combine(output, "group-caption-default-" + mode);
            File.WriteAllText(path + ".svg", svg); File.WriteAllBytes(path + ".png", png); File.WriteAllText(path + ".json", json);
        }
        var text = prepared.Visual.Scene.Nodes.OfType<VisualSceneText>().SelectMany(item => item.Text.Lines.Select(line => line.Text));
        Assert.Contains("Scope", text); Assert.Contains("Default detail", text); Assert.Contains("Node", text);
        Assert.True(png.Length > 64); Assert.DoesNotContain("\"showLabel\"", json);
    }
}
