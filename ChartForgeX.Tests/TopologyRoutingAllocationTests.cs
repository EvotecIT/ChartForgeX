using System.Globalization;
using ChartForgeX.Topology;
using Xunit;
using Xunit.Abstractions;

namespace ChartForgeX.Tests;

[Collection(nameof(RenderingBudgetCollection))]
public sealed class TopologyRoutingAllocationTests {
    private readonly ITestOutputHelper _output;
    public TopologyRoutingAllocationTests(ITestOutputHelper output) => _output = output;

    // Routing and its diagnostics score every edge against every other edge. When each of those calls copied every
    // other route and rebuilt the endpoint map, preparing this chart allocated about 680 MB; it now takes about 280 MB.
    [Fact]
    public void LargeForceDirectedTopologyPreparesWithinAllocationBudget() {
        const long budget = 450L * 1024 * 1024;
        var options = new TopologyRenderOptions { IncludeLegend = false };
        Chart(60).Prepare(options);
        var chart = Chart(600);
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        var prepared = chart.Prepare(options);
        allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
        _output.WriteLine($"600 nodes, {chart.Edges.Count} edges: {allocated / 1048576} MB allocated; budget {budget / 1048576} MB.");
        Assert.Equal(chart.Edges.Count, prepared.EdgeCount);
        Assert.True(allocated <= budget, $"Preparing the topology allocated {allocated / 1048576} MB.");
    }

    // Groups of ten cards joined in a chain, about 0.3 extra links per card across groups, and a link between neighbouring groups.
    private static TopologyChart Chart(int nodes) {
        var side = Math.Sqrt(nodes) * 90;
        var chart = TopologyChart.Create().WithId("routing-allocation").WithTitle("Routing allocation")
            .WithViewport(Math.Max(1200, side * 1.4), Math.Max(800, side), 24)
            .WithLayout(TopologyLayoutMode.ForceDirected, TopologyLayoutDirection.LeftToRight);
        var groups = Math.Max(1, nodes / 10);
        uint state = 777;
        int Next(int max) { state = unchecked(state * 1664525u + 1013904223u); return (int)((state >> 8) % (uint)max); }
        for (var g = 0; g < groups; g++) chart.AddAutoGroup("g" + g, "Group " + (g + 1).ToString(CultureInfo.InvariantCulture));
        for (var n = 0; n < nodes; n++)
            chart.AddAutoNode("n" + n.ToString(CultureInfo.InvariantCulture), "SRV-" + n.ToString("0000", CultureInfo.InvariantCulture), TopologyNodeKind.Server,
                TopologyHealthStatus.Healthy, "g" + (n / 10 % groups).ToString(CultureInfo.InvariantCulture), width: 108, height: 40);
        var edge = 0;
        for (var n = 1; n < nodes; n++) if (n % 10 != 0) chart.AddEdge("e" + edge++, "n" + (n - 1), "n" + n);
        var pairs = new HashSet<long>();
        for (var attempt = 0; pairs.Count < nodes * 3 / 10 && attempt < nodes * 12; attempt++) {
            int a = Next(nodes), b = Next(nodes);
            if (a / 10 == b / 10 || !pairs.Add((long)Math.Min(a, b) * 100000 + Math.Max(a, b))) continue;
            chart.AddEdge("e" + edge++, "n" + a, "n" + b, kind: TopologyEdgeKind.Replication);
        }
        for (var g = 1; g < groups; g++) chart.AddEdge("e" + edge++, "n" + (g - 1) * 10, "n" + g * 10);
        return chart;
    }
}
