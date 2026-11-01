using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyCompartmentGeometryTests {
    [Theory]
    [InlineData(TopologyNodeShape.Diamond)]
    [InlineData(TopologyNodeShape.Circle)]
    [InlineData(TopologyNodeShape.Rectangle)]
    [InlineData(TopologyNodeShape.Rounded)]
    public void NativeSectionsStayInsideTheirContourWithoutAnUnrelatedFittingOverride(TopologyNodeShape shape) {
        var chart = TopologyChart.Create().WithViewport(420, 360, 20);
        var node = new TopologyNode { Id = "type", Label = "Order", X = 60, Y = 60, Width = 240, Height = 220,
            Shape = shape, ShowStatusBadge = false, PreserveDisplayModeSize = true };
        node.Details.Add(new TopologyNodeDetail { Text = "+string Id" });
        node.Details.Add(new TopologyNodeDetail { Text = "+Save()", StartsSection = true });
        chart.Nodes.Add(node);
        var prepared = chart.Prepare(Context(420, 360), new TopologyRenderOptions { IncludeLegend = false, FitContentToViewport = false });
        ScatterBubbleColorRenderingTests.Capture(prepared, "native-sections-" + shape);
        var surface = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), item => item.Role == "topology-node-surface" && item.Fill.HasValue);
        var outline = new LabelMarkShape(VisualSceneGeometry.Flatten(surface, 32), true, 0);
        var lines = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(item => item.Role == "topology-node-detail-separator").ToArray();
        Assert.Equal(2, lines.Length);
        foreach (var line in lines) {
            // A thin interior strip observes both ends without treating boundary antialiasing as overflow.
            Assert.True(outline.Contains(new ChartRect(line.Start.X + .1, line.Start.Y - .01, line.End.X - line.Start.X - .2, .02)));
        }
    }

    [Theory]
    [InlineData(TopologyEdgePort.Top, TopologyEdgePort.Auto, 220, 50)]
    [InlineData(TopologyEdgePort.Top, TopologyEdgePort.Auto, 120, 50)]
    [InlineData(TopologyEdgePort.Bottom, TopologyEdgePort.Right, 220, 50)]
    [InlineData(TopologyEdgePort.Left, TopologyEdgePort.Top, 220, 50)]
    [InlineData(TopologyEdgePort.Right, TopologyEdgePort.Bottom, 220, 50)]
    [InlineData(TopologyEdgePort.Top, TopologyEdgePort.Auto, 345, 300)]
    public void CurvedRoutesLeaveAndApproachEachAuthoredSideAlongItsOwnOutwardNormal(TopologyEdgePort sourcePort, TopologyEdgePort targetPort, double targetY, double sourceHeight) {
        var chart = TopologyChart.Create().WithViewport(700, 500, 20);
        chart.Nodes.Add(new TopologyNode { Id = "source", Label = "Source", X = 100, Y = 120, Width = 100, Height = sourceHeight, Shape = TopologyNodeShape.Rectangle });
        chart.Nodes.Add(new TopologyNode { Id = "target", Label = "Target", X = 350, Y = targetY, Width = 100, Height = 50, Shape = TopologyNodeShape.Rectangle });
        chart.AddEdge("route", "source", "target", routing: TopologyEdgeRouting.Curved);
        chart.Edges[0].SourcePort = sourcePort; chart.Edges[0].TargetPort = targetPort;
        var prepared = chart.Prepare(Context(700, 500), new TopologyRenderOptions { IncludeLegend = false, FitContentToViewport = false });
        ScatterBubbleColorRenderingTests.Capture(prepared, "curve-sides-" + sourcePort + "-" + targetPort + "-" + targetY + "-" + sourceHeight);
        var stroke = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), item => item.Role == "topology-edge-line");
        var points = Assert.Single(VisualSceneGeometry.Flatten(stroke, 32));
        Assert.True(points.Count > 2);
        Assert.True(Outward(points[0], points[1], sourcePort) > Crosswise(points[0], points[1], sourcePort));
        var arrival = targetPort == TopologyEdgePort.Auto ? TopologyEdgePort.Left : targetPort;
        Assert.True(Outward(points[^1], points[^2], arrival) > Crosswise(points[^1], points[^2], arrival));
    }

    private static VisualRenderContext Context(double width, double height) => new(
        new VisualLayoutOptions(new VisualSize(width, height), 20), frame: new VisualFrame(showLegend: false),
        font: new FontSpec { Family = BrowserProofFonts.Family });

    private static double Outward(ChartPoint endpoint, ChartPoint adjacent, TopologyEdgePort side) => side switch {
        TopologyEdgePort.Left => endpoint.X - adjacent.X,
        TopologyEdgePort.Right => adjacent.X - endpoint.X,
        TopologyEdgePort.Top => endpoint.Y - adjacent.Y,
        _ => adjacent.Y - endpoint.Y
    };

    private static double Crosswise(ChartPoint endpoint, ChartPoint adjacent, TopologyEdgePort side) =>
        side is TopologyEdgePort.Left or TopologyEdgePort.Right ? Math.Abs(adjacent.Y - endpoint.Y) : Math.Abs(adjacent.X - endpoint.X);
}
