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

    // fixture, then ceilings: card + caption + header crossings, overlapping route pairs, detached route ends,
    // farthest label from its route (px), overlapping label pairs. These are the values measured before the dense
    // router was reworked; they record the starting point and only ever go down.
    public static IEnumerable<object[]> Fixtures => new[] {
        new object[] { "small", 0, 9, 10, 0, 0 },
        new object[] { "mesh", 100, 220, 36, 0, 0 },
        new object[] { "overview", 1, 530, 24, 0, 0 },
        new object[] { "drill", 1, 5, 12, 0, 0 },
        new object[] { "replication-76", 22, 115, 68, 101, 1 },
        new object[] { "replication-121", 36, 130, 54, 86, 2 },
        new object[] { "replication-144", 33, 179, 66, 101, 2 }
    };

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void DenseFixture_RouteDefectsStayWithinCeilings(string fixture, int crossingCeiling, int overlapCeiling, int detachedCeiling, int labelDistanceCeiling, int labelOverlapCeiling) {
        var (chart, options) = Build(fixture);
        var prepared = chart.Prepare(options);
        var quality = RouteQuality.Measure(prepared.Analyze());
        _output.WriteLine(fixture + ": " + quality);
        Save(fixture, prepared);

        Assert.Equal(0, quality.NodeCrossings);
        Assert.True(quality.CaptionCrossings + quality.HeaderCrossings <= crossingCeiling, fixture + ": " + quality);
        Assert.True(quality.Overlaps <= overlapCeiling, fixture + ": " + quality);
        Assert.True(quality.DetachedEnds <= detachedCeiling, fixture + ": " + quality);
        Assert.True(quality.FarthestLabel <= labelDistanceCeiling, fixture + ": " + quality);
        Assert.True(quality.LabelOverlaps <= labelOverlapCeiling, fixture + ": " + quality);
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
