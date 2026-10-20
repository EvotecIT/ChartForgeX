using System.Text.Json;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects obstacle-router automatic attachments independently of direct orthogonal fan policy.</summary>
public sealed class TopologyObstacleAutomaticPortTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AutomaticCardRoutesKeepSeparateArrowsAndNearbyRelationshipCaptions(bool mirrored) {
        var chart = MiniMap();
        if (mirrored) foreach (var node in chart.Nodes) node.X = chart.Viewport.Width - node.X - node.Width;
        var prepared = chart.Prepare(Options());
        var report = prepared.Analyze();
        Capture(prepared, "obstacle-auto-mini" + (mirrored ? "-mirrored" : ""));
        Assert.Empty(report.Collisions);
        Assert.Empty(report.RouteCrossings);
        Assert.Empty(report.RouteOverlaps);
        Assert.All(report.Edges, edge => {
            Assert.True(edge.SourceAttached && edge.TargetAttached, edge.Id);
            Assert.Equal(0, edge.ObstacleHits);
        });
        Separate(report, "dns-domain", "ip-domain", source: false);
        Separate(report, "domain-server", "finding-domain", source: true);
        foreach (var id in new[] { "ip-domain", "finding-domain" }) {
            var label = report.EdgeLabels.Single(item => item.EdgeId == id);
            Assert.InRange(label.RouteDistance, 0, 35);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ObstacleCandidatesKeepExplicitSidesAndNamedCrossAxisPositions(bool named) {
        var chart = MiniMap();
        foreach (var edge in chart.Edges) chart.WithEdgePorts(edge.Id, TopologyEdgePort.Right, TopologyEdgePort.Left);
        if (named) {
            chart.AddNodePort("domain", "verified", TopologyEdgePort.Left, .25)
                .AddNodePort("domain", "observed", TopologyEdgePort.Left, .75);
            chart.Edges.Single(edge => edge.Id == "dns-domain").TargetPortId = "verified";
            chart.Edges.Single(edge => edge.Id == "ip-domain").TargetPortId = "observed";
        }
        var prepared = chart.Prepare(Options());
        var report = prepared.Analyze();
        Capture(prepared, "obstacle-authored-mini-" + (named ? "named" : "sides"));
        var domain = report.Nodes.Single(node => node.Id == "domain").Bounds;
        foreach (var id in new[] { "dns-domain", "ip-domain" }) {
            var edge = report.Edges.Single(item => item.Id == id);
            Assert.True(edge.SourceAttached && edge.TargetAttached, id);
            Assert.Equal(domain.Left - 7, edge.Points[^1].X, 6);
            if (named) Assert.Equal(domain.Top + (domain.Bottom - domain.Top) * (id == "dns-domain" ? .25 : .75), edge.Points[^1].Y, 6);
        }
        Separate(report, "dns-domain", "ip-domain", source: false, minimum: 10);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public void WideAndTallAutomaticCandidatesAndRoutedEndsKeepPhysicalNormals(bool horizontal, bool reversed) {
        var chart = TopologyChart.Create().WithViewport(850, 850).WithLegend(null)
            .AddNode("source", "S", horizontal && reversed ? 600 : 0, !horizontal && reversed ? 600 : 0,
                width: horizontal ? 200 : 50, height: horizontal ? 50 : 200)
            .AddNode("target", "T", horizontal ? reversed ? 0 : 400 : 150,
                horizontal ? 150 : reversed ? 0 : 400, width: horizontal ? 200 : 50, height: horizontal ? 50 : 200)
            .AddEdge("route", "source", "target", direction: VisualLinkDirection.Forward, routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal);
        var sourceNode = chart.Nodes.Single(node => node.Id == "source");
        var targetNode = chart.Nodes.Single(node => node.Id == "target");
        var candidate = TopologyRenderPrimitives.ObstacleOrthogonalDefaultPoints(sourceNode, targetNode,
            TopologyEdgePort.Auto, TopologyEdgePort.Auto, 0);
        var sourceBounds = new ChartRect(sourceNode.X, sourceNode.Y, sourceNode.Width, sourceNode.Height);
        var targetBounds = new ChartRect(targetNode.X, targetNode.Y, targetNode.Width, targetNode.Height);
        NormalEnd(candidate, sourceBounds, source: true);
        NormalEnd(candidate, targetBounds, source: false);
        Assert.Equal(horizontal ? reversed ? sourceBounds.Left - 7 : sourceBounds.Right + 7 : reversed ? sourceBounds.Top - 7 : sourceBounds.Bottom + 7,
            horizontal ? candidate[0].X : candidate[0].Y, 6);
        var prepared = chart.Prepare(new TopologyRenderOptions { IncludeLegend = false, IncludeEdgeLabels = false,
            ArrowMarkerStyle = TopologyArrowMarkerStyle.Diamond });
        var report = prepared.Analyze();
        Capture(prepared, "obstacle-axis-" + (horizontal ? "wide" : "tall") + (reversed ? "-reversed" : ""));
        var route = Assert.Single(report.Edges);
        Assert.True(route.SourceAttached && route.TargetAttached);
        Assert.Equal(0, route.ObstacleHits);
        var source = report.Nodes.Single(node => node.Id == "source").Bounds;
        var target = report.Nodes.Single(node => node.Id == "target").Bounds;
        // The obstacle router may choose another side; whichever corridor wins must
        // leave and arrive along that physical side's outward normal, never its tangent.
        NormalEnd(route.Points, source, source: true);
        NormalEnd(route.Points, target, source: false);
    }

    private static void NormalEnd(IReadOnlyList<ChartPoint> points, ChartRect bounds, bool source) {
        var point = source ? points[0] : points[^1];
        var adjacent = source ? points[1] : points[^2];
        var dx = adjacent.X - point.X; var dy = adjacent.Y - point.Y;
        if (Math.Abs(dx) > .001) {
            Assert.InRange(Math.Abs(dy), 0, .001);
            Assert.InRange(point.Y, bounds.Top, bounds.Bottom);
            Assert.Equal(point.X < bounds.Left ? bounds.Left - 7 : bounds.Right + 7, point.X, 6);
            Assert.True(dx * (point.X < bounds.Left ? -1 : 1) > 0);
        } else {
            Assert.InRange(point.X, bounds.Left, bounds.Right);
            Assert.Equal(point.Y < bounds.Top ? bounds.Top - 7 : bounds.Bottom + 7, point.Y, 6);
            Assert.True(dy * (point.Y < bounds.Top ? -1 : 1) > 0);
        }
    }

    private static TopologyRenderOptions Options() {
        var options = TopologyRenderOptions.FromPreset(TopologyViewPreset.RelationshipOverview)
            .WithSelectedNode("domain").WithSelectedEdge("finding-domain");
        options.ArrowMarkerStyle = TopologyArrowMarkerStyle.Diamond;
        return options;
    }

    private static void Separate(TopologyLayoutDiagnosticReport report, string firstId, string secondId, bool source, double minimum = 12) {
        var first = report.Edges.Single(edge => edge.Id == firstId).Points;
        var second = report.Edges.Single(edge => edge.Id == secondId).Points;
        var a = source ? first[0] : first[^1];
        var b = source ? second[0] : second[^1];
        Assert.True(Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2)) >= minimum,
            "Wide relationship arrows must retain distinct attachments: " + firstId + " / " + secondId);
    }

    private static void Capture(PreparedTopology prepared, string name) {
        if (Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, name + ".svg"), prepared.ToSvg());
        File.WriteAllBytes(Path.Combine(output, name + ".png"), prepared.ToPng());
        File.WriteAllText(Path.Combine(output, name + ".json"), JsonSerializer.Serialize(prepared.Analyze(), new JsonSerializerOptions { IncludeFields = true }));
    }

    private static TopologyChart MiniMap() {
        return TopologyChart.Create()
            .WithId("visual-mini-correlation-map")
            .WithTitle("Mini Correlation Map")
            .WithSubtitle("Small dashboard-card relationship map for selected assets.")
            .WithViewport(920, 420, 24)
            .WithTheme(VisualTheme.Graphite())
            .WithLegend(TopologyLegend.Create(null)
                .AddEdgeKind("Verified", TopologyEdgeKind.Mapping, "#16A34A", TopologyEdgeLineStyle.Solid)
                .AddEdgeKind("Observed", TopologyEdgeKind.Link, "#2563EB", TopologyEdgeLineStyle.Dashed)
                .AddEdgeKind("Inferred", TopologyEdgeKind.Dependency, "#94A3B8", TopologyEdgeLineStyle.Dotted))
            .AddNode("dns", "DNS: ad.evotec.xyz\n10.1.4.10", 72, 116, TopologyNodeKind.Network, TopologyHealthStatus.Healthy, subtitle: "internal", width: 178, height: 74, symbol: "DNS", color: "#0891B2")
            .AddNode("ip", "IP: 192.168.2.146\nInternal", 72, 224, TopologyNodeKind.Endpoint, TopologyHealthStatus.Healthy, subtitle: "observed", width: 178, height: 74, symbol: "IP", color: "#0891B2")
            .AddNode("domain", "ad.evotec.xyz\nCertificate", 372, 170, TopologyNodeKind.Namespace, TopologyHealthStatus.Healthy, subtitle: "selected record", width: 186, height: 82, symbol: "D", color: "#2563EB")
            .AddNode("server", "evotec-web-01\n192.168.2.15", 668, 92, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, subtitle: "hosting", width: 176, height: 74, symbol: "SRV", color: "#2563EB")
            .AddNode("finding", "Finding (7)\n3 Critical + 4 High", 658, 232, TopologyNodeKind.Process, TopologyHealthStatus.Critical, subtitle: "risk evidence", width: 196, height: 80, symbol: "!", color: "#EF4444")
            .AddEdge("dns-domain", "dns", "domain", "verified", TopologyEdgeKind.Mapping, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward, TopologyEdgeRouting.ObstacleAvoidingOrthogonal, "DNS")
            .AddEdge("ip-domain", "ip", "domain", "observed", TopologyEdgeKind.Link, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward, TopologyEdgeRouting.ObstacleAvoidingOrthogonal, "IP")
            .AddEdge("domain-server", "domain", "server", "inferred", TopologyEdgeKind.Dependency, TopologyHealthStatus.Unknown, VisualLinkDirection.Forward, TopologyEdgeRouting.ObstacleAvoidingOrthogonal, "host")
            .AddEdge("finding-domain", "domain", "finding", "selected", TopologyEdgeKind.Link, TopologyHealthStatus.Critical, VisualLinkDirection.Forward, TopologyEdgeRouting.ObstacleAvoidingOrthogonal, "risk")
            .WithEdgeLineStyle("ip-domain", TopologyEdgeLineStyle.Dashed)
            .WithEdgeLineStyle("domain-server", TopologyEdgeLineStyle.Dotted)
            .WithEdgeLineStyle("finding-domain", TopologyEdgeLineStyle.Dashed)
            .WithEdgeColor("dns-domain", "#16A34A")
            .WithEdgeColor("ip-domain", "#2563EB")
            .WithEdgeColor("domain-server", "#94A3B8")
            .WithEdgeColor("finding-domain", "#EF4444");
    }

}
