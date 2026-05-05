using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.Topology;

/// <summary>
/// Renders topology charts to dependency-free PNG images.
/// </summary>
public sealed class TopologyPngRenderer {
    /// <summary>
    /// Renders a topology chart to PNG bytes.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="options">Optional render options.</param>
    /// <returns>A PNG image.</returns>
    public byte[] Render(TopologyChart chart, TopologyRenderOptions? options = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        options ??= new TopologyRenderOptions();
        var prepared = TopologyLayoutEngine.Prepare(chart, options.View);
        var validation = new TopologyChartValidator().Validate(prepared);
        if (!validation.IsValid) throw new TopologyValidationException(validation);

        var width = (int)Math.Ceiling(prepared.Viewport.Width);
        var height = (int)Math.Ceiling(prepared.Viewport.Height);
        var theme = prepared.Theme ?? TopologyTheme.Light();
        var canvas = new RgbaCanvas(width, height, Math.Max(1, options.PngSupersamplingScale), null, Math.Max(1, options.PngOutputScale));
        canvas.Clear(Color(theme.Background));
        if (options.IncludeTitle) DrawHeader(canvas, prepared, theme);
        DrawGroups(canvas, prepared, theme);
        DrawEdges(canvas, prepared, theme);
        DrawEdgeLabels(canvas, prepared, theme);
        DrawNodes(canvas, prepared, theme);
        DrawStatusBadges(canvas, prepared, theme);
        if (options.IncludeLegend && prepared.Legend != null) DrawLegend(canvas, prepared, theme);
        return PngWriter.WriteRgba(canvas.OutputWidth, canvas.OutputHeight, canvas.ToOutputPixels());
    }

    private static void DrawHeader(RgbaCanvas canvas, TopologyChart chart, TopologyTheme theme) {
        if (string.IsNullOrWhiteSpace(chart.Title) && string.IsNullOrWhiteSpace(chart.Subtitle)) return;
        var x = chart.Viewport.Padding;
        var y = chart.Viewport.Padding + 8;
        if (!string.IsNullOrWhiteSpace(chart.Title)) canvas.DrawTextEmphasized(x, y, chart.Title!, Color(theme.Foreground), 22);
        if (!string.IsNullOrWhiteSpace(chart.Subtitle)) canvas.DrawText(x, y + 27, chart.Subtitle!, Color(theme.MutedForeground), 13);
    }

    private static void DrawGroups(RgbaCanvas canvas, TopologyChart chart, TopologyTheme theme) {
        foreach (var group in chart.Groups) {
            var status = Color(theme.StatusColor(group.Status));
            canvas.FillRoundedRect(group.X, group.Y, group.Width, group.Height, 16, Tint(status));
            canvas.StrokeRoundedRect(group.X, group.Y, group.Width, group.Height, 16, WithAlpha(status, 170), 1.2);
            canvas.DrawTextEmphasized(group.X + 24, group.Y + 16, group.Label, status, 16);
            if (!string.IsNullOrWhiteSpace(group.Subtitle)) canvas.DrawText(group.X + 24, group.Y + 38, group.Subtitle!, Color(theme.MutedForeground), 12);
        }
    }

    private static void DrawEdges(RgbaCanvas canvas, TopologyChart chart, TopologyTheme theme) {
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        foreach (var edge in chart.Edges) {
            var source = nodes[edge.SourceNodeId];
            var target = nodes[edge.TargetNodeId];
            var points = EdgePoints(source, target, edge.Routing);
            var color = Color(theme.StatusColor(edge.Status));
            for (var i = 0; i < points.Count - 1; i++) {
                if (edge.Status is TopologyHealthStatus.Warning or TopologyHealthStatus.Critical or TopologyHealthStatus.Unknown or TopologyHealthStatus.Disabled) {
                    canvas.DrawDashedLine(points[i].X, points[i].Y, points[i + 1].X, points[i + 1].Y, color, 2.2, 8, 5);
                } else {
                    canvas.DrawLine(points[i].X, points[i].Y, points[i + 1].X, points[i + 1].Y, color, 2.2);
                }
            }

            if (edge.Direction is TopologyDirection.Forward or TopologyDirection.Bidirectional) DrawArrow(canvas, points[points.Count - 2], points[points.Count - 1], color);
            if (edge.Direction is TopologyDirection.Backward or TopologyDirection.Bidirectional) DrawArrow(canvas, points[1], points[0], color);
        }
    }

    private static void DrawEdgeLabels(RgbaCanvas canvas, TopologyChart chart, TopologyTheme theme) {
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        foreach (var edge in chart.Edges.Where(edge => !string.IsNullOrWhiteSpace(edge.Label) || !string.IsNullOrWhiteSpace(edge.SecondaryLabel))) {
            var source = nodes[edge.SourceNodeId];
            var target = nodes[edge.TargetNodeId];
            var cx = (CenterX(source) + CenterX(target)) / 2;
            var cy = (CenterY(source) + CenterY(target)) / 2;
            var label = edge.Label ?? string.Empty;
            var secondary = edge.SecondaryLabel ?? string.Empty;
            var boxWidth = Math.Max(48, Math.Max(label.Length, secondary.Length) * 7.2 + 18);
            var boxHeight = string.IsNullOrWhiteSpace(secondary) ? 22 : 38;
            canvas.FillRoundedRect(cx - boxWidth / 2, cy - boxHeight / 2, boxWidth, boxHeight, 9, Color(theme.Background));
            canvas.StrokeRoundedRect(cx - boxWidth / 2, cy - boxHeight / 2, boxWidth, boxHeight, 9, Color(theme.Border), 1);
            if (!string.IsNullOrWhiteSpace(label)) DrawCentered(canvas, cx, cy + (string.IsNullOrWhiteSpace(secondary) ? -7 : -13), label, Color(theme.StatusColor(edge.Status)), 12, true);
            if (!string.IsNullOrWhiteSpace(secondary)) DrawCentered(canvas, cx, cy + 3, secondary, Color(theme.MutedForeground), 10, false);
        }
    }

    private static void DrawNodes(RgbaCanvas canvas, TopologyChart chart, TopologyTheme theme) {
        foreach (var node in chart.Nodes) {
            var status = Color(theme.StatusColor(node.Status));
            canvas.FillRoundedRect(node.X + 2, node.Y + 5, node.Width, node.Height, 10, ChartColor.FromRgba(15, 23, 42, 18));
            canvas.FillRoundedRect(node.X, node.Y, node.Width, node.Height, 10, Color(theme.Card));
            canvas.StrokeRoundedRect(node.X, node.Y, node.Width, node.Height, 10, status, 1.5);
            DrawNodeIcon(canvas, node, status);
            canvas.DrawTextEmphasized(node.X + 42, node.Y + 17, TrimTo(node.Label, 18), Color(theme.Foreground), 12.5);
            if (!string.IsNullOrWhiteSpace(node.Subtitle)) canvas.DrawText(node.X + 42, node.Y + 37, TrimTo(node.Subtitle!, 18), Color(theme.MutedForeground), 10.5);
        }
    }

    private static void DrawNodeIcon(RgbaCanvas canvas, TopologyNode node, ChartColor status) {
        var cx = node.X + 22;
        var cy = node.Y + node.Height / 2;
        canvas.FillRoundedRect(cx - 11, cy - 11, 22, 22, 6, Tint(status));
        canvas.StrokeRoundedRect(cx - 11, cy - 11, 22, 22, 6, status, 1);
        DrawCentered(canvas, cx, cy - 6, KindGlyph(node.Kind), status, 8.5, true);
    }

    private static void DrawStatusBadges(RgbaCanvas canvas, TopologyChart chart, TopologyTheme theme) {
        foreach (var node in chart.Nodes) {
            var color = Color(theme.StatusColor(node.Status));
            var cx = node.X + node.Width - 11;
            var cy = node.Y + 11;
            canvas.DrawCircle(cx, cy, 9, Color(theme.Background));
            canvas.DrawCircle(cx, cy, 7, color);
            DrawCentered(canvas, cx, cy - 5, StatusGlyph(node.Status), ChartColor.White, 8, true);
        }
    }

    private static void DrawLegend(RgbaCanvas canvas, TopologyChart chart, TopologyTheme theme) {
        var legend = chart.Legend!;
        var x = chart.Viewport.Padding;
        var y = chart.Viewport.Height - chart.Viewport.Padding - 86;
        var width = Math.Min(560, chart.Viewport.Width - chart.Viewport.Padding * 2);
        canvas.FillRoundedRect(x, y, width, 86, 12, Color(theme.Card));
        canvas.StrokeRoundedRect(x, y, width, 86, 12, Color(theme.Border), 1);
        if (!string.IsNullOrWhiteSpace(legend.Title)) canvas.DrawTextEmphasized(x + 16, y + 11, legend.Title!, Color(theme.Foreground), 12);
        for (var i = 0; i < legend.Items.Count; i++) {
            var item = legend.Items[i];
            var col = i % 4;
            var row = i / 4;
            var itemX = x + 18 + col * 132;
            var itemY = y + 46 + row * 24;
            var color = Color(item.Color ?? (item.Status.HasValue ? theme.StatusColor(item.Status.Value) : theme.Accent));
            if (item.Kind == "edge") canvas.DrawDashedLine(itemX, itemY - 4, itemX + 24, itemY - 4, color, 2, 6, 4);
            else canvas.DrawCircle(itemX + 8, itemY - 5, 6, color);
            canvas.DrawText(itemX + 32, itemY - 14, item.Label, Color(theme.MutedForeground), 11);
        }
    }

    private static List<ChartPoint> EdgePoints(TopologyNode source, TopologyNode target, TopologyEdgeRouting routing) {
        var x1 = CenterX(source);
        var y1 = CenterY(source);
        var x2 = CenterX(target);
        var y2 = CenterY(target);
        if (routing != TopologyEdgeRouting.Orthogonal) return new List<ChartPoint> { new(x1, y1), new(x2, y2) };
        var mx = (x1 + x2) / 2;
        return new List<ChartPoint> { new(x1, y1), new(mx, y1), new(mx, y2), new(x2, y2) };
    }

    private static void DrawArrow(RgbaCanvas canvas, ChartPoint from, ChartPoint to, ChartColor color) {
        var angle = Math.Atan2(to.Y - from.Y, to.X - from.X);
        const double length = 10;
        const double spread = 0.52;
        var p1 = new ChartPoint(to.X, to.Y);
        var p2 = new ChartPoint(to.X - Math.Cos(angle - spread) * length, to.Y - Math.Sin(angle - spread) * length);
        var p3 = new ChartPoint(to.X - Math.Cos(angle + spread) * length, to.Y - Math.Sin(angle + spread) * length);
        canvas.FillPolygon(new[] { p1, p2, p3 }, color);
    }

    private static void DrawCentered(RgbaCanvas canvas, double centerX, double y, string text, ChartColor color, double fontSize, bool emphasized) {
        var width = emphasized ? RgbaCanvas.MeasureTextEmphasizedWidth(text, fontSize, null) : RgbaCanvas.MeasureTextWidth(text, fontSize, null);
        if (emphasized) canvas.DrawTextEmphasized(centerX - width / 2, y, text, color, fontSize);
        else canvas.DrawText(centerX - width / 2, y, text, color, fontSize);
    }

    private static double CenterX(TopologyNode node) => node.X + node.Width / 2;

    private static double CenterY(TopologyNode node) => node.Y + node.Height / 2;

    private static ChartColor Color(string value) => ChartColor.FromHex(value);

    private static ChartColor WithAlpha(ChartColor color, byte alpha) => ChartColor.FromRgba(color.R, color.G, color.B, alpha);

    private static ChartColor Tint(ChartColor color) => ChartColor.FromRgba(color.R, color.G, color.B, 26);

    private static string TrimTo(string value, int max) {
        if (value.Length <= max) return value;
        return value.Substring(0, Math.Max(0, max - 3)) + "...";
    }

    private static string KindGlyph(TopologyNodeKind kind) {
        return kind switch {
            TopologyNodeKind.HubSite => "H",
            TopologyNodeKind.BranchSite => "B",
            TopologyNodeKind.DomainController => "DC",
            TopologyNodeKind.BridgeheadServer => "BH",
            TopologyNodeKind.Subnet => "S",
            TopologyNodeKind.SubnetGroup => "SG",
            TopologyNodeKind.Gateway => "GW",
            TopologyNodeKind.Region => "R",
            _ => "N"
        };
    }

    private static string StatusGlyph(TopologyHealthStatus status) {
        return status switch {
            TopologyHealthStatus.Healthy => "OK",
            TopologyHealthStatus.Warning => "!",
            TopologyHealthStatus.Critical => "!",
            TopologyHealthStatus.Disabled => "-",
            _ => "?"
        };
    }
}
