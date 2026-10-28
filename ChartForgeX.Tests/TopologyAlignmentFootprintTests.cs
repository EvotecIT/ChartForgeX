using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyAlignmentFootprintTests {
    [Theory]
    [InlineData(TopologyNodeDisplayMode.Icon)] [InlineData(TopologyNodeDisplayMode.Tile)]
    public void ExternalCaptionFootprintsAndMeasuredAncestorHeadersParticipateInTheSameSolve(TopologyNodeDisplayMode mode) {
        var chart = TopologyChart.Create().WithTitle("").WithViewport(700, 500).WithLayout(TopologyLayoutMode.Layered)
            .AddAutoGroup("scope", "First line\nSecond line", subtitle: "Measured context", iconId: "common:server")
            .AddAutoNode("first", "Receive all requests", groupId: "scope", subtitle: "Ready", width: 64, height: 46)
            .AddAutoNode("second", "Verify the complete request", groupId: "scope", subtitle: "Review", width: 64, height: 46);
        foreach (var node in chart.Nodes) node.DisplayMode = mode;
        chart.Alignments.Add(new TopologyAlignment("row", TopologyAlignmentKind.Row, new[] { "first", "second" }));
        var prepared = chart.Prepare(new TopologyRenderOptions { IncludeLegend = false, IncludeIconLabels = true, IncludeTileSubtitles = true, AllowMultilineNodeLabels = true, WrapNodeLabels = true });
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var path = Path.Combine(output, "native-alignment-footprints-" + mode);
            File.WriteAllText(path + ".svg", prepared.ToSvg()); File.WriteAllBytes(path + ".native.png", prepared.ToPng()); File.WriteAllText(path + ".native.json", prepared.ToInterchangeEnvelope().ToJson());
        }
        var report = prepared.Analyze(); var group = Assert.Single(report.Groups); Assert.NotNull(group.HeaderBounds);
        Assert.Equal(report.Nodes[0].Bounds.Y + report.Nodes[0].Bounds.Height / 2, report.Nodes[1].Bounds.Y + report.Nodes[1].Bounds.Height / 2, 6);
        foreach (var node in report.Nodes) Assert.True(node.Bounds.Top > group.HeaderBounds!.Value.Bottom);
        var captions = mode == TopologyNodeDisplayMode.Tile
            ? report.Nodes.Select(node => node.CaptionBounds!.Value).ToArray()
            : prepared.Visual.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "topology-node-icon-label").Select(node => node.Bounds).ToArray();
        Assert.Equal(2, captions.Length); Assert.True(captions[0].Right < captions[1].Left);
        foreach (var caption in captions) Assert.True(caption.Left >= group.Bounds.Left && caption.Right <= group.Bounds.Right && caption.Bottom <= group.Bounds.Bottom);
        Assert.DoesNotContain(report.Collisions, collision => collision.Kind == "node-node");
    }
}
