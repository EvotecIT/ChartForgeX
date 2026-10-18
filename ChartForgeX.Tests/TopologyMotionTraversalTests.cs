using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects scenario traversal direction across animated SVG, sampled frames and detached routes.</summary>
public sealed class TopologyMotionTraversalTests {
    [Theory]
    [InlineData("a", "b", false)]
    [InlineData("b", "a", true)]
    [InlineData("b", null, true)]
    [InlineData(null, "a", true)]
    [InlineData("a", null, false)]
    [InlineData(null, "b", false)]
    [InlineData(null, null, false)]
    [InlineData("c", "a", false)]
    [InlineData("b", "b", false)]
    [InlineData("a", "a", false)]
    public void AdjacentNodeHintsAgreeBeforeReversingTheDetachedRoute(string? previous, string? following, bool reverse) {
        var prepared = Chart(previous, following).Prepare(Options());
        var original = prepared.ToSvg();
        var authored = AuthoredRoute(prepared);
        var presentation = prepared.WithMotion(Motion());
        AssertRoute(reverse ? Enumerable.Reverse(authored).ToArray() : authored, presentation);
        Assert.Equal(original, prepared.ToSvg());
        Assert.Equal(authored, AuthoredRoute(prepared));
        // A second presentation from the same preparation must retain its authored route.
        AssertRoute(authored, prepared.WithMotion(ExplicitMotion()));
    }

    [Fact]
    public void ExplicitEdgesKeepAuthoredOrderEvenWhenAReverseScenarioIsSelected() {
        var prepared = Chart("b", "a").Prepare(Options());
        var motion = ExplicitMotion(); motion.ScenarioId = "traversal";
        AssertRoute(AuthoredRoute(prepared), prepared.WithMotion(motion));
    }

    [Fact]
    public void SelfLoopsRetainTheirAuthoredPortsAndWaypointOrder() {
        var chart = Chart("a", "a");
        var edge = chart.Edges[0]; edge.TargetNodeId = "a";
        edge.SourcePort = TopologyEdgePort.Right; edge.TargetPort = TopologyEdgePort.Bottom;
        edge.Waypoints.AddRange(new[] { new ChartPoint(240, 110), new ChartPoint(240, 180), new ChartPoint(60, 180) });
        var prepared = chart.Prepare(Options());
        AssertRoute(AuthoredRoute(prepared), prepared.WithMotion(Motion()));
    }

    [Fact]
    public void ReverseScenarioMovesTheNativePngMarkerInTheSameDirectionAsSvg() {
        var prepared = Chart("b", "a").Prepare(Options());
        var authored = AuthoredRoute(prepared);
        var presentation = prepared.WithMotion(Motion());
        foreach (var progress in new[] { .25, .75 }) {
            var expected = new ChartPoint(authored[^1].X + (authored[0].X - authored[^1].X) * progress,
                authored[^1].Y + (authored[0].Y - authored[^1].Y) * progress);
            var sampled = presentation.Sample(progress);
            AssertMarkerPosition(expected, XDocument.Parse(sampled.ToSvg()));
            var image = RasterImageDecoder.Decode(sampled.ToPng());
            var count = 0; var x = 0d; var y = 0d;
            for (var row = 0; row < image.Height; row++) for (var column = 0; column < image.Width; column++) {
                var offset = (row * image.Width + column) * 4;
                if (image.Pixels[offset] > 220 && image.Pixels[offset + 1] < 50 && image.Pixels[offset + 2] > 180) {
                    count++; x += column + .5; y += row + .5;
                }
            }
            Assert.True(count > 20, "The native frame needs the visible opaque route marker.");
            Assert.InRange(Math.Abs(x / count - expected.X), 0, 1);
            Assert.InRange(Math.Abs(y / count - expected.Y), 0, 1);
        }
    }

    private static TopologyChart Chart(string? previous, string? following) {
        var chart = TopologyChart.Create().WithViewport(520, 300, 20).WithLayout(TopologyLayoutMode.Manual).WithLegend(null)
            .AddNode("a", "First", 20, 80, width: 80, height: 60)
            .AddNode("b", "Second", 340, 80, width: 80, height: 60)
            .AddNode("c", "Other", 200, 210, width: 80, height: 60)
            .AddEdge("route", "a", "b", routing: TopologyEdgeRouting.Straight, color: "#64748B");
        chart.Edges[0].Direction = VisualLinkDirection.Bidirectional;
        chart.AddScenario("traversal", "Traversal", scenario => {
            if (previous != null) scenario.AddNodeStep(previous);
            scenario.AddEdgeStep("route");
            if (following != null) scenario.AddNodeStep(following);
        });
        return chart;
    }

    private static ChartPoint[] AuthoredRoute(PreparedTopology prepared) => prepared.ToInterchangeEnvelope().Edges.Single()
        .ResolvedRoute.Select(point => new ChartPoint(point.X, point.Y)).ToArray();
    private static TopologyRenderOptions Options() => new() { IncludeLegend = false, PngSupersamplingScale = 1 };
    private static TopologyMotionOptions Motion() => NonLooping(TopologyMotionOptions.RoutePulseForScenario("traversal"));
    private static TopologyMotionOptions ExplicitMotion() => NonLooping(TopologyMotionOptions.RoutePulseForEdges("route"));
    private static TopologyMotionOptions NonLooping(TopologyMotionOptions motion) {
        motion.Loop = false;
        return motion.WithMarker(6, "#ED17D6").WithEndpointPulses(false);
    }

    private static void AssertRoute(ChartPoint[] expected, TopologyMotionPresentation presentation) {
        var document = XDocument.Parse(presentation.ToSvg());
        var route = document.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "topology-motion-route");
        var paths = ChartMapPathParser.ParseSubpaths((string)route.Attribute("d")!, 1);
        var actual = Assert.Single(paths).Points;
        Assert.Equal(expected.Length, actual.Count);
        for (var index = 0; index < expected.Length; index++) {
            Assert.Equal(expected[index].X, actual[index].X, 3);
            Assert.Equal(expected[index].Y, actual[index].Y, 3);
        }
        AssertMarkerPosition(expected[0], XDocument.Parse(presentation.Sample(0).ToSvg()));
        AssertMarkerPosition(expected[^1], XDocument.Parse(presentation.Sample(1).ToSvg()));
    }

    private static void AssertMarkerPosition(ChartPoint expected, XDocument document) {
        var marker = document.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "topology-motion-marker");
        Assert.Equal(expected.X, double.Parse((string)marker.Attribute("cx")!, CultureInfo.InvariantCulture), 3);
        Assert.Equal(expected.Y, double.Parse((string)marker.Attribute("cy")!, CultureInfo.InvariantCulture), 3);
    }
}
