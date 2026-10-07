using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>
/// The queue and reusable buffers behind the dense route search. Routes depend on the exact order states leave the
/// queue, so the queue must pop by priority and then by state index, whatever the push order, and reused buffers must
/// start every grid without blocks or occupancy left by an earlier one.
/// </summary>
[Collection(nameof(TopologyDensePlanCacheTests))]
public sealed class TopologyDenseRouteSearchStructureTests {
    [Fact]
    public void MinHeap_InterleavedPushesAndPops_PopsByPriorityThenState() {
        var random = new Random(20261007);
        var heap = new TopologyDenseRoutePlanner.MinHeap();
        var reference = new List<(double Priority, int State, double Cost)>();
        for (var round = 0; round < 4000; round++) {
            if (reference.Count == 0 || random.Next(3) != 0) {
                // Few distinct priorities and states, so ties on priority and on both keys are common.
                var priority = random.Next(40) * 0.5;
                var state = random.Next(64);
                var cost = random.NextDouble();
                heap.Push(state, priority, cost);
                reference.Add((priority, state, cost));
                continue;
            }

            heap.Pop(out var poppedState, out var poppedPriority, out var poppedCost);
            var expected = reference.OrderBy(item => item.Priority).ThenBy(item => item.State).First();
            Assert.Equal(expected.Priority, poppedPriority);
            Assert.Equal(expected.State, poppedState);
            // Equal keys belong to one state; any of their entries may come first, but the entry must have been pushed.
            var index = reference.FindIndex(item => item.Priority == poppedPriority && item.State == poppedState && item.Cost == poppedCost);
            Assert.True(index >= 0);
            reference.RemoveAt(index);
            Assert.Equal(reference.Count, heap.Count);
        }
    }

    [Fact]
    public void MinHeap_ClearAfterGrowth_StartsEmptyAndKeepsOrdering() {
        var heap = new TopologyDenseRoutePlanner.MinHeap();
        for (var i = 1000; i > 0; i--) heap.Push(i, i % 7, i);
        heap.Clear();
        Assert.Equal(0, heap.Count);
        heap.Push(5, 2, 0);
        heap.Push(3, 2, 0);
        heap.Push(9, 1, 0);
        var order = new List<int>();
        while (heap.Count > 0) {
            heap.Pop(out var state, out _, out _);
            order.Add(state);
        }

        Assert.Equal(new[] { 9, 3, 5 }, order);
    }

    [Fact]
    public void GridBuffers_ReusedForSmallerGrid_ClearsBlocksAndOccupancyAndKeepsStampsCounting() {
        var buffers = new TopologyDenseRoutePlanner.GridBuffers();
        var first = new object();
        buffers.Prepare(first, 100);
        var flags = buffers.Flags;
        var nodes = buffers.Nodes;
        Assert.True(flags.Length >= 100);
        Assert.True(nodes.Length >= 400);
        for (var i = 0; i < 100; i++) {
            buffers.Flags[i] = 31;
            buffers.UsedRight[i] = buffers.UsedDown[i] = buffers.CrossRight[i] = buffers.CrossDown[i] = 7;
        }

        buffers.Search = 12;
        var second = new object();
        buffers.Prepare(second, 60);
        Assert.Same(second, buffers.Owner);
        Assert.Same(flags, buffers.Flags);
        Assert.Same(nodes, buffers.Nodes);
        Assert.All(Enumerable.Range(0, 60), i => {
            Assert.Equal(0, buffers.Flags[i]);
            Assert.Equal(0, buffers.UsedRight[i] + buffers.UsedDown[i] + buffers.CrossRight[i] + buffers.CrossDown[i]);
        });
        // Search stamps are never reset, so entries written by searches of the earlier grid can never look current.
        Assert.Equal(12, buffers.Search);

        buffers.Prepare(new object(), 500);
        Assert.True(buffers.Flags.Length >= 500);
        Assert.True(buffers.Nodes.Length >= 2000);
        Assert.Equal(12, buffers.Search);
    }

    [Theory]
    [InlineData("replication-sites-60")]
    [InlineData("mesh")]
    public void DensePlan_PreparedTwice_RoutesAreIdentical(string fixture) {
        // The plan cache is cleared before each prepare, so both run the search (with reused buffers) from scratch.
        var (firstChart, options) = TopologyRouteQualityTests.Build(fixture);
        var (secondChart, _) = TopologyRouteQualityTests.Build(fixture);
        TopologyDenseRoutePlanner.ClearPlanCache();
        var first = firstChart.Prepare(options).Analyze();
        TopologyDenseRoutePlanner.ClearPlanCache();
        Assert.Equal(0, TopologyDenseRoutePlanner.CachedPlanCount);
        var second = secondChart.Prepare(options).Analyze();
        Assert.Equal(first.Edges.Count, second.Edges.Count);
        for (var i = 0; i < first.Edges.Count; i++) Assert.Equal(first.Edges[i].Points, second.Edges[i].Points);
    }
}
