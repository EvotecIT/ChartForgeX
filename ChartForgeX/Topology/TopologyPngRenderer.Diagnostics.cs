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
        foreach (var group in report.Groups) DrawDiagnosticBounds(canvas, group.Bounds, groupColor, 1, dashed: true);
        foreach (var node in report.Nodes) {
            DrawDiagnosticBounds(canvas, node.Bounds, nodeColor, 1, dashed: true);
            foreach (var port in node.Ports) canvas.DrawCircle(port.Position.X, port.Position.Y, 4, portColor);
        }
        foreach (var edge in report.Edges) foreach (var point in edge.Points) canvas.DrawCircle(point.X, point.Y, 2.5, routeColor);
        foreach (var collision in report.Collisions) DrawDiagnosticBounds(canvas, collision.Bounds, collisionColor, 2, dashed: false);
    }

    private static void DrawDiagnosticBounds(RgbaCanvas canvas, ChartRect bounds, ChartColor color, double width, bool dashed) {
        if (!dashed) {
            canvas.StrokeRect(bounds.X, bounds.Y, bounds.Width, bounds.Height, color, width);
            return;
        }

        // One dash pattern runs around the whole outline, as the SVG rect's stroke-dasharray does.
        var outline = new[] { new ChartPoint(bounds.X, bounds.Y), new ChartPoint(bounds.Right, bounds.Y), new ChartPoint(bounds.Right, bounds.Bottom), new ChartPoint(bounds.X, bounds.Bottom), new ChartPoint(bounds.X, bounds.Y) };
        canvas.DrawPolyline(outline, color, width, RasterLineCap.Butt, RasterLineJoin.Miter, RgbaCanvas.DashPattern(5, 4));
    }
}
