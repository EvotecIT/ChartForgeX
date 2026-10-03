using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyFallbackRoutingTests {
    [Theory]
    [InlineData(18)]
    [InlineData(34)]
    [InlineData(136)]
    public void CrowdedReadableFallbackRoutesKeepBothEndsAttached(int index) {
        var chart = DenseRouteFixture.Mesh(4 + index % 17, 25 + index % 20, 10 + index % 50)
            .WithId("fallback-" + index).WithViewport(900 + index % 3 * 180, 500 + index % 5 * 180);
        var options = DenseRouteFixture.Options(subtitles: index % 3 == 0, legend: index % 4 == 0);
        options.ShareIncomingTrunks = index % 7 == 0;
        if (index % 3 == 0) for (var i = 0; i < chart.Edges.Count; i += 3) chart.Edges[i].Label = "Link " + i;

        var report = chart.Prepare(options).Analyze();
        Assert.Contains(report.Edges, edge => edge.Corridor != TopologyEdgeRouter.PlannedCorridor);
        Assert.All(report.Edges, edge => Assert.True(edge.SourceAttached && edge.TargetAttached,
            edge.Id + " fallback route does not point at both nodes."));
    }
}
