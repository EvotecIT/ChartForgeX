using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeBurstShapeTests {
    [Theory]
    [InlineData(TopologyEdgePort.Top)]
    [InlineData(TopologyEdgePort.Right)]
    [InlineData(TopologyEdgePort.Bottom)]
    [InlineData(TopologyEdgePort.Left)]
    public void BurstSceneSvgAttachmentsAndCaptionAreaUseOneFiniteContour(TopologyEdgePort port) {
        var node = new TopologyNode { Id = "burst", Label = "Notice", X = 20.5, Y = 30.25, Width = 180, Height = 90,
            Shape = TopologyNodeShape.Burst, Kind = TopologyNodeKind.Generic, FitLabelToContent = true,
            PreserveDisplayModeSize = true, ShowStatusBadge = false };
        var part = Assert.Single(TopologyNodeShapeGeometry.SceneParts(node)); Assert.True(part.Fill); Assert.True(part.Close);
        Assert.Equal(24, part.Path.Commands.Count); Assert.All(part.Path.Commands.Skip(1), item => Assert.Equal(ChartPathCommandKind.LineTo, item.Kind));
        var contour = part.Path.Flatten(.1);
        var attachment = TopologyNodeShapeGeometry.BoundaryPoint(node, node.X + node.Width / 2, node.Y + node.Height / 2, port, 0);
        Assert.True(DistanceToContour(attachment, contour) < .0001);
        var route = new List<ChartPoint> { new(node.X + node.Width / 2, node.Y + node.Height / 2), new(node.X + node.Width + 100, node.Y + node.Height / 2) };
        TopologyNodeShapeGeometry.AttachRoute(node, route, 0, 1, 0, port, .5); Assert.True(DistanceToContour(route[0], contour) < .0001);
        var svgContour = Assert.Single(ChartMapPathParser.ParseSubpaths(TopologyNodeShapeGeometry.Path(node), .1));
        Assert.True(DistanceToContour(attachment, svgContour.Points) < .002);
        var caption = TopologyNodeShapeGeometry.CaptionBounds(node, 30);
        Assert.Equal(node.Width * .5, caption.Width); Assert.True(caption.Height <= node.Height * .5);
        Assert.True(caption.Left >= node.X + node.Width * .25 && caption.Right <= node.X + node.Width * .75);
        var chart = TopologyChart.Create().WithViewport(380, 230, 12).WithLegend(null); chart.Nodes.Add(node);
        var prepared = chart.Prepare(new TopologyRenderOptions { IncludeLegend = false, IncludeStatusBadges = false });
        Assert.Single(prepared.Visual.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "topology-node-surface");
        Assert.Contains("data-node-shape=\"Burst\"", prepared.ToSvg(), StringComparison.Ordinal); Assert.NotEmpty(prepared.ToPng());
        var facts = prepared.ToInterchangeEnvelope(); Assert.Equal(TopologyNodeShape.Burst, Assert.Single(facts.Nodes).Topology!.Shape);
        var json = facts.ToJson();
        using var parsed = JsonDocument.Parse(json); Assert.Equal("Burst", parsed.RootElement.GetProperty("nodes")[0].GetProperty("topology").GetProperty("shape").GetString());
        Assert.Equal(json, VisualArtifactInterchangeEnvelope.FromUtf8Json(facts.ToUtf8Json()).ToJson());
        Assert.Equal(TopologyNodeShape.Burst, TopologyLayoutEngine.Clone(chart).Nodes[0].Shape);
        facts.Nodes[0].Topology!.Shape = (TopologyNodeShape)int.MaxValue;
        Assert.Throws<ArgumentOutOfRangeException>(() => facts.ToJson());
    }
    private static double DistanceToContour(ChartPoint point, IReadOnlyList<ChartPoint> points) =>
        Enumerable.Range(0, points.Count).Min(index => Distance(point, points[index], points[(index + 1) % points.Count]));
    private static double Distance(ChartPoint point, ChartPoint start, ChartPoint end) {
        var dx = end.X - start.X; var dy = end.Y - start.Y; var length = dx * dx + dy * dy;
        var t = length == 0 ? 0 : Math.Max(0, Math.Min(1, ((point.X - start.X) * dx + (point.Y - start.Y) * dy) / length));
        var x = point.X - start.X - t * dx; var y = point.Y - start.Y - t * dy; return Math.Sqrt(x * x + y * y);
    }
}
