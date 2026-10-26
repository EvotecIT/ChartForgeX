using System.Text.Json;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeTopologyContourDetailPaintTests {
    [Theory]
    [InlineData(TopologyNodeShape.Browser, false)] [InlineData(TopologyNodeShape.Browser, true)]
    [InlineData(TopologyNodeShape.Bucket, false)] [InlineData(TopologyNodeShape.Bucket, true)]
    [InlineData(TopologyNodeShape.Console, false)] [InlineData(TopologyNodeShape.Console, true)]
    [InlineData(TopologyNodeShape.CollapsedContainer, false)] [InlineData(TopologyNodeShape.CollapsedContainer, true)]
    public void ExplicitContourInkExcludesFixedShapeDetailsAndRestoresThroughTheSameUnitOwner(TopologyNodeShape shape, bool fitted) {
        var chart = Source(shape, fitted); var node = chart.Nodes[0];
        node.BorderColor = "#0000ff"; node.StrokeWidth = 7; node.SurfaceOpacity = .25; node.DashPattern.AddRange(new[] { 9d, 3d });
        var before = Prepare(chart, fitted); var facts = before.ToArtifact().ToInterchangeEnvelope();
        var restored = VisualArtifactInterchangeEnvelope.FromJson(facts.ToJson()).ToTopologyChart().Prepare();
        Capture(shape + "-" + fitted + "-direct", before); Capture(shape + "-" + fitted + "-restored", restored.Visual);
        var marks = before.Scene.Nodes.OfType<VisualScenePath>().Where(mark => mark.Role == "topology-node-surface").ToArray();
        Assert.True(marks.Length > 1); var units = facts.Nodes[0].Width!.Value / node.Width;
        Assert.Equal(ChartColor.FromHex("#0000ff"), marks[0].Stroke); Assert.Equal(.25, marks[0].Opacity);
        var dash = Assert.IsAssignableFrom<IReadOnlyList<double>>(marks[0].Dash);
        Assert.Equal(7 * units, marks[0].StrokeWidth, 6); Assert.Equal(2, dash.Count); Assert.Equal(9 * units, dash[0], 6); Assert.Equal(3 * units, dash[1], 6);
        foreach (var detail in marks.Skip(1)) {
            Assert.Equal(ChartColor.FromHex("#123456"), detail.Stroke); Assert.Equal(1, detail.Opacity); Assert.Null(detail.Dash);
            Assert.Equal(chart.Theme.AxisStrokeWidth * units, detail.StrokeWidth, 6);
        }
        Assert.Equal(TopologyStaticPaintObservation.Paint(before.ToSvg()), TopologyStaticPaintObservation.Paint(restored.ToSvg()));
        Assert.Equal(before.ToPng(), restored.ToPng()); Assert.NotNull(facts.Nodes[0].Topology);
        var topology = facts.Nodes[0].Topology!; Assert.Equal(shape, topology.Shape);
        Assert.Equal(7 * units, topology.StrokeWidth!.Value, 6);
    }
    [Theory]
    [InlineData(TopologyNodeShape.Browser)] [InlineData(TopologyNodeShape.Bucket)] [InlineData(TopologyNodeShape.Console)] [InlineData(TopologyNodeShape.CollapsedContainer)]
    public void OrdinaryAndExplicitNullPoliciesRetainDefaultMarksAndWire(TopologyNodeShape shape) {
        var chart = Source(shape, false); var before = Prepare(chart, false); Capture(shape + "-ordinary", before);
        chart.Nodes[0].BorderColor = null; chart.Nodes[0].SurfaceOpacity = null; chart.Nodes[0].StrokeWidth = null;
        var after = Prepare(chart, false); Capture(shape + "-explicit-null", after);
        Assert.Equal(before.ToSvg(), after.ToSvg()); Assert.Equal(before.ToPng(), after.ToPng()); Assert.Equal(before.ToArtifact().ToInterchangeJson(), after.ToArtifact().ToInterchangeJson());
    }
    private static TopologyChart Source(TopologyNodeShape shape, bool fitted) {
        var chart = TopologyChart.Create().WithId("contour-details").WithTitle("").WithViewport(360, 260, 16).WithLegend(null);
        chart.Nodes.Add(new TopologyNode { Id = "node", Label = "Hidden", Shape = shape, X = fitted ? 1400 : 90, Y = 70, Width = 180, Height = 120,
            Color = "#123456", BackgroundColor = "#ffeedd", ShowLabel = false, ShowStatusBadge = false, PreserveDisplayModeSize = true }); return chart;
    }
    private static PreparedVisual Prepare(TopologyChart chart, bool fitted) => chart.Prepare(
        new VisualRenderContext(new VisualLayoutOptions(new VisualSize(360, 260), 16), frame: new VisualFrame(showLegend: false, transparentBackground: true)),
        new TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false, FitContentToViewport = fitted, CanvasSurfaceStyle = TopologyCanvasSurfaceStyle.Plain });
    private static void Capture(string name, PreparedVisual prepared) {
        if (Environment.GetEnvironmentVariable("CFX_SOURCE_CONTOUR_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); var stem = Path.Combine(output, "native-contour-detail-" + name);
        File.WriteAllText(stem + ".svg", prepared.ToSvg()); File.WriteAllBytes(stem + ".png", prepared.ToPng());
        File.WriteAllText(stem + ".json", prepared.ToArtifact().ToInterchangeJson());
        File.WriteAllText(stem + ".scene.json", JsonSerializer.Serialize(prepared.Scene.Nodes.OfType<VisualScenePath>().Select(mark => new { mark.Role, mark.StrokeWidth, mark.Dash, mark.Opacity, Stroke = mark.Stroke?.ToHexRgba(), Fill = mark.Fill?.ToHexRgba() })));
    }
}
