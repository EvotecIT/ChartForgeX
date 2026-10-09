using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyMotionVisualParityTests {
    [Theory]
    [InlineData(420)]
    [InlineData(620)]
    [InlineData(620.375)]
    public void AnimatedMarkerColorStopsMatchNativeSamplesAtResolvedRouteBoundaries(double lastX) {
        var prepared = Diagram(lastX).Prepare(Options());
        var motion = Motion(loop: false);
        var presentation = prepared.WithMotion(motion);
        var animated = XDocument.Parse(presentation.ToSvg());
        var marker = Role(animated, "topology-motion-marker");
        var fill = marker.Elements().Single(e => (string?)e.Attribute("attributeName") == "fill");
        var values = ((string)fill.Attribute("values")!).Split(';');
        var stops = ((string)fill.Attribute("keyTimes")!).Split(';').Select(Number).ToArray();
        Assert.Equal(new[] { "#EF4444", "#22C55E", "#22C55E" }, values);
        var edges = prepared.ToInterchangeEnvelope().Edges;
        double Length(int index) => edges[index].ResolvedRoute.Zip(edges[index].ResolvedRoute.Skip(1), (a, b) =>
            Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y))).Sum();
        Assert.Equal(Length(0) / (Length(0) + Length(1)), stops[1], 10);
        Assert.Equal("discrete", (string?)fill.Attribute("calcMode"));
        foreach (var progress in new[] { 0d, stops[1] - .00001, stops[1], stops[1] + .00001, .75, 1 }) {
            var sample = XDocument.Parse(presentation.Sample(progress).ToSvg());
            Assert.Equal(progress < stops[1] ? values[0] : values[1], (string?)Role(sample, "topology-motion-marker").Attribute("fill"));
        }
        foreach (var role in new[] { "topology-motion-halo", "topology-motion-surface", "topology-motion-outline" }) {
            Assert.Contains(Role(animated, role).Elements(), e => e.Name.LocalName == "animateMotion");
            Assert.NotNull(Role(XDocument.Parse(presentation.Sample(.75).ToSvg()), role));
        }
    }

    [Theory]
    [InlineData(null, "#2563EB")]
    [InlineData("#a855f7", "#A855F7")]
    public void ScenarioAndExplicitMarkerPaintRemainAuthoritativeAcrossEveryRoute(string? markerOverride, string expected) {
        var chart = Diagram(620).AddScenario("tour", "Tour", scenario => scenario.AddEdgeStep("first").AddEdgeStep("second"));
        chart.Scenarios[0].Color = "#2563eb";
        var motion = TopologyMotionOptions.RoutePulseForScenario("tour").WithMarkerColor(markerOverride);
        motion.Loop = false;
        var presentation = chart.WithMotion(motion, Options());
        Assert.Equal(expected, (string?)Role(XDocument.Parse(presentation.ToSvg()), "topology-motion-marker").Attribute("fill"));
        foreach (var progress in new[] { 0d, .5, 1 })
            Assert.Equal(expected, (string?)Role(XDocument.Parse(presentation.Sample(progress).ToSvg()), "topology-motion-marker").Attribute("fill"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EndpointPulsesArePresentOnlyWhenEnabledAndShareTheSvgPhase(bool enabled) {
        var presentation = Diagram(620).WithMotion(Motion(loop: false).WithEndpointPulses(enabled), Options());
        var animated = XDocument.Parse(presentation.ToSvg());
        var svgNodes = animated.Descendants().Where(e => (string?)e.Attribute("data-cfx-role") == "topology-motion-node").ToArray();
        Assert.Equal(enabled ? 3 : 0, svgNodes.Length);
        foreach (var progress in new[] { 0d, .25, .5, .75, 1 }) {
            var sample = XDocument.Parse(presentation.Sample(progress).ToSvg());
            var nativeNodes = sample.Descendants().Where(e => (string?)e.Attribute("data-cfx-role") == "topology-motion-node").ToArray();
            Assert.Equal(svgNodes.Length, nativeNodes.Length);
            for (var i = 0; i < svgNodes.Length; i++) {
                var radii = svgNodes[i].Elements().Single(e => (string?)e.Attribute("attributeName") == "r");
                var values = ((string)radii.Attribute("values")!).Split(';').Select(Number).ToArray();
                var strength = 1 - Math.Abs(2 * progress - 1);
                Assert.Equal(values[0] + (values[1] - values[0]) * strength, Number((string)nativeNodes[i].Attribute("rx")!), 3);
                Assert.True(ChartForgeX.SvgRaster.SvgRasterColor.TryParse((string)nativeNodes[i].Attribute("stroke")!, out var color));
                Assert.Equal((byte)Math.Round((.18 + (.62 - .18) * strength) * 255), color.A);
                Assert.Equal("0;0.5;1", (string?)radii.Attribute("keyTimes"));
                Assert.Equal((string?)svgNodes[i].Attribute("data-node-id"), (string?)nativeNodes[i].Parent!.Attribute("data-node-id"));
            }
        }
        var other = Diagram(620).WithMotion(Motion(loop: false).WithEndpointPulses(!enabled), Options());
        Assert.NotEqual(presentation.Sample(.5).ToPng(), other.Sample(.5).ToPng());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DashSamplesAndCompletedOrLoopingSeamsPreserveDetachedBaseScene(bool loop) {
        var chart = Diagram(620); var motion = Motion(loop);
        var prepared = chart.Prepare(Options()); var original = prepared.ToSvg();
        var presentation = prepared.WithMotion(motion);
        var animated = presentation.ToSvg();
        var start = XDocument.Parse(presentation.Sample(0).ToSvg());
        var middle = XDocument.Parse(presentation.Sample(.75).ToSvg());
        var end = XDocument.Parse(presentation.Sample(1).ToSvg());
        var display = presentation.Sample(.75).Scene.Nodes.ToList();
        var routeIndex = display.FindIndex(node => node.Role == "topology-motion-route");
        var nodeIndex = display.FindIndex(node => node.Role == "topology-node");
        Assert.True(routeIndex >= 0 && routeIndex < nodeIndex, "Motion route ink must stay below topology node surfaces.");
        var startRoute = start.Descendants().First(e => (string?)e.Attribute("data-cfx-role") == "topology-motion-route");
        var middleRoute = middle.Descendants().First(e => (string?)e.Attribute("data-cfx-role") == "topology-motion-route");
        Assert.NotEqual((string?)startRoute.Attribute("d"), (string?)middleRoute.Attribute("d"));
        var points = ChartMapPathParser.ParseSubpaths((string)middleRoute.Attribute("d")!, 1).First().Points;
        Assert.Equal(3.5, Math.Abs(points.Last().X - points.First().X), 3); // 10-unit dash minus the 6.5-unit SVG offset.
        Assert.Equal(loop ? "#EF4444" : "#22C55E", (string?)Role(end, "topology-motion-marker").Attribute("fill"));
        if (loop) Assert.Equal(presentation.Sample(0).ToPng(), presentation.Sample(1).ToPng());
        else Assert.NotEqual(presentation.Sample(0).ToPng(), presentation.Sample(1).ToPng());
        Assert.All(XDocument.Parse(animated).Descendants().Where(e => e.Name.LocalName is "animate" or "animateMotion"), e => {
            Assert.Equal(loop ? "indefinite" : "1", (string?)e.Attribute("repeatCount"));
            Assert.Equal(loop ? "remove" : "freeze", (string?)e.Attribute("fill"));
        });
        motion.MarkerColor = "#000000"; motion.PulseRouteEndpoints = !motion.PulseRouteEndpoints; chart.Nodes.Clear();
        Assert.Equal(animated, presentation.ToSvg());
        Assert.Equal(original, prepared.ToSvg());
        Assert.Equal(original, presentation.StaticVisual.ToSvg());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LongAuthoredRoutesRejectExcessiveNativeDashGeometryBeforeProducingCommands(bool fit) {
        var chart = TopologyChart.Create().WithViewport(640, 240, 20).WithLayout(TopologyLayoutMode.Manual).WithLegend(null)
            .AddNode("a", "Near", 20, 80, width: 100, height: 60)
            .AddNode("b", "Far", 400, 80, width: 100, height: 60)
            .AddEdge("long", "a", "b", routing: TopologyEdgeRouting.Straight, color: "#EF4444");
        chart.Edges[0].Waypoints.Add(new ChartPoint(1e12, 110));
        var options = Options(); options.FitContentToViewport = fit;
        var prepared = chart.Prepare(options);
        if (fit) Assert.Equal(640, prepared.Width);
        else {
            Assert.True(prepared.Width > 1e12, "Natural preparation reserves the complete authored route.");
            Assert.ThrowsAny<ArgumentException>(() => prepared.ToPng());
        }
        var presentation = prepared.WithMotion(TopologyMotionOptions.RoutePulseForEdges("long").WithEndpointPulses(false));
        var error = Assert.Throws<InvalidOperationException>(() => presentation.Sample(.25));
        Assert.Contains("visible route geometry budget", error.Message);
    }

    private static TopologyChart Diagram(double lastX) => TopologyChart.Create().WithViewport(800, 240, 20)
        .WithLayout(TopologyLayoutMode.Manual).WithLegend(null)
        .AddNode("a", "API", 20, 80, width: 100, height: 60)
        .AddNode("b", "Worker", 220, 80, width: 100, height: 60)
        .AddNode("c", "Data", lastX, 80, width: 100, height: 60)
        .AddEdge("first", "a", "b", routing: TopologyEdgeRouting.Straight, color: "#EF4444")
        .AddEdge("second", "b", "c", routing: TopologyEdgeRouting.Straight, color: "#22C55E");
    private static TopologyRenderOptions Options() => new() { IncludeLegend = false, PngSupersamplingScale = 1 };
    private static TopologyMotionOptions Motion(bool loop) {
        var motion = TopologyMotionOptions.RoutePulseForEdges("first", "second").WithDuration(2).WithFrameRate(10);
        motion.Loop = loop;
        return motion;
    }
    private static XElement Role(XDocument document, string role) => document.Descendants().Single(e => (string?)e.Attribute("data-cfx-role") == role);
    private static double Number(string value) => double.Parse(value, CultureInfo.InvariantCulture);
}
