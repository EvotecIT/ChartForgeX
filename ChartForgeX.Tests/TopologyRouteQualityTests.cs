using ChartForgeX.Topology;
using Xunit;
using Xunit.Abstractions;

namespace ChartForgeX.Tests;

/// <summary>
/// Route quality on dense layouts, measured on the prepared geometry both renderers draw: routes through foreign cards,
/// captions and group headers, routes drawn on top of each other, route ends that do not point at their node, and edge
/// labels away from their route.
/// </summary>
public sealed class TopologyRouteQualityTests {
    private readonly ITestOutputHelper _output;

    public TopologyRouteQualityTests(ITestOutputHelper output) => _output = output;

    public static IEnumerable<object[]> Fixtures => new[] {
        new object[] { "small" },
        new object[] { "mesh" },
        new object[] { "overview" },
        new object[] { "drill" },
        new object[] { "replication-76" },
        new object[] { "replication-121" },
        new object[] { "replication-144" }
    };

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void DenseFixture_RoutesStayClearSeparateAndAttached(string fixture) {
        var (chart, options) = Build(fixture);
        var prepared = chart.Prepare(options);
        var quality = RouteQuality.Measure(prepared.Analyze());
        _output.WriteLine(fixture + ": " + quality);
        Save(fixture, prepared);

        Assert.True(quality.NodeCrossings + quality.CaptionCrossings + quality.HeaderCrossings == 0, "Routes run through cards, captions or group headers. " + quality);
        Assert.True(quality.Overlaps == 0, "Routes are drawn on top of each other. " + quality);
        Assert.True(quality.DetachedEnds == 0, "Route ends do not point at their node. " + quality);
        Assert.True(quality.FarthestLabel == 0, "An edge label is not on its route. " + quality);
        Assert.True(quality.LabelOverlaps == 0, "Edge labels overlap. " + quality);
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void DenseFixture_AllRoutesArePlannedTogether(string fixture) {
        var (chart, options) = Build(fixture);
        var report = chart.Prepare(options).Analyze();
        Assert.All(report.Edges, edge => Assert.Equal(TopologyEdgeRouter.PlannedCorridor, edge.Corridor));
        Assert.All(report.Edges, edge => {
            for (var i = 1; i < edge.Points.Count; i++) {
                Assert.True(Math.Abs(edge.Points[i].X - edge.Points[i - 1].X) < 0.001 || Math.Abs(edge.Points[i].Y - edge.Points[i - 1].Y) < 0.001, edge.Id + " has a diagonal segment.");
                Assert.InRange(edge.Points[i].X, 0, report.Width);
                Assert.InRange(edge.Points[i].Y, 0, report.Height);
            }
        });
    }

    [Fact]
    public void ScatteredCards_AreStillPlannedWhenGridLinesAreMerged() {
        // 150 cards that share no rows or columns produce far more grid lines than the search holds.
        var chart = TopologyChart.Create().WithId("scattered").WithViewport(4200, 3200, 24).WithLegend(null);
        for (var i = 0; i < 150; i++) {
            chart.AddNode("n" + i, "N" + i, 60 + i % 15 * 270 + i * 0.7, 60 + i / 15 * 300 + i % 15 * 9.3, width: 60, height: 40);
        }

        for (var i = 0; i + 16 < 150; i += 3) chart.AddEdge("e" + i, "n" + i, "n" + (i + 16), routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal);
        var report = chart.Prepare(DenseRouteFixture.Options(legend: false)).Analyze();
        var quality = RouteQuality.Measure(report);
        Assert.All(report.Edges, edge => Assert.Equal(TopologyEdgeRouter.PlannedCorridor, edge.Corridor));
        Assert.True(quality.NodeCrossings + quality.CaptionCrossings + quality.Overlaps + quality.DetachedEnds == 0, quality.ToString());
    }

    [Fact]
    public void NamedPort_KeepsItsPositionWhenLanesAreSeparated() {
        var chart = TopologyChart.Create().WithId("named-lanes").WithViewport(900, 520, 24).WithLegend(null)
            .AddNode("hub", "Hub", 120, 220, width: 60, height: 44)
            .AddNodePort("hub", "upper", TopologyEdgePort.Right, 0.2);
        for (var i = 0; i < 4; i++) {
            chart.AddNode("leaf" + i, "Leaf " + i, 640, 60 + i * 110, width: 60, height: 44)
                .AddEdge("link" + i, "hub", "leaf" + i, routing: TopologyEdgeRouting.ObstacleAvoidingOrthogonal)
                .WithEdgePorts("link" + i, TopologyEdgePort.Right, TopologyEdgePort.Left);
        }

        chart.WithEdgeNamedPorts("link0", "upper", null);
        var report = chart.Prepare(DenseRouteFixture.Options(legend: false)).Analyze();
        var hub = report.Nodes.Single(node => node.Id == "hub").Bounds;
        var starts = report.Edges.Select(edge => edge.Points[0]).ToArray();
        Assert.Equal(hub.Top + hub.Height * 0.2, starts[0].Y, 3);
        Assert.All(starts, start => Assert.InRange(start.Y, hub.Top, hub.Bottom));
        Assert.Equal(starts.Length, starts.Select(start => Math.Round(start.Y, 1)).Distinct().Count());
        Assert.Empty(report.RouteOverlaps);
    }

    [Fact]
    public void RoutedEdges_WidenTheRowsTheyRunBetween() {
        var quiet = DenseRouteFixture.Overview(links: 0);
        var busy = DenseRouteFixture.Overview(links: 90);
        var options = DenseRouteFixture.Options(subtitles: true);
        var quietReport = quiet.Prepare(options).Analyze();
        var busyReport = busy.Prepare(options).Analyze();
        Assert.True(busyReport.Groups[0].Bounds.Height > quietReport.Groups[0].Bounds.Height, "Groups with many routed edges should reserve taller row gaps.");
        Assert.Equal(quietReport.Groups[0].Bounds.Width, busyReport.Groups[0].Bounds.Width);
    }

    internal static (TopologyChart Chart, TopologyRenderOptions Options) Build(string fixture) => fixture switch {
        "small" => (DenseRouteFixture.Small(), DenseRouteFixture.Options()),
        "mesh" => (DenseRouteFixture.Mesh(), DenseRouteFixture.Options()),
        "overview" => (DenseRouteFixture.Overview(), DenseRouteFixture.Options(subtitles: true)),
        "drill" => (DenseRouteFixture.Drill(), DenseRouteFixture.Options()),
        "replication-76" => (ReplicationTopologyFixture.Create(3, 10), DenseRouteFixture.Options(legend: false)),
        "replication-121" => (ReplicationTopologyFixture.Create(5, 15), DenseRouteFixture.Options(legend: false)),
        "replication-144" => (ReplicationTopologyFixture.Create(6, 18), DenseRouteFixture.Options(legend: false)),
        _ => throw new ArgumentOutOfRangeException(nameof(fixture), fixture, "Unknown fixture.")
    };

    private void Save(string fixture, PreparedTopology prepared) {
        var directory = Environment.GetEnvironmentVariable("CFX_TOPOLOGY_FIXTURE_OUTPUT");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "route-quality-" + fixture + ".svg"), prepared.ToSvg());
        File.WriteAllBytes(Path.Combine(directory, "route-quality-" + fixture + ".png"), prepared.ToPng());
        _output.WriteLine("saved " + Path.Combine(directory, "route-quality-" + fixture + ".png"));
    }
}

/// <summary>Counts of route defects in one prepared topology.</summary>
internal readonly record struct RouteQuality(int Edges, int NodeCrossings, int CaptionCrossings, int HeaderCrossings, int Overlaps, double OverlapLength, int DetachedEnds, double FarthestLabel, int LabelOverlaps) {
    public static RouteQuality Measure(TopologyLayoutDiagnosticReport report) {
        var labelOverlaps = 0;
        for (var i = 0; i < report.EdgeLabels.Count; i++) {
            for (var j = i + 1; j < report.EdgeLabels.Count; j++) {
                var a = report.EdgeLabels[i].Bounds;
                var b = report.EdgeLabels[j].Bounds;
                if (a.Left < b.Right && a.Right > b.Left && a.Top < b.Bottom && a.Bottom > b.Top) labelOverlaps++;
            }
        }

        return new RouteQuality(
            report.Edges.Count,
            report.RouteCrossings.Count(crossing => crossing.Kind == TopologyLayoutRouteCrossingKind.Node),
            report.RouteCrossings.Count(crossing => crossing.Kind == TopologyLayoutRouteCrossingKind.NodeCaption),
            report.RouteCrossings.Count(crossing => crossing.Kind == TopologyLayoutRouteCrossingKind.GroupHeader),
            report.RouteOverlaps.Count,
            report.RouteOverlaps.Sum(overlap => overlap.Length),
            report.Edges.Sum(edge => (edge.SourceAttached ? 0 : 1) + (edge.TargetAttached ? 0 : 1)),
            report.EdgeLabels.Select(label => label.RouteDistance).DefaultIfEmpty(0).Max(),
            labelOverlaps);
    }

    public override string ToString() =>
        $"edges={Edges} nodeCrossings={NodeCrossings} captionCrossings={CaptionCrossings} headerCrossings={HeaderCrossings} overlaps={Overlaps} overlapLength={OverlapLength:0} detachedEnds={DetachedEnds} farthestLabel={FarthestLabel:0.#} labelOverlaps={LabelOverlaps}";
}
