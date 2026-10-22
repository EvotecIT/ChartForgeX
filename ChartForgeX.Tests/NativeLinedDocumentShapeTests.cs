using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeLinedDocumentShapeTests {
    [Theory]
    [InlineData(TopologyEdgePort.Top)]
    [InlineData(TopologyEdgePort.Right)]
    [InlineData(TopologyEdgePort.Bottom)]
    [InlineData(TopologyEdgePort.Left)]
    public void DocumentInkCaptionAndAttachedRoutesShareTheSameNativeContour(TopologyEdgePort port) {
        var node = new TopologyNode { Id = "reference", Label = "Reference", X = 20.5, Y = 30.25,
            Width = 180, Height = 90, Shape = TopologyNodeShape.LinedDocument };
        var parts = TopologyNodeShapeGeometry.SceneParts(node);
        Assert.Equal(2, parts.Count);
        Assert.Contains(parts[0].Path.Commands, command => command.Kind == ChartPathCommandKind.CubicTo);
        var attachment = TopologyNodeShapeGeometry.BoundaryPoint(node, node.X + node.Width / 2, node.Y + node.Height / 2, port, 0);
        var flattened = parts[0].Path.Flatten(.5);
        Assert.True(flattened.Zip(flattened.Skip(1), (start, end) => Distance(attachment, start, end)).Append(Distance(attachment, flattened[flattened.Count - 1], flattened[0])).Min() < .05);
        var caption = TopologyNodeShapeGeometry.CaptionBounds(node, 30);
        Assert.True(caption.Left > node.X + 12);
        Assert.True(caption.Bottom < node.Y + node.Height - 10);
        var chart = TopologyChart.Create().WithViewport(380, 230, 12).WithLegend(null);
        chart.Nodes.Add(node);
        var prepared = chart.Prepare(new TopologyRenderOptions { IncludeLegend = false });
        Assert.Equal(2, prepared.Visual.Scene.Nodes.OfType<VisualScenePath>().Count(path => path.Role == "topology-node-surface"));
        Assert.NotEmpty(prepared.ToPng());
        Assert.Equal(TopologyNodeShape.LinedDocument, prepared.ToInterchangeEnvelope().Nodes.Single().Topology!.Shape);
    }
    private static double Distance(ChartPoint point, ChartPoint start, ChartPoint end) {
        var dx = end.X - start.X; var dy = end.Y - start.Y;
        var length = dx * dx + dy * dy;
        var t = length == 0 ? 0 : Math.Max(0, Math.Min(1, ((point.X - start.X) * dx + (point.Y - start.Y) * dy) / length));
        var x = point.X - start.X - t * dx; var y = point.Y - start.Y - t * dy;
        return Math.Sqrt(x * x + y * y);
    }
}
