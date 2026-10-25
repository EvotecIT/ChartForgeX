using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class TopologyExplicitLabelMarkerTests {
    internal static TopologyChart Source(TopologyLabelVerticalAnchor anchor = TopologyLabelVerticalAnchor.TextBaseline) {
        var chart = TopologyChart.Create().WithId("pinned-handoff").WithViewport(760, 440, 24);
        chart.AddNode("a", "Accept", 50, 160, width: 150, height: 70); chart.AddNode("b", "Build", 540, 160, width: 150, height: 70);
        chart.AddEdge("handoff", "a", "b", "Review", direction: VisualLinkDirection.Bidirectional);
        var edge = chart.Edges[0]; edge.PathGeometry = new TopologyPathGeometry("M 200 195 C 300 25 440 25 540 195");
        edge.LabelPosition = new TopologyLabelPosition(new ChartPoint(370, 70), anchor);
        edge.LabelAnchorX = 370; edge.LabelAnchorY = 100; edge.HasLabelAnchorOverride = true;
        edge.SourceMarkerStyle = new TopologyMarkerStyle { Scale = 1.5, Offset = 2, StrokeWidth = 0 };
        edge.TargetMarkerStyle = new TopologyMarkerStyle { Scale = 2, Offset = 3, StrokeWidth = 0 };
        edge.TextStyle = new TextStyleOverride { FontSize = 18, FontWeight = "400" };
        return chart;
    }

    [Theory]
    [InlineData(false, TextBaseline.Normal)] [InlineData(true, TextBaseline.Normal)]
    [InlineData(true, TextBaseline.Subscript)] [InlineData(true, TextBaseline.Superscript)]
    public void ActualTextBaselineAndEndpointStylesAreCapturedOnceThroughFitAndRestore(bool fit, TextBaseline baseline) {
        var source = Source(); source.Edges[0].TextStyle!.Baseline = baseline; var point = source.Edges[0].LabelPosition!; var sourceMarker = source.Edges[0].TargetMarkerStyle!;
        var options = new TopologyRenderOptions { FitContentToViewport = fit, IncludeLegend = false, IncludeEdgeLabelBackplates = false };
        var prepared = fit ? source.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(400, 280), 20)), options) : source.Prepare(options).Visual;
        var saved = prepared.ToArtifact().ToInterchangeEnvelope(); var native = saved.Edges[0].Topology!;
        Capture(prepared, "explicit-baseline-marker-" + fit + "-" + baseline);
        var xml = XDocument.Parse(prepared.ToSvg()); var text = PaintedLabel(xml); Assert.Equal("Review", text.Value);
        var ink = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), item => item.Role == "topology-edge-label-text");
        // The authored X is the label centre; SVG x is the first glyph's left edge. Compare each through its actual owner.
        Assert.Equal(native.LabelPosition!.Point.Y, ink.Baseline, 2); Assert.Equal(native.LabelPosition.Point.X, ink.X, 2);
        Assert.Equal(SvgMarkupWriter.FormatNumber(ink.Baseline), (string?)text.Attribute("y"));
        Assert.Equal(SvgMarkupWriter.FormatNumber(ink.LineLeft(Assert.Single(ink.Text.Lines))), (string?)text.Attribute("x"));
        Assert.Equal(TopologyLabelVerticalAnchor.TextBaseline, native.LabelPosition.VerticalAnchor);
        var scale = saved.Nodes[0].Width!.Value / source.Nodes[0].Width;
        Assert.Equal(2 * scale, native.TargetMarkerStyle!.Scale, 8); Assert.Equal(3 * scale, native.TargetMarkerStyle.Offset, 8); Assert.Equal(0, native.TargetMarkerStyle.StrokeWidth);
        Assert.Equal(2, saved.Version); var restored = VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToTopologyChart();
        Assert.Equal(native.LabelPosition.Point.Y, restored.Edges[0].LabelPosition!.Point.Y);
        Assert.Equal(native.TargetMarkerStyle.Scale, restored.Edges[0].TargetMarkerStyle!.Scale);
        var restoredPrepared = restored.Prepare(); var restoredText = PaintedLabel(XDocument.Parse(restoredPrepared.ToSvg())); Assert.Equal("Review", restoredText.Value);
        var restoredInk = Assert.Single(restoredPrepared.Visual.Scene.Nodes.OfType<VisualSceneText>(), item => item.Role == "topology-edge-label-text");
        Assert.Equal(native.LabelPosition.Point.Y, restoredInk.Baseline, 2); Assert.Equal(native.LabelPosition.Point.X, restoredInk.X, 2);
        Assert.Equal(SvgMarkupWriter.FormatNumber(restoredInk.Baseline), (string?)restoredText.Attribute("y"));
        Assert.Equal(SvgMarkupWriter.FormatNumber(restoredInk.LineLeft(Assert.Single(restoredInk.Text.Lines))), (string?)restoredText.Attribute("x"));
        sourceMarker.Scale = 4; Assert.Equal(2 * scale, native.TargetMarkerStyle.Scale, 8);
        Assert.Same(point, source.Edges[0].LabelPosition); Assert.Equal(70, point.Point.Y);
        Assert.True(prepared.ToPng().Length > 64); Assert.True(restored.ToPng().Length > 64);
    }

    [Theory]
    [InlineData(VisualLinkDirection.Forward, 1)] [InlineData(VisualLinkDirection.Backward, 1)] [InlineData(VisualLinkDirection.Bidirectional, 2)]
    public void StyledMarkersFollowExactCubicEndpointTangentsAndDirection(VisualLinkDirection direction, int count) {
        var chart = Source(); chart.Edges[0].Direction = direction;
        var prepared = chart.Prepare(new TopologyRenderOptions { IncludeLegend = false }); var saved = prepared.ToInterchangeEnvelope();
        var edge = saved.Edges[0].Topology!; var xml = XDocument.Parse(prepared.ToSvg());
        var markers = xml.Descendants().Where(item => (string?)item.Attribute("data-cfx-role") == "topology-marker").ToArray(); Assert.Equal(count, markers.Length);
        Assert.All(markers, marker => Assert.Equal(0, Number(marker, "stroke-width")));
        var values = Numbers(edge.PathGeometry!.PathData); var start = new ChartPoint(values[0], values[1]); var end = new ChartPoint(values[6], values[7]);
        var first = new ChartPoint(values[2], values[3]); var second = new ChartPoint(values[4], values[5]); var index = 0;
        if (direction is VisualLinkDirection.Backward or VisualLinkDirection.Bidirectional) AssertTip(markers[index++], first, start, edge.SourceMarkerStyle!.Offset);
        if (direction is VisualLinkDirection.Forward or VisualLinkDirection.Bidirectional) AssertTip(markers[index], second, end, edge.TargetMarkerStyle!.Offset);
        static void AssertTip(XElement marker, ChartPoint adjacent, ChartPoint end, double offset) {
            var p = Numbers(marker.Attribute("d")!.Value); var dx = end.X - adjacent.X; var dy = end.Y - adjacent.Y; var length = Math.Sqrt(dx * dx + dy * dy);
            Assert.Equal(end.X + dx / length * offset, p[0], 2); Assert.Equal(end.Y + dy / length * offset, p[1], 2);
        }
    }

    [Fact]
    public void NullPositionAndStylesKeepOrdinaryBytesAndSeparateLeaderSemantics() {
        var chart = Source(TopologyLabelVerticalAnchor.Center); var edge = chart.Edges[0]; edge.PathGeometry = null;
        edge.LabelPosition = null; edge.SourceMarkerStyle = null; edge.TargetMarkerStyle = null;
        var svg = chart.ToSvg(); var png = chart.ToPng(); var json = chart.ToVisualArtifact().ToInterchangeJson();
        edge.LabelPosition = new TopologyLabelPosition(new ChartPoint(370, 70)); edge.TargetMarkerStyle = new TopologyMarkerStyle();
        edge.LabelPosition = null; edge.TargetMarkerStyle = null;
        Assert.Equal(svg, chart.ToSvg()); Assert.Equal(png, chart.ToPng()); Assert.Equal(json, chart.ToVisualArtifact().ToInterchangeJson());
        Assert.Equal(370, edge.LabelAnchorX); Assert.Equal(100, edge.LabelAnchorY); Assert.True(edge.HasLabelAnchorOverride);
    }

    [Fact]
    public void ContradictoryOffsetsAndInvalidStylesRejectBeforePaint() {
        var chart = Source(); chart.Edges[0].LabelOffsetX = 2; Assert.False(new TopologyChartValidator().Validate(chart).IsValid);
        Assert.Throws<TopologyValidationException>(() => chart.ToSvg());
        Assert.Throws<ArgumentOutOfRangeException>(() => new TopologyLabelPosition(new ChartPoint(1, 2), (TopologyLabelVerticalAnchor)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TopologyMarkerStyle { Scale = 0 });
        Assert.Throws<ArgumentOutOfRangeException>(() => new TopologyMarkerStyle { StrokeWidth = -1 });
        Assert.Throws<ArgumentOutOfRangeException>(() => new TopologyMarkerStyle { Offset = double.NaN });
    }
    private static XElement PaintedLabel(XDocument document) {
        var semantic = Assert.Single(document.Descendants(), item => (string?)item.Attribute("data-cfx-role") == "topology-edge-label-text");
        // The semantic role belongs to its group; x/y are the actual plain SVG text baseline.
        return Assert.Single(semantic.Descendants(), item => item.Name.LocalName == "text");
    }
    private static double Number(XElement item, string name) {
        var value = item.Attribute(name); Assert.NotNull(value);
        return double.Parse(value.Value, CultureInfo.InvariantCulture);
    }
    private static double[] Numbers(string path) => Regex.Matches(path, @"[-+]?(?:\d*\.)?\d+(?:[eE][-+]?\d+)?").Select(match => double.Parse(match.Value, CultureInfo.InvariantCulture)).ToArray();
    private static void Capture(PreparedVisual prepared, string name) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); var path = Path.Combine(output, name); File.WriteAllText(path + ".svg", prepared.ToSvg());
        File.WriteAllBytes(path + ".native.png", prepared.ToPng()); File.WriteAllText(path + ".portable.json", prepared.ToArtifact().ToInterchangeEnvelope().ToJson());
    }
}
