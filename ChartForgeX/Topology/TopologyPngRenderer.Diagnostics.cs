using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.Topology;

public sealed partial class TopologyPngRenderer {
    private static void DrawLayoutDiagnosticOverlay(RgbaCanvas canvas, TopologyChart chart, TopologyRenderOptions options) {
        var report = TopologyLayoutDiagnostics.AnalyzePrepared(chart, options);
        var groupColor = ChartColor.FromRgba(124, 58, 237, 210);
        var nodeColor = ChartColor.FromRgba(2, 132, 199, 220);
        var portColor = ChartColor.FromRgba(245, 158, 11, 235);
        var routeColor = ChartColor.FromRgba(16, 185, 129, 220);
        var collisionColor = ChartColor.FromRgba(220, 38, 38, 235);
        foreach (var group in report.Groups) canvas.StrokeRect(group.Bounds.X, group.Bounds.Y, group.Bounds.Width, group.Bounds.Height, groupColor, 1);
        foreach (var node in report.Nodes) {
            canvas.StrokeRect(node.Bounds.X, node.Bounds.Y, node.Bounds.Width, node.Bounds.Height, nodeColor, 1);
            foreach (var port in node.Ports) canvas.DrawCircle(port.Position.X, port.Position.Y, 4, portColor);
        }
        foreach (var edge in report.Edges) foreach (var point in edge.Points) canvas.DrawCircle(point.X, point.Y, 2.5, routeColor);
        foreach (var collision in report.Collisions) canvas.StrokeRect(collision.Bounds.X, collision.Bounds.Y, collision.Bounds.Width, collision.Bounds.Height, collisionColor, 2);
    }
}
