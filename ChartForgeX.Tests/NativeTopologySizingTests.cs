using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeTopologySizingTests {
    [Fact]
    public void NaturalCanvasRemeasuresHeadingAfterItsContentExpandsAnInitiallyNarrowFrame() {
        var chart = TopologyChart.Create().WithId("narrow-frame").WithTitle("Narrow").WithViewport(120, 180, 80)
            .AddNode("root", "Root", 0, 0, TopologyNodeKind.Hub, width: 64, height: 42);
        var prepared = chart.Prepare(new TopologyRenderOptions { HeaderStyle = TopologyHeaderStyle.CenterBanner, IncludeLegend = false });
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.Contains(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "frame-heading");
        var node = Assert.Single(prepared.ToInterchangeEnvelope().Nodes);
        Assert.True(node.X >= 0 && node.Y >= 0 && node.X + node.Width <= prepared.Width && node.Y + node.Height <= prepared.Height);
        Assert.True(prepared.Width > 120 && prepared.Height >= 180);
        Assert.Equal(120, chart.Viewport.Width); Assert.Equal(180, chart.Viewport.Height); Assert.Equal(80, chart.Viewport.Padding);
        Assert.Equal(0, chart.Nodes[0].X); Assert.Equal(0, chart.Nodes[0].Y);
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void ConvenienceExportReservesPaddingOnceAndKeepsAuthoredCanvasWhenContentFits() {
        var chart = Diagram(640, 360, 480);
        var prepared = chart.Prepare();
        Assert.Equal(640, prepared.Width); Assert.Equal(360, prepared.Height);
        Assert.Equal(24, prepared.ToInterchangeEnvelope().Nodes[0].X);
        Assert.Equal(480, prepared.ToInterchangeEnvelope().Nodes[1].X);
        Assert.Equal(prepared.ToSvg(), chart.ToSvg());
        AssertRoutePixels(prepared);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NaturalAndFittedConvenienceExportsShareResolvedEndpointsWithoutMutatingSource(bool fit) {
        var chart = Diagram(320, 180, 420);
        var options = chart.DefaultRenderOptions!.Clone(); options.FitContentToViewport = fit;
        var prepared = chart.Prepare(options);
        if (fit) { Assert.Equal(320, prepared.Width); Assert.Equal(180, prepared.Height); }
        else Assert.True(prepared.Width > 320);
        var image = PngReader.Decode(prepared.ToPng());
        Assert.Equal((int)Math.Ceiling(prepared.Width), image.Width); Assert.Equal((int)Math.Ceiling(prepared.Height), image.Height);
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.Equal(prepared.Width.ToString(System.Globalization.CultureInfo.InvariantCulture), (string?)svg.Root!.Attribute("width"));
        var envelope = prepared.ToInterchangeEnvelope();
        Assert.Equal(prepared.Width, envelope.Width); Assert.Equal(prepared.Height, envelope.Height);
        AssertRoutePixels(prepared);
        Assert.Equal(320, chart.Viewport.Width); Assert.Equal(180, chart.Viewport.Height);
        Assert.Equal(24, chart.Nodes[0].X); Assert.Equal(420, chart.Nodes[1].X);
        Assert.Empty(chart.Edges[0].Waypoints); Assert.False(chart.DefaultRenderOptions.FitContentToViewport);
    }

    [Fact]
    public void ExplicitCommonSizeRetainsItsFixedViewportConstraint() {
        var chart = Diagram(320, 180, 420);
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(320, 180), 24), frame: new VisualFrame(showLegend: false));
        Assert.Throws<NotSupportedException>(() => chart.Prepare(context));
        var options = chart.DefaultRenderOptions!.Clone(); options.FitContentToViewport = true;
        var visual = chart.Prepare(context, options);
        Assert.Equal(320, visual.Size.Width); Assert.Equal(180, visual.Size.Height);
    }

    private static void AssertRoutePixels(PreparedTopology prepared) {
        var edge = Assert.Single(prepared.ToInterchangeEnvelope().Edges);
        var route = edge.ResolvedRoute;
        var endpoint = route.Last(); var previous = route[route.Count - 2];
        var dx = endpoint.X - previous.X; var dy = endpoint.Y - previous.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        var inset = Math.Min(4, length / 3);
        var x = (int)Math.Round(endpoint.X - dx / length * inset);
        var y = (int)Math.Round(endpoint.Y - dy / length * inset);
        var image = prepared.Visual.ToRgba(new VisualRenderOptions(supersampling: 1));
        Assert.InRange(x, 0, image.Width - 1); Assert.InRange(y, 0, image.Height - 1);
        var offset = (y * image.Width + x) * 4;
        Assert.True(image.Pixels[offset] > 200 && image.Pixels[offset + 1] < 80 && image.Pixels[offset + 2] < 80,
            "The native raster route must reach the endpoint projected by semantic interchange.");
    }

    private static TopologyChart Diagram(double width, double height, double targetX) {
        var chart = TopologyChart.Create().WithId("sizing").WithViewport(width, height, 24);
        chart.Nodes.Add(new TopologyNode { Id = "source", Label = "Source", X = 24, Y = 64, Width = 120, Height = 64, PreserveDisplayModeSize = true });
        chart.Nodes.Add(new TopologyNode { Id = "target", Label = "Target", X = targetX, Y = 64, Width = 120, Height = 64, PreserveDisplayModeSize = true });
        chart.Edges.Add(new TopologyEdge { Id = "route", SourceNodeId = "source", TargetNodeId = "target", Routing = TopologyEdgeRouting.Straight,
            Color = "#ff0000", StrokeWidth = 4, SourceMarker = TopologyMarkerKind.None, TargetMarker = TopologyMarkerKind.None });
        chart.WithRenderOptions(new TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false, IncludeNodeLabels = false, IncludeStatusBadges = false });
        return chart;
    }
}
