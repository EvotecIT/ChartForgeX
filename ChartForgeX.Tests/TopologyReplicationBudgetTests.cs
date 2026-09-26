using System.Diagnostics;
using ChartForgeX.Topology;
using Xunit;
using Xunit.Abstractions;

namespace ChartForgeX.Tests;

/// <summary>
/// Dense Active Directory replication fixtures (76, 121, and 144 domain controllers in 16, 24, and 28 sites) with CI
/// budgets for render time, SVG size, and routing quality. The time budget is generous for small CI runners and a JIT
/// warm-up render runs first. Size and routing ceilings sit about 25% above the values measured on Windows; layout uses
/// portable text estimates, so the margin covers floating-point differences between runner architectures. The ceilings
/// are regression guards: routes still cross foreign cards and the 144-DC tier still has overlapping cards (TODO.md).
/// </summary>
[Collection(nameof(TopologyReplicationBudgetCollection))]
public sealed class TopologyReplicationBudgetTests {
    private const long RenderTimeBudgetMilliseconds = 45_000;
    private readonly ITestOutputHelper _output;

    public TopologyReplicationBudgetTests(ITestOutputHelper output) => _output = output;

    public static IEnumerable<object[]> Tiers => new[] {
        // branches per region, DCs per hub site, SVG byte budget, edge/foreign-card crossing ceiling, node collision ceiling
        new object[] { 3, 10, 480_000, 39, 0 },
        new object[] { 5, 15, 760_000, 78, 0 },
        // Known DenseGrouped defect: six overlapping DC cards at 144 DCs.
        new object[] { 6, 18, 900_000, 103, 8 }
    };

    [Theory]
    [MemberData(nameof(Tiers))]
    public void DenseReplicationFixture_RendersWithinBudgets(int branchSites, int hubControllers, int svgByteBudget, int crossingCeiling, int collisionCeiling) {
        var options = new TopologyRenderOptions { IncludeLegend = false, NodeDisplayMode = TopologyNodeDisplayMode.Tile };
        var chart = ReplicationTopologyFixture.Create(branchSites, hubControllers);
        var expectedControllers = ReplicationTopologyFixture.DomainControllerCount(branchSites, hubControllers);
        ReplicationTopologyFixture.Create(1, 2).Prepare(options).ToPng();

        var stopwatch = Stopwatch.StartNew();
        var prepared = chart.Prepare(options);
        var prepareMs = stopwatch.ElapsedMilliseconds;
        stopwatch.Restart();
        var svg = prepared.ToSvg();
        var svgMs = stopwatch.ElapsedMilliseconds;
        stopwatch.Restart();
        var png = prepared.ToPng();
        var pngMs = stopwatch.ElapsedMilliseconds;
        var report = prepared.Analyze();
        var crossings = ReplicationTopologyFixture.NodeCardCrossings(report);
        var collisions = report.Collisions.Count(collision => collision.Kind == "node-node");
        _output.WriteLine($"dcs={expectedControllers} edges={prepared.EdgeCount} prepare={prepareMs}ms svg={svgMs}ms/{svg.Length}B png={pngMs}ms/{png.Length}B size={prepared.Width}x{prepared.Height} cardCrossings={crossings} nodeCollisions={collisions}");
        SaveSample(svg, png, expectedControllers);

        Assert.Equal(expectedControllers, prepared.NodeCount);
        Assert.InRange(expectedControllers, 70, 150);
        Assert.Equal(chart.Edges.Count, prepared.EdgeCount);
        Assert.Equal(new HashSet<string>(chart.Edges.Select(edge => edge.Id)), RenderedEdgeIds(svg));
        Assert.True(png.Length > 10_000, "The PNG export should contain the rendered topology.");
        Assert.True(prepareMs + svgMs + pngMs <= RenderTimeBudgetMilliseconds, $"Layout plus SVG and PNG export took {prepareMs + svgMs + pngMs} ms; budget {RenderTimeBudgetMilliseconds} ms.");
        Assert.True(svg.Length <= svgByteBudget, $"SVG is {svg.Length} bytes; budget {svgByteBudget}.");
        Assert.Equal(0, report.Edges.Sum(edge => edge.LabelObstacleHits));
        Assert.True(crossings <= crossingCeiling, $"Routes cross node cards {crossings} times; ceiling {crossingCeiling}.");
        Assert.True(collisions <= collisionCeiling, $"Node cards overlap {collisions} times; ceiling {collisionCeiling}.");
    }

    [Fact]
    public void DenseReplicationFixture_RendersDeterministically() {
        var options = new TopologyRenderOptions { IncludeLegend = false, NodeDisplayMode = TopologyNodeDisplayMode.Tile };
        var first = ReplicationTopologyFixture.Create(3, 10).Prepare(options).ToSvg();
        var second = ReplicationTopologyFixture.Create(3, 10).Prepare(options).ToSvg();
        Assert.Equal(first, second);
    }

    [Fact]
    public void NodeCardCrossings_CountsEachEdgeAndForeignCardOnce() {
        Assert.Equal(1, Crossings(TopologyEdgeRouting.Straight, 270, 80));
        Assert.Equal(0, Crossings(TopologyEdgeRouting.Straight, 270, 121));
        Assert.Equal(0, Crossings(TopologyEdgeRouting.Straight, 270, 101));
    }

    private static int Crossings(TopologyEdgeRouting routing, double middleX, double middleY) {
        var chart = TopologyChart.Create().WithId("crossing").WithViewport(600, 240, 0).WithLegend(null)
            .AddNode("a", "A", 10, 80, width: 60, height: 40)
            .AddNode("b", "B", middleX, middleY, width: 60, height: 40)
            .AddNode("c", "C", 530, 80, width: 60, height: 40)
            .AddEdge("a-c", "a", "c", routing: routing);
        return ReplicationTopologyFixture.NodeCardCrossings(chart.Prepare(new TopologyRenderOptions { IncludeLegend = false }).Analyze());
    }

    private void SaveSample(string svg, byte[] png, int controllers) {
        var directory = Environment.GetEnvironmentVariable("CFX_TOPOLOGY_FIXTURE_OUTPUT");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        var name = "replication-" + controllers.ToString(System.Globalization.CultureInfo.InvariantCulture) + "-dcs";
        File.WriteAllText(Path.Combine(directory, name + ".svg"), svg);
        File.WriteAllBytes(Path.Combine(directory, name + ".png"), png);
        _output.WriteLine("saved " + Path.Combine(directory, name + ".png"));
    }

    private static HashSet<string> RenderedEdgeIds(string svg) =>
        new(System.Text.RegularExpressions.Regex.Matches(svg, "data-edge-id=\"([^\"]+)\"").Cast<System.Text.RegularExpressions.Match>().Select(match => match.Groups[1].Value));
}

/// <summary>Runs the replication budget tests without parallel neighbours so timing budgets stay meaningful.</summary>
[CollectionDefinition(nameof(TopologyReplicationBudgetCollection), DisableParallelization = true)]
public sealed class TopologyReplicationBudgetCollection {
}
