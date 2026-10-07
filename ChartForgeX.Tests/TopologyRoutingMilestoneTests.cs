using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;
using static ChartForgeX.Tests.TopologyRoutingFixtures;

namespace ChartForgeX.Tests;

public sealed class TopologyRoutingMilestoneTests {
    [Theory]
    [InlineData(TopologyLayoutMode.Matrix, false)]
    [InlineData(TopologyLayoutMode.Matrix, true)]
    [InlineData(TopologyLayoutMode.DenseGrouped, false)]
    [InlineData(TopologyLayoutMode.DenseGrouped, true)]
    public void PreservedViewExportsKeepTheSameOutputDimensions(TopologyLayoutMode mode, bool explicitSize) {
        var chart = DenseRouteFixture.Mesh(12, 24, 20).WithViewport(600, 300).WithLayout(mode);
        chart.WithRenderOptions(Options());
        var artifact = chart.ToVisualArtifact();
        if (explicitSize) artifact.NaturalSize = new VisualArtifactSize(480, 240);
        artifact.PreserveNaturalSize = true;
        var size = artifact.NaturalSize!.Value;
        var view = Options();
        view.View = new TopologyView { NodeIds = { chart.Nodes[0].Id, chart.Nodes[1].Id }, IncludeNodeGroups = false };
        var renderOptions = new VisualArtifactRenderOptions { Topology = view };
        var envelope = artifact.ToInterchangeEnvelope(renderOptions);
        var svg = XDocument.Parse(artifact.ToSvg(renderOptions));
        var png = ChartForgeX.Raster.PngReader.Decode(artifact.ToPng(renderOptions));
        Assert.Equal(size.Width, envelope.Width);
        Assert.Equal(size.Height, envelope.Height);
        Assert.Equal(size.Width, double.Parse(svg.Root!.Attribute("width")!.Value, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(size.Height, double.Parse(svg.Root.Attribute("height")!.Value, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal((int)Math.Ceiling(size.Width), png.Width);
        Assert.Equal((int)Math.Ceiling(size.Height), png.Height);
        var layoutInput = explicitSize ? DenseRouteFixture.Mesh(12, 24, 20).WithViewport(size.Width, size.Height).WithLayout(mode) : chart;
        var expectedOptions = view.Clone();
        // Explicit output replacement is a fixed viewport. Compare the same fit policy that
        // exports must use to keep its full geometry inside that caller-selected canvas.
        expectedOptions.FitContentToViewport = explicitSize;
        var expected = layoutInput.Prepare(expectedOptions).ToInterchangeEnvelope();
        Assert.Equal(expected.Nodes.Select(node => (node.Id, node.X, node.Y, node.Width, node.Height)),
            envelope.Nodes.Select(node => (node.Id, node.X, node.Y, node.Width, node.Height)));
        Assert.Equal(2, envelope.Nodes.Count);
    }

    [Fact]
    public void SharedTrunksDoNotIntroduceSiblingCrossingsOrPrefixOverdraw() {
        var chart = TopologyChart.Create().WithId("sibling-trunks").WithViewport(1200, 760).WithLegend(null)
            .AddNode("target", "Service", 1000, 300, width: 100, height: 44);
        var positions = new[] { (640, 500), (640, 324), (220, 324), (780, 324), (780, 148), (500, 324) };
        for (var i = 0; i < positions.Length; i++) {
            chart.AddNode("n" + i, "Input " + i, positions[i].Item1, positions[i].Item2, width: 100, height: 44)
                .AddEdge("e" + i, "n" + i, "target", kind: TopologyEdgeKind.Dependency, status: TopologyHealthStatus.Healthy,
                    direction: VisualLinkDirection.Forward, routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal)
                .WithEdgePorts("e" + i, TopologyEdgePort.Right, TopologyEdgePort.Left);
        }
        var plain = Options();
        var shared = plain.Clone();
        shared.ShareIncomingTrunks = true;
        var before = chart.Prepare(plain).Analyze();
        var after = chart.Prepare(shared).Analyze();
        Assert.True(ProperCrossings(after) <= ProperCrossings(before));
        Assert.DoesNotContain(after.RouteOverlaps, overlap => !overlap.IsIntentional);
        Assert.All(after.Edges, edge => Assert.True(edge.SourceAttached && edge.TargetAttached));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ArtifactNaturalSizeUsesPreparedGeometry(bool fit) {
        var chart = DenseRouteFixture.Mesh(12, 20, 25).WithViewport(600, 300);
        var options = DenseRouteFixture.Options(legend: false);
        options.FitContentToViewport = fit;
        chart.WithRenderOptions(options);
        var prepared = chart.Prepare();
        var artifact = chart.ToVisualArtifact();
        if (fit) Assert.Equal(chart.Viewport.Height, prepared.Height);
        else Assert.True(prepared.Height > chart.Viewport.Height);
        Assert.Equal(prepared.Width, artifact.NaturalSize!.Value.Width);
        Assert.Equal(prepared.Height, artifact.NaturalSize.Value.Height);
        Assert.Equal(prepared.Height, artifact.ToInterchangeEnvelope().Height);
        Assert.Equal(300, chart.Viewport.Height);
    }

    [Theory]
    [InlineData(TopologyLayoutMode.Matrix)]
    [InlineData(TopologyLayoutMode.DenseGrouped)]
    public void PreservingDiscoveredNaturalSizeDoesNotReflowLayout(TopologyLayoutMode mode) {
        var chart = DenseRouteFixture.Mesh(12, 24, 20).WithViewport(600, 300).WithLayout(mode);
        chart.WithRenderOptions(Options());
        var expected = chart.Prepare().ToInterchangeEnvelope();
        Assert.NotNull(expected.Width);
        Assert.NotNull(expected.Height);
        var expectedWidth = expected.Width.GetValueOrDefault();
        var expectedHeight = expected.Height.GetValueOrDefault();
        var artifact = chart.ToVisualArtifact();
        artifact.PreserveNaturalSize = true;
        var envelope = artifact.ToInterchangeEnvelope();
        Assert.Equal(expected.Nodes.Select(node => (node.X, node.Y, node.Width, node.Height)),
            envelope.Nodes.Select(node => (node.X, node.Y, node.Width, node.Height)));
        var svg = XDocument.Parse(artifact.ToSvg());
        Assert.Equal(expectedWidth, double.Parse(svg.Root!.Attribute("width")!.Value, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(expectedHeight, double.Parse(svg.Root.Attribute("height")!.Value, System.Globalization.CultureInfo.InvariantCulture));
        var png = ChartForgeX.Raster.PngReader.Decode(artifact.ToPng());
        Assert.Equal((int)expectedWidth, png.Width);
        Assert.Equal((int)expectedHeight, png.Height);
        var html = artifact.ToHtmlPage();
        var start = html.IndexOf("<svg", StringComparison.Ordinal);
        var end = html.IndexOf("</svg>", start, StringComparison.Ordinal) + 6;
        var embedded = XDocument.Parse(html.Substring(start, end - start));
        Assert.True(XNode.DeepEquals(svg.Root, embedded.Root));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ClassicRoutesAvoidTheDrawnCenteredHeader(bool labels) {
        var chart = CenteredHeader();
        var options = Options();
        options.ReadableDenseLayout = false;
        options.IncludeGroupLabels = labels;
        var prepared = chart.Prepare(options);
        var report = prepared.Analyze();
        Assert.All(report.Edges, edge => Assert.True(edge.SourceAttached && edge.TargetAttached));
        Assert.DoesNotContain(report.RouteCrossings, item => item.Kind == TopologyLayoutRouteCrossingKind.GroupHeader);
        if (labels) {
            Assert.NotNull(report.Groups[0].HeaderBounds);
            Assert.True(report.Edges[0].Points.Count > 2);
        } else {
            Assert.Null(report.Groups[0].HeaderBounds);
            Assert.Equal(2, report.Edges[0].Points.Count);
        }
    }

    [Fact]
    public void ExplicitNaturalSizeAssignmentRetainsViewportPolicyEvenAtDiscoveredSize() {
        var chart = DenseRouteFixture.Mesh(12, 24, 20).WithViewport(600, 300).WithLayout(TopologyLayoutMode.Matrix);
        chart.WithRenderOptions(Options());
        var artifact = chart.ToVisualArtifact();
        var size = artifact.NaturalSize!.Value;
        artifact.NaturalSize = size;
        artifact.PreserveNaturalSize = true;
        var fit = Options(); fit.FitContentToViewport = true;
        var expected = DenseRouteFixture.Mesh(12, 24, 20).WithViewport(size.Width, size.Height)
            .WithLayout(TopologyLayoutMode.Matrix).WithRenderOptions(fit).Prepare().ToInterchangeEnvelope();
        var actual = artifact.ToInterchangeEnvelope();
        Assert.Equal(expected.Nodes.Select(node => (node.X, node.Y)), actual.Nodes.Select(node => (node.X, node.Y)));
    }

    [Theory]
    [InlineData(TopologyEdgeRouting.Orthogonal)]
    [InlineData(TopologyEdgeRouting.Straight)]
    [InlineData(TopologyEdgeRouting.Curved)]
    public void MixedPlannerRetainsAuthoredRouteGeometry(TopologyEdgeRouting routing) {
        var chart = Mixed(routing);
        var plain = Options();
        plain.ReadableDenseLayout = false;
        var expected = chart.Prepare(plain).Analyze().Edges.Single(edge => edge.Id == "fixed");
        var actual = chart.Prepare(Options()).Analyze().Edges.Single(edge => edge.Id == "fixed");
        Assert.Equal(expected.Points, actual.Points);
    }

    [Fact]
    public void PlannedRouteAvoidsOccupiedWaypointCorridor() {
        var chart = Mixed(TopologyEdgeRouting.Orthogonal)
            .WithEdgeWaypoints("fixed", new ChartPoint(380, 142), new ChartPoint(550, 142));
        var prepared = chart.Prepare(Options());
        var report = prepared.Analyze();
        Assert.Empty(report.RouteOverlaps);
        Assert.Empty(report.RouteCrossings);
        Assert.All(report.Edges, edge => Assert.True(edge.SourceAttached && edge.TargetAttached));
        Assert.Equal("maze", report.Edges.Single(edge => edge.Id == "planned").Corridor);
    }

    [Theory]
    [InlineData("mesh", 420)]
    [InlineData("overview", 700)]
    public void CompletePlanReducesCrossingsWithoutOverdraw(string fixture, int maximumCrossings) {
        var chart = fixture == "mesh" ? DenseRouteFixture.Mesh() : DenseRouteFixture.Overview();
        var report = chart.Prepare(DenseRouteFixture.Options(legend: false)).Analyze();
        // Independent segment intersections protect the observed readability improvement. The old planner produced
        // 578/854 crossings on these fixtures; diagnostics alone did not count relationship-to-relationship crossings.
        Assert.InRange(ProperCrossings(report), 1, maximumCrossings);
        Assert.Empty(report.RouteOverlaps);
        Assert.Empty(report.RouteCrossings);
        Assert.All(report.Edges, edge => Assert.True(edge.SourceAttached && edge.TargetAttached));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SharedTrunksAreOptInAndPaintTheCommonTailOnce(bool named) {
        var chart = FanIn(named);
        var separate = chart.Prepare(Options()).Analyze();
        if (!named) Assert.Empty(separate.RouteOverlaps);
        else Assert.All(separate.RouteOverlaps, overlap => Assert.InRange(overlap.Length, 0, 14));
        var options = Options();
        options.ShareIncomingTrunks = true;
        var prepared = chart.Prepare(options);
        var report = prepared.Analyze();
        Assert.NotEmpty(report.RouteOverlaps);
        Assert.All(report.RouteOverlaps, overlap => Assert.True(overlap.IsIntentional));
        Assert.Empty(report.RouteCrossings);
        Assert.All(report.Edges, edge => Assert.True(edge.SourceAttached && edge.TargetAttached));
        if (named) Assert.All(report.Edges, edge => Assert.Equal(180 + 44 * 0.35, edge.Points.Last().Y, 3));
        var svg = XDocument.Parse(prepared.ToSvg());
        var branches = svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "topology-shared-trunk-hit").ToList();
        Assert.NotEmpty(branches);
        Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "topology-shared-trunk-tail");
        Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "topology-marker");
        Assert.NotEmpty(prepared.ToPng());
        Assert.All(chart.Edges, edge => Assert.Null(edge.TargetMarker));
    }

    [Fact]
    public void TrunksRetainDistinctInkAndSelectionStates() {
        var chart = FanIn(false);
        chart.Edges[1].Color = "#dc2626";
        var options = Options();
        options.ShareIncomingTrunks = true;
        options.SelectedEdgeIds.Add("e2");
        var report = chart.Prepare(options).Analyze();
        Assert.DoesNotContain(report.RouteOverlaps, overlap => overlap.IsIntentional);
    }

    [Fact]
    public void DashedRelationshipsKeepIndependentPatternAndMarkers() {
        var chart = FanIn(false);
        foreach (var edge in chart.Edges) edge.DashPattern.AddRange(new[] { 8.0, 4.0 });
        var options = Options();
        options.ShareIncomingTrunks = true;
        var prepared = chart.Prepare(options);
        Assert.DoesNotContain(prepared.Analyze().RouteOverlaps, overlap => overlap.IsIntentional);
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.Equal(3, svg.Descendants().Count(element => (string?)element.Attribute("data-cfx-role") == "topology-marker"));
        Assert.All(svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "topology-edge-line"),
            element => Assert.Equal("8 4", (string?)element.Attribute("stroke-dasharray")));
    }

    [Fact]
    public void BusyWrappedPanelsWidenColumnsWithinViewportBudget() {
        var quiet = DenseRouteFixture.Overview(7, 12, 0).WithViewport(1100, 900);
        var busy = DenseRouteFixture.Overview(7, 12, 140).WithViewport(1100, 900);
        var quietReport = quiet.Prepare(Options()).Analyze();
        var busyReport = busy.Prepare(Options()).Analyze();
        Assert.True(busyReport.Groups[0].Bounds.Width > quietReport.Groups[0].Bounds.Width);
        Assert.True(busyReport.Groups[0].Bounds.Width <= 1100 - 48);
        Assert.True(busyReport.Groups.Max(group => group.Bounds.Right) <= 1100 - 24);
        Assert.Empty(busyReport.Collisions);
    }

    private static int ProperCrossings(TopologyLayoutDiagnosticReport report) {
        var count = 0;
        for (var first = 0; first < report.Edges.Count; first++) for (var second = first + 1; second < report.Edges.Count; second++) {
            var a = report.Edges[first].Points;
            var b = report.Edges[second].Points;
            var intentional = report.RouteOverlaps.FirstOrDefault(overlap => overlap.IsIntentional &&
                overlap.FirstEdgeId == report.Edges[first].Id && overlap.SecondEdgeId == report.Edges[second].Id)?.Length ?? 0;
            var locations = new HashSet<(long, long)>();
            for (var i = 1; i < a.Count; i++) for (var j = 1; j < b.Count; j++) {
                var dx = a[i].X - a[i - 1].X;
                var dy = a[i].Y - a[i - 1].Y;
                var ex = b[j].X - b[j - 1].X;
                var ey = b[j].Y - b[j - 1].Y;
                var denominator = dx * ey - dy * ex;
                if (Math.Abs(denominator) < 1e-9) continue;
                var x = b[j - 1].X - a[i - 1].X;
                var y = b[j - 1].Y - a[i - 1].Y;
                var t = (x * ey - y * ex) / denominator;
                var u = (x * dy - y * dx) / denominator;
                if (t < -1e-6 || t > 1 + 1e-6 || u < -1e-6 || u > 1 + 1e-6) continue;
                var point = new ChartPoint(a[i - 1].X + t * dx, a[i - 1].Y + t * dy);
                if (new[] { a[0], a.Last(), b[0], b.Last() }.Any(end => Math.Abs(end.X - point.X) + Math.Abs(end.Y - point.Y) < 0.01)) continue;
                if (intentional > 0 && DistanceFromTarget(a, point) <= intentional + 0.01 &&
                    DistanceFromTarget(b, point) <= intentional + 0.01) continue;
                locations.Add(((long)Math.Round(point.X * 100), (long)Math.Round(point.Y * 100)));
            }
            count += locations.Count;
        }
        return count;
    }

    private static double DistanceFromTarget(IReadOnlyList<ChartPoint> route, ChartPoint point) {
        var distance = 0.0;
        for (var index = route.Count - 1; index > 0; index--) {
            var a = route[index];
            var b = route[index - 1];
            var length = Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
            var toA = Math.Abs(point.X - a.X) + Math.Abs(point.Y - a.Y);
            var toB = Math.Abs(point.X - b.X) + Math.Abs(point.Y - b.Y);
            if (Math.Abs(toA + toB - length) < 0.01) return distance + toA;
            distance += length;
        }
        return double.PositiveInfinity;
    }
}
