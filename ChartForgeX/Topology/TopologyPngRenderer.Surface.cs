using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
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
        for (var gx = x + spacing; gx < right - 2; gx += spacing) canvas.DrawLine(gx, y + 10, gx, bottom - 10, WithAlpha(Color(theme.Border), 72), 0.75, RasterLineCap.Butt);
        for (var gy = y + spacing; gy < bottom - 2; gy += spacing) canvas.DrawLine(x + 10, gy, right - 10, gy, WithAlpha(Color(theme.Border), 56), 0.75, RasterLineCap.Butt);
    }

    private static void DrawArrow(RgbaCanvas canvas, ChartPoint from, ChartPoint to, ChartColor color, TopologyRenderOptions options) {
        if (options.ArrowMarkerStyle == TopologyArrowMarkerStyle.Circle) {
            var center = TopologyArrowGeometry.Project(new ChartPoint(5, 5), from, to, options);
            canvas.DrawCircle(center.X, center.Y, 3.4 * TopologyArrowGeometry.Extent(options) / 10, color);
            return;
        }
        foreach (var subpath in ChartMapPathParser.ParseSubpaths(TopologyArrowGeometry.Path(options.ArrowMarkerStyle), canvas.DeviceScale)) {
            var points = new ChartPoint[subpath.Points.Count];
            for (var i = 0; i < points.Length; i++) points[i] = TopologyArrowGeometry.Project(subpath.Points[i], from, to, options);
            if (options.ArrowMarkerStyle == TopologyArrowMarkerStyle.Chevron)
                canvas.DrawPolyline(points, color, 1.85 * TopologyArrowGeometry.Extent(options) / 10, RasterLineCap.Round, RasterLineJoin.Round, null);
            else canvas.FillPolygon(points, color);
        }
    }

    private static void DrawEndpointMarker(RgbaCanvas canvas, ChartPoint from, ChartPoint to, ChartColor color, TopologyMarkerKind kind, TopologyRenderOptions options, ChartColor background) {
        if (TopologyEndpointGeometry.Path(kind) is string endpointPath) {
            foreach (var subpath in ChartMapPathParser.ParseSubpaths(endpointPath, canvas.DeviceScale)) {
                var points = new ChartPoint[subpath.Points.Count];
                for (var i = 0; i < points.Length; i++) points[i] = TopologyEndpointGeometry.Project(subpath.Points[i], from, to);
                if (TopologyEndpointGeometry.IsClosed(kind)) canvas.FillPolygon(points, background);
                var stroke = new System.Collections.Generic.List<ChartPoint>(points);
                if (subpath.IsClosed && stroke.Count > 0) stroke.Add(stroke[0]);
                canvas.DrawPolyline(stroke, color, 1.5, RasterLineCap.Butt, RasterLineJoin.Round, null);
            }
            if (TopologyEndpointGeometry.HasCircle(kind)) {
                var center = TopologyEndpointGeometry.Project(new ChartPoint(5, 6), from, to);
                canvas.DrawCircle(center.X, center.Y, 3.5, background);
                canvas.DrawCircleOutline(center.X, center.Y, 3.5, color, 1.5);
            }
            return;
        }
        switch (kind) {
            case TopologyMarkerKind.Arrow:
                DrawArrow(canvas, from, to, color, options);
                break;
            case TopologyMarkerKind.Circle:
                canvas.DrawCircle(to.X, to.Y, 4, color);
                break;
            case TopologyMarkerKind.Diamond:
                var angle = Math.Atan2(to.Y - from.Y, to.X - from.X);
                const double radius = 6;
                var forward = new ChartPoint(to.X + Math.Cos(angle) * radius, to.Y + Math.Sin(angle) * radius);
                var backward = new ChartPoint(to.X - Math.Cos(angle) * radius, to.Y - Math.Sin(angle) * radius);
                var left = new ChartPoint(to.X + Math.Cos(angle + Math.PI / 2) * radius * 0.72, to.Y + Math.Sin(angle + Math.PI / 2) * radius * 0.72);
                var right = new ChartPoint(to.X + Math.Cos(angle - Math.PI / 2) * radius * 0.72, to.Y + Math.Sin(angle - Math.PI / 2) * radius * 0.72);
                canvas.FillPolygon(new[] { forward, left, backward, right }, color);
                break;
        }
    }
}
