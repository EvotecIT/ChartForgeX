using ChartForgeX.Topology;
using ChartForgeX.Primitives;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyRegionExampleRoutingTests {
    [Theory]
    [InlineData(1440, false)]
    [InlineData(390, true)]
    public void RegionRelationshipsDoNotCrossOtherNodeCores(int width, bool dark) {
        var chart = TopologyHierarchyExamples.CreateTopology(width, dark);
        var prepared = chart.Prepare(new TopologyRenderOptions {
            ReadableDenseLayout = true, IncludeTileSubtitles = true, IncludeIconLabels = true,
            IncludeEdgeLabelLeaders = true, WrapNodeLabels = true, NodeSurfaceStyle = TopologyNodeSurfaceStyle.Card
        });
        var envelope = prepared.ToInterchangeEnvelope();
        Assert.Equal(5, envelope.Nodes.Count);
        Assert.Equal(6, envelope.Edges.Count);
        Assert.All(envelope.Edges.SelectMany(edge => edge.ResolvedRoute), point => {
            Assert.InRange(point.X, 0, prepared.Width);
            Assert.InRange(point.Y, 0, prepared.Height);
        });
        foreach (var edge in envelope.Edges.Where(edge => edge.Id != "overlay")) {
            foreach (var node in envelope.Nodes.Where(node => node.Id != edge.SourceId && node.Id != edge.TargetId)) {
                for (var i = 1; i < edge.ResolvedRoute.Count; i++) {
                    var a = edge.ResolvedRoute[i - 1]; var b = edge.ResolvedRoute[i];
                    var cx = node.X!.Value + node.Width!.Value / 2; var cy = node.Y!.Value + node.Height!.Value / 2;
                    var dx = b.X - a.X; var dy = b.Y - a.Y;
                    var fraction = Math.Clamp(((cx - a.X) * dx + (cy - a.Y) * dy) / Math.Max(1, dx * dx + dy * dy), 0, 1);
                    Assert.True(Math.Sqrt(Math.Pow(cx - a.X - dx * fraction, 2) + Math.Pow(cy - a.Y - dy * fraction, 2)) >= node.Width / 2, edge.Id + " crosses " + node.Id);
                }
            }
        }
    }
}
