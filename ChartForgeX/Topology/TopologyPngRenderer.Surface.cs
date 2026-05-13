using System;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

public sealed partial class TopologyPngRenderer {
    private static void DrawCanvasSurface(RgbaCanvas canvas, TopologyChart chart, TopologyTheme theme, TopologyRenderOptions options) {
        if (!ShouldRenderCanvasSurface(chart, options)) return;
        var x = chart.Viewport.Padding;
        var y = options.IncludeTitle && (!string.IsNullOrWhiteSpace(chart.Title) || !string.IsNullOrWhiteSpace(chart.Subtitle)) ? chart.Viewport.Padding + 62 : chart.Viewport.Padding;
        var width = chart.Viewport.Width - chart.Viewport.Padding * 2;
        var height = chart.Viewport.Height - y - chart.Viewport.Padding - LegendReservedHeight(chart.Legend, chart.Viewport);
        if (width <= 0 || height <= 0) return;
        canvas.FillRoundedRect(x + 2, y + 5, width, height, 12, ChartColor.FromRgba(15, 23, 42, 12));
        canvas.FillRoundedRect(x, y, width, height, IsMonitoringDashboardStyle(options) ? 12 : 14, Color(theme.Card));
        canvas.StrokeRoundedRect(x, y, width, height, IsMonitoringDashboardStyle(options) ? 12 : 14, Color(theme.Border), 1);
        if (options.CanvasSurfaceStyle != TopologyCanvasSurfaceStyle.PanelGrid) return;
        const double spacing = 56;
        var right = x + width;
        var bottom = y + height;
        for (var gx = x + spacing; gx < right - 2; gx += spacing) canvas.DrawLine(gx, y + 10, gx, bottom - 10, WithAlpha(Color(theme.Border), 72), 0.7);
        for (var gy = y + spacing; gy < bottom - 2; gy += spacing) canvas.DrawLine(x + 10, gy, right - 10, gy, WithAlpha(Color(theme.Border), 56), 0.7);
    }

    private static void DrawArrow(RgbaCanvas canvas, ChartPoint from, ChartPoint to, ChartColor color, TopologyRenderOptions options) {
        var angle = Math.Atan2(to.Y - from.Y, to.X - from.X);
        const double length = 10;
        const double spread = 0.52;
        var p1 = new ChartPoint(to.X, to.Y);
        var p2 = new ChartPoint(to.X - Math.Cos(angle - spread) * length, to.Y - Math.Sin(angle - spread) * length);
        var p3 = new ChartPoint(to.X - Math.Cos(angle + spread) * length, to.Y - Math.Sin(angle + spread) * length);
        switch (options.ArrowMarkerStyle) {
            case TopologyArrowMarkerStyle.Chevron:
                canvas.DrawLine(p2.X, p2.Y, p1.X, p1.Y, color, 2);
                canvas.DrawLine(p3.X, p3.Y, p1.X, p1.Y, color, 2);
                break;
            case TopologyArrowMarkerStyle.Diamond:
                var p4 = new ChartPoint(to.X - Math.Cos(angle) * length * 1.4, to.Y - Math.Sin(angle) * length * 1.4);
                canvas.FillPolygon(new[] { p1, p2, p4, p3 }, color);
                break;
            case TopologyArrowMarkerStyle.Circle:
                canvas.DrawCircle(to.X, to.Y, 4, color);
                break;
            default:
                canvas.FillPolygon(new[] { p1, p2, p3 }, color);
                break;
        }
    }
}
