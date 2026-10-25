using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyRegionExampleRoutingTests {
    [Theory]
    [InlineData(1440, false)]
    [InlineData(390, true)]
    public void RegionRelationshipsDoNotCrossTheirOwnOrOtherNodeCaptions(int width, bool dark) {
        var chart = TopologyHierarchyExamples.CreateTopology(width, dark);
        var prepared = chart.Prepare(new TopologyRenderOptions {
            ReadableDenseLayout = true, IncludeTileSubtitles = true, IncludeIconLabels = true,
            IncludeEdgeLabelLeaders = true, WrapNodeLabels = true, NodeSurfaceStyle = TopologyNodeSurfaceStyle.Card
        });
        var diagnostics = prepared.Analyze();
        Assert.Equal(4, diagnostics.Nodes.Count);
        Assert.Equal(3, diagnostics.Edges.Count);
        Assert.DoesNotContain(diagnostics.RouteCrossings, crossing => crossing.Kind == TopologyLayoutRouteCrossingKind.NodeCaption);
        Assert.All(diagnostics.Edges, edge => Assert.True(edge.SourceAttached && edge.TargetAttached, edge.Id));
    }
}
