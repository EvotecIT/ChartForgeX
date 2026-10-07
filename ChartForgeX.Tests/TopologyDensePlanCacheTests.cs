using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

[CollectionDefinition(nameof(TopologyDensePlanCacheTests), DisableParallelization = true)]
public sealed class TopologyDensePlanCacheCollection { }

/// <summary>
/// The dense planner shares plans between charts whose derived planning inputs are equal, as a host's light and dark
/// drawings of one chart are. A shared plan must equal a fresh one, and any change to the inputs must plan afresh.
/// </summary>
[Collection(nameof(TopologyDensePlanCacheTests))]
public sealed class TopologyDensePlanCacheTests {
    [Fact]
    public void Plan_EqualChartDrawnAgain_ReusesAPlanEqualToAFreshOne() {
        TopologyDenseRoutePlanner.ClearPlanCache();
        var first = Routes(DenseReplicationFixture.Sites60(), DenseReplicationFixture.Options());
        Assert.Equal(1, TopologyDenseRoutePlanner.CachedPlanCount);
        var dark = DenseReplicationFixture.Sites60();
        dark.Theme = TopologyTheme.Dark();
        var second = Routes(dark, DenseReplicationFixture.Options());
        Assert.Equal(1, TopologyDenseRoutePlanner.CachedPlanCount);
        TopologyDenseRoutePlanner.ClearPlanCache();
        var fresh = Routes(DenseReplicationFixture.Sites60(), DenseReplicationFixture.Options());
        AssertSame(fresh, first);
        AssertSame(fresh, second);
    }

    [Fact]
    public void Plan_ChangedTerminalOrTrunkInput_PlansAfresh() {
        TopologyDenseRoutePlanner.ClearPlanCache();
        Routes(DenseReplicationFixture.Sites60(), DenseReplicationFixture.Options());
        var moved = DenseReplicationFixture.Sites60().WithEdgePorts("link-0", TopologyEdgePort.Top, TopologyEdgePort.Auto);
        var movedRoutes = Routes(moved, DenseReplicationFixture.Options());
        Assert.Equal(2, TopologyDenseRoutePlanner.CachedPlanCount);

        var trunks = DenseReplicationFixture.Options();
        trunks.ShareIncomingTrunks = true;
        var trunkRoutes = Routes(DenseReplicationFixture.Sites60(), trunks);
        Assert.Equal(3, TopologyDenseRoutePlanner.CachedPlanCount);
        var recoloured = DenseReplicationFixture.Sites60();
        recoloured.Edges[3].Color = "#123456";
        var recolouredRoutes = Routes(recoloured, trunks);
        Assert.Equal(4, TopologyDenseRoutePlanner.CachedPlanCount);

        TopologyDenseRoutePlanner.ClearPlanCache();
        AssertSame(Routes(DenseReplicationFixture.Sites60().WithEdgePorts("link-0", TopologyEdgePort.Top, TopologyEdgePort.Auto), DenseReplicationFixture.Options()), movedRoutes);
        AssertSame(Routes(DenseReplicationFixture.Sites60(), trunks), trunkRoutes);
        var again = DenseReplicationFixture.Sites60();
        again.Edges[3].Color = "#123456";
        AssertSame(Routes(again, trunks), recolouredRoutes);
    }

    private static List<IReadOnlyList<ChartPoint>> Routes(TopologyChart chart, TopologyRenderOptions options) =>
        chart.Prepare(options).Analyze().Edges.Select(edge => (IReadOnlyList<ChartPoint>)edge.Points.ToList()).ToList();

    private static void AssertSame(List<IReadOnlyList<ChartPoint>> expected, List<IReadOnlyList<ChartPoint>> actual) {
        Assert.Equal(expected.Count, actual.Count);
        for (var i = 0; i < expected.Count; i++) Assert.Equal(expected[i], actual[i]);
    }
}
