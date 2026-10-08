using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.Primitives;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class PreparedTopologyDiagnosticSnapshotTests {
    [Theory]
    [InlineData(false, TopologyEdgeRouting.ObstacleAvoidingOrthogonal)]
    [InlineData(true, TopologyEdgeRouting.ObstacleAvoidingOrthogonal)]
    [InlineData(false, TopologyEdgeRouting.Curved)]
    [InlineData(true, TopologyEdgeRouting.Curved)]
    public void AnalysisUsesRenderedFrameAndFitCoordinates(bool fit, TopologyEdgeRouting routing) {
        var chart = TopologyChart.Create().WithViewport(860, 440, 24).WithTitle("Measured topology frame")
            .WithLegend(TopologyLegend.Default().AddNodeKind("Server", TopologyNodeKind.Server, symbol: "S"))
            .AddGroup("group", "Services", 50, 80, 700, 240)
            .AddNode("source", "Source caption", 90, 140, groupId: "group", width: 60, height: 40)
            .AddNode("target", "Target caption", 640, 140, groupId: "group", width: 60, height: 40)
            .AddNode("wall", "Wall", 380, 140, groupId: "group", width: 60, height: 100)
            .AddNodePort("source", "out", TopologyEdgePort.Right, .3)
            .AddEdge("route", "source", "target", routing: routing)
            .WithEdgeNamedPorts("route", "out", null);
        var options = new TopologyRenderOptions { ReadableDenseLayout = true, NodeDisplayMode = TopologyNodeDisplayMode.Tile,
            EdgeCornerStyle = TopologyEdgeCornerStyle.Rounded };
        var prepared = chart.Prepare(options);
        if (fit) prepared = prepared.WithOutputSize(430, 300);
        var report = prepared.Analyze();
        var envelope = prepared.ToInterchangeEnvelope();
        Assert.Equal(prepared.Width, report.Width);
        Assert.Equal(prepared.Height, report.Height);
        foreach (var node in report.Nodes) {
            var rendered = envelope.Nodes.Single(item => item.Id == node.Id);
            Assert.Equal(new ChartRect(rendered.X!.Value, rendered.Y!.Value, rendered.Width!.Value, rendered.Height!.Value), node.Bounds);
            Assert.NotNull(node.CaptionBounds);
            Assert.True(node.CaptionBounds!.Value.Top >= node.Bounds.Bottom);
        }
        var group = Assert.Single(report.Groups);
        var renderedGroup = Assert.Single(envelope.Groups);
        Assert.Equal(new ChartRect(renderedGroup.X!.Value, renderedGroup.Y!.Value, renderedGroup.Width!.Value, renderedGroup.Height!.Value), group.Bounds);
        Assert.NotNull(group.HeaderBounds);
        Assert.True(group.HeaderBounds!.Value.Top >= group.Bounds.Top);
        var source = report.Nodes.Single(node => node.Id == "source");
        var port = Assert.Single(source.Ports);
        Assert.Equal(source.Bounds.Right, port.Position.X, 6);
        Assert.Equal(source.Bounds.Top + source.Bounds.Height * .3, port.Position.Y, 6);
        var route = Assert.Single(report.Edges);
        var renderedRoute = Assert.Single(envelope.Edges).ResolvedRoute;
        Assert.Equal(renderedRoute.Count, route.Points.Count);
        for (var index = 0; index < route.Points.Count; index++) {
            Assert.Equal(renderedRoute[index].X, route.Points[index].X, 6);
            Assert.Equal(renderedRoute[index].Y, route.Points[index].Y, 6);
        }
        if (fit) Assert.True(source.Bounds.Width < chart.Nodes[0].Width);
        Assert.Equal(90, chart.Nodes[0].X);
        Assert.Equal(140, chart.Nodes[0].Y);
    }

    [Theory]
    [InlineData(TextMeasurementMode.PortableEstimate)]
    [InlineData(TextMeasurementMode.InstalledFonts)]
    public void DetachedAnalysisRetainsDenseRoutingAndMeasurementWithoutReservingTheFrameAgain(TextMeasurementMode mode) {
        var chart = DenseRouteFixture.Small();
        chart.Accessibility.Language = "pl";
        var title = chart.Title;
        var options = DenseRouteFixture.Options();
        options.TextMeasurementMode = mode;
        var prepared = chart.Prepare(options);
        var svg = prepared.ToSvg();
        var before = prepared.Analyze();
        Assert.All(before.Edges, edge => Assert.Equal(TopologyEdgeRouter.PlannedCorridor, edge.Corridor));
        Assert.Equal(title, prepared.Title);
        Assert.Equal("pl", prepared.Language);
        Assert.Equal(title, prepared.ToInterchangeEnvelope().Title);

        chart.Title = "Changed source";
        chart.Nodes.Clear(); chart.Edges.Clear();
        options.ReadableDenseLayout = false;
        options.TextMeasurementMode = mode == TextMeasurementMode.PortableEstimate ? TextMeasurementMode.InstalledFonts : TextMeasurementMode.PortableEstimate;
        TopologyDenseRoutePlanner.ClearPlanCache();
        var after = prepared.Analyze();
        Assert.Equal(before.Edges.Select(edge => edge.Corridor), after.Edges.Select(edge => edge.Corridor));
        Assert.Equal(before.Edges.SelectMany(edge => edge.Points), after.Edges.SelectMany(edge => edge.Points));
        Assert.Equal(before.Nodes.Select(node => node.CaptionBounds), after.Nodes.Select(node => node.CaptionBounds));
        Assert.Equal(svg, prepared.ToSvg());
        Assert.Equal(title, prepared.Title);
    }
}
