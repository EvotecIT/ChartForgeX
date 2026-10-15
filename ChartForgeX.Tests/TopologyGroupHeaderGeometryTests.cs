using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyGroupHeaderGeometryTests {
    [Theory]
    [InlineData("Arial")]
    [InlineData("Missing CFX Group Header Font")]
    [InlineData("CFX Group Header Carlito")]
    public void SourceSizedGroupCaptionsRemainVisibleOutsideTheirNodes(string family) {
        if (family == "CFX Group Header Carlito") {
            var fonts = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito");
            FontRegistry.Register(family, Path.Combine(fonts, "Carlito-Regular.ttf"), 400);
            FontRegistry.Register(family, Path.Combine(fonts, "Carlito-Bold.ttf"), 700);
        }
        var result = MermaidRenderer.Render("%%{init: {'fontFamily':'" + family + "','themeVariables':{'fontSize':256}}}%%\nflowchart LR\nsubgraph Service[Service group]\n A --> B\nend\n");
        Assert.Empty(result.Diagnostics);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        chart.Groups[0].Subtitle = "Group description";
        var authored = chart.Nodes.Select(node => (node.Id, node.X, node.Y, node.Width, node.Height)).ToArray();
        var prepared = chart.Prepare();
        AssertHeaderContainsPaintAndAvoidsNodes(prepared);
        Assert.Equal(authored, chart.Nodes.Select(node => (node.Id, node.X, node.Y, node.Width, node.Height)).ToArray());
        var image = PngReader.Decode(prepared.ToPng());
        Assert.Equal((int)Math.Ceiling(prepared.Width), image.Width);
        Assert.Equal((int)Math.Ceiling(prepared.Height), image.Height);
        var xml = XDocument.Parse(prepared.ToSvg());
        foreach (var caption in Captions(prepared)) {
            var element = Assert.Single(xml.Descendants(), item => (string?)item.Attribute("data-cfx-role") == caption.Role);
            Assert.Equal(Assert.Single(caption.Text.Lines).Text, element.Value);
            Assert.True(HasNativeInk(image, caption), "The group caption must contribute native raster ink.");
        }
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void HiddenGroupHeadersDoNotMoveAuthoredManualNodes(bool groups, bool labels) {
        var chart = ManualGroup();
        var options = new TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false, IncludeGroups = groups, IncludeGroupLabels = labels };
        var prepared = chart.Prepare(options);
        var node = Assert.Single(prepared.ToInterchangeEnvelope().Nodes);
        Assert.Equal(chart.Nodes[0].X, node.X);
        Assert.Equal(chart.Nodes[0].Y, node.Y);
        Assert.Null(Assert.Single(prepared.Analyze().Groups).HeaderBounds);
        Assert.Empty(Captions(prepared));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void SharedHeaderAllocationContainsThePaintedSymbolAndStatusOptions(bool symbol, bool status) {
        var chart = ManualGroup();
        if (symbol) chart.Groups[0].Symbol = "S";
        var prepared = chart.Prepare(new TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false, IncludeGroupStatusDots = status });
        AssertHeaderContainsPaintAndAvoidsNodes(prepared);
        var header = Assert.Single(prepared.Analyze().Groups).HeaderBounds!.Value;
        var dots = prepared.Visual.Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "topology-group-status").ToArray();
        Assert.Equal(status ? 1 : 0, dots.Length);
        foreach (var dot in dots) AssertContained(header, new ChartRect(dot.Cx - dot.Rx, dot.Cy - dot.Ry, dot.Rx * 2, dot.Ry * 2));
        Assert.Equal(symbol, prepared.Visual.Scene.Nodes.Any(node => node.Role == "topology-node-icon"));
        Assert.Equal(49, chart.Nodes[0].Y);
    }

    [Fact]
    public void RoutesCanUseUnpaintedSpaceBetweenCaptionAndStatusDot() {
        var chart = TopologyChart.Create().WithViewport(400, 300, 16).WithLegend(null)
            .AddGroup("service", "Service group", 32, 40, 300, 180)
            .AddNode("a", "Source", 180, 40, width: 20, height: 40)
            .AddNode("b", "Target", 240, 40, width: 20, height: 40)
            .AddEdge("link", "a", "b", routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal);
        var prepared = chart.Prepare(new TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false, IncludeGroupStatusDots = true });
        var report = prepared.Analyze();
        var edge = Assert.Single(report.Edges);
        Assert.Equal(2, edge.Points.Count);
        Assert.True(edge.SourceAttached && edge.TargetAttached);
        Assert.DoesNotContain(report.RouteCrossings, item => item.Kind == TopologyLayoutRouteCrossingKind.GroupHeader);
        var header = Assert.Single(report.Groups).HeaderBounds!.Value;
        Assert.All(edge.Points, point => Assert.True(point.X > header.Left && point.X < header.Right && point.Y > header.Top && point.Y < header.Bottom));
    }

    private static TopologyChart ManualGroup() => TopologyChart.Create().WithViewport(400, 300, 16).WithLegend(null)
        .AddGroup("service", "Service group", 32, 40, 300, 180, subtitle: "Group description")
        .AddNode("A", "A", 64, 49, TopologyNodeKind.Generic, groupId: "service", width: 80, height: 40);

    private static VisualSceneText[] Captions(PreparedTopology prepared) => prepared.Visual.Scene.Nodes.OfType<VisualSceneText>()
        .Where(node => node.Role is "topology-group-label" or "topology-group-subtitle").ToArray();

    private static void AssertHeaderContainsPaintAndAvoidsNodes(PreparedTopology prepared) {
        var header = Assert.Single(prepared.Analyze().Groups).HeaderBounds!.Value;
        var captions = Captions(prepared);
        Assert.Equal(2, captions.Length);
        Assert.Equal(new[] { "Service group", "Group description" }, captions.Select(node => Assert.Single(node.Text.Lines).Text));
        foreach (var caption in captions) {
            var bounds = new ChartRect(caption.X, caption.Baseline - caption.Text.Ascent, caption.Text.Metrics.Width, caption.Text.Metrics.Height);
            AssertContained(header, bounds);
            Assert.All(prepared.ToInterchangeEnvelope().Nodes, node =>
                Assert.False(bounds.Left < node.X + node.Width && bounds.Right > node.X && bounds.Top < node.Y + node.Height && bounds.Bottom > node.Y,
                    "A rendered node covers the painted group caption."));
        }
    }

    private static void AssertContained(ChartRect outer, ChartRect inner) => Assert.True(
        inner.Left >= outer.Left - .01 && inner.Right <= outer.Right + .01 && inner.Top >= outer.Top - .01 && inner.Bottom <= outer.Bottom + .01);

    private static bool HasNativeInk(RgbaImage image, VisualSceneText caption) {
        var color = caption.Color;
        var left = Math.Max(0, (int)Math.Ceiling(caption.X));
        var right = Math.Min(image.Width, (int)Math.Floor(caption.X + caption.Text.Metrics.Width));
        var top = Math.Max(0, (int)Math.Ceiling(caption.Baseline - caption.Text.Ascent));
        var bottom = Math.Min(image.Height, (int)Math.Floor(caption.Baseline - caption.Text.Ascent + caption.Text.Metrics.Height));
        for (var y = top; y < bottom; y++) for (var x = left; x < right; x++) {
            var offset = (y * image.Width + x) * 4;
            if (image.Pixels[offset] == color.R && image.Pixels[offset + 1] == color.G && image.Pixels[offset + 2] == color.B && image.Pixels[offset + 3] == color.A) return true;
        }
        return false;
    }
}
