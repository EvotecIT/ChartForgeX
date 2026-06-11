using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.VisualBlocks;

public sealed partial class PngVisualBlockRenderer {
    private static void DrawWardleyMap(RgbaCanvas canvas, WardleyMapBlock map) {
        var options = map.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        DrawHeading(canvas, map, ref y, content.X, content.Width);
        var layout = VisualBlockRendering.BuildWardleyMapLayout(map, content, y, options.Padding.Bottom, options.Size.Height);
        DrawWardleyAxes(canvas, map, layout);
        foreach (var pipeline in map.Pipelines) DrawWardleyPipeline(canvas, pipeline, layout);
        foreach (var link in map.Links) DrawWardleyLink(canvas, link, layout);
        foreach (var evolution in map.Evolutions) DrawWardleyEvolution(canvas, evolution, layout);
        foreach (var note in map.Notes) DrawAlignedText(canvas, note.Text, VisualBlockRendering.ProjectWardleyX(layout.Plot, note.Evolution), VisualBlockRendering.ProjectWardleyY(layout.Plot, note.Visibility) - 8, 150, VisualTextAlignment.Left, theme.MutedText, Math.Max(8, theme.SubtitleFontSize - 2), true);
        foreach (var marker in map.Markers) DrawWardleyMarker(canvas, marker, layout);
        foreach (var node in layout.Nodes) DrawWardleyNode(canvas, node, map);
        foreach (var annotation in map.Annotations) DrawWardleyAnnotation(canvas, annotation, layout);
    }

    private static void DrawWardleyAxes(RgbaCanvas canvas, WardleyMapBlock map, VisualBlockRendering.WardleyMapLayout layout) {
        var theme = map.Options.Theme;
        var plot = layout.Plot;
        canvas.FillRect(plot.X, plot.Y, plot.Width, plot.Height, theme.PlotBackground);
        canvas.StrokeRect(plot.X, plot.Y, plot.Width, plot.Height, theme.PlotBorder, 1);
        for (var index = 1; index < 4; index++) {
            var x = plot.X + plot.Width * index / 4.0;
            canvas.DrawLine(x, plot.Y, x, plot.Bottom, theme.Grid, 1);
        }

        canvas.DrawLine(plot.X, plot.Bottom, plot.Right, plot.Bottom, theme.Axis, 1.4);
        canvas.DrawLine(plot.X, plot.Y, plot.X, plot.Bottom, theme.Axis, 1.4);
        DrawAlignedText(canvas, "Visibility", Math.Max(0, plot.X - 54), plot.Y + plot.Height * 0.50 - 8, 48, VisualTextAlignment.Center, theme.MutedText, Math.Max(9, theme.SubtitleFontSize - 1), true);
        DrawAlignedText(canvas, "Evolution", plot.X + plot.Width * 0.38, plot.Bottom + 28, plot.Width * 0.24, VisualTextAlignment.Center, theme.MutedText, Math.Max(9, theme.SubtitleFontSize - 1), true);
        var stages = map.Stages.Count == 0 ? new[] { "Genesis", "Custom", "Product", "Commodity" } : map.Stages;
        for (var index = 0; index < stages.Count; index++) {
            var x = plot.X + plot.Width * (index + 0.5) / stages.Count;
            DrawAlignedText(canvas, stages[index], x - plot.Width / stages.Count * 0.5 + 4, plot.Bottom + 10, plot.Width / stages.Count - 8, VisualTextAlignment.Center, theme.MutedText, Math.Max(8, theme.SubtitleFontSize - 2), true);
        }
    }

    private static void DrawWardleyLink(RgbaCanvas canvas, WardleyMapLink link, VisualBlockRendering.WardleyMapLayout layout) {
        if (!layout.NodeLookup.TryGetValue(link.FromId, out var from) || !layout.NodeLookup.TryGetValue(link.ToId, out var to)) return;
        var color = ApplyOpacity(ChartColor.Black, 0.44);
        if (link.Dashed) canvas.DrawDashedLine(from.X, from.Y, to.X, to.Y, color, 1.2, 5, 5);
        else canvas.DrawLine(from.X, from.Y, to.X, to.Y, color, 1.2);
        DrawWardleyFlowHint(canvas, link, from.X, from.Y, to.X, to.Y, color);
        if (link.Label.Length > 0) DrawAlignedText(canvas, link.Label, (from.X + to.X) / 2 - 50, (from.Y + to.Y) / 2 - 11, 100, VisualTextAlignment.Center, color, 8.5, true);
    }

    private static void DrawWardleyFlowHint(RgbaCanvas canvas, WardleyMapLink link, double fromX, double fromY, double toX, double toY, ChartColor color) {
        if (link.Flow == WardleyMapFlow.None) return;
        var dx = toX - fromX;
        var dy = toY - fromY;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length <= 0.000001) return;
        var ux = dx / length;
        var uy = dy / length;
        var midX = (fromX + toX) / 2;
        var midY = (fromY + toY) / 2;
        if (link.Flow == WardleyMapFlow.Forward || link.Flow == WardleyMapFlow.Bidirectional) DrawWardleyFlowArrow(canvas, midX + ux * 9, midY + uy * 9, ux, uy, color);
        if (link.Flow == WardleyMapFlow.Backward || link.Flow == WardleyMapFlow.Bidirectional) DrawWardleyFlowArrow(canvas, midX - ux * 9, midY - uy * 9, -ux, -uy, color);
    }

    private static void DrawWardleyFlowArrow(RgbaCanvas canvas, double centerX, double centerY, double ux, double uy, ChartColor color) {
        var px = -uy;
        var py = ux;
        canvas.FillPolygon(new[] {
            new ChartPoint(centerX + ux * 7, centerY + uy * 7),
            new ChartPoint(centerX - ux * 5 + px * 4, centerY - uy * 5 + py * 4),
            new ChartPoint(centerX - ux * 5 - px * 4, centerY - uy * 5 - py * 4)
        }, color);
    }

    private static void DrawWardleyEvolution(RgbaCanvas canvas, WardleyMapEvolution evolution, VisualBlockRendering.WardleyMapLayout layout) {
        if (!layout.NodeLookup.TryGetValue(evolution.NodeId, out var from)) return;
        var x = VisualBlockRendering.ProjectWardleyX(layout.Plot, evolution.TargetEvolution);
        var color = ChartColor.FromRgb(220, 53, 69);
        canvas.DrawLine(from.X, from.Y, x, from.Y, color, 1.8);
        canvas.DrawLine(x, from.Y, x - 7, from.Y - 4, color, 1.8);
        canvas.DrawLine(x, from.Y, x - 7, from.Y + 4, color, 1.8);
    }

    private static void DrawWardleyPipeline(RgbaCanvas canvas, WardleyMapPipeline pipeline, VisualBlockRendering.WardleyMapLayout layout) {
        if (!layout.NodeLookup.TryGetValue(pipeline.ParentId, out var parent) || pipeline.Components.Count == 0) return;
        var minX = parent.X;
        var maxX = parent.X;
        foreach (var child in pipeline.Components) {
            var x = VisualBlockRendering.ProjectWardleyX(layout.Plot, child.Evolution);
            minX = Math.Min(minX, x);
            maxX = Math.Max(maxX, x);
            canvas.DrawCircle(x, parent.Y, 4.5, ChartColor.Black);
            DrawAlignedText(canvas, child.Label, x + 6, parent.Y + 7, 82, VisualTextAlignment.Left, ChartColor.Black, 8, true);
        }

        canvas.StrokeRoundedRect(minX - 14, parent.Y - 22, maxX - minX + 28, 44, 7, ApplyOpacity(ChartColor.Black, 0.44), 1);
    }

    private static void DrawWardleyNode(RgbaCanvas canvas, VisualBlockRendering.WardleyNodePlacement placement, WardleyMapBlock map) {
        var theme = map.Options.Theme;
        var node = placement.Node;
        var color = node.Kind == WardleyMapNodeKind.Anchor ? theme.Text : VisualBlockRendering.PaletteAt(theme, placement.Index);
        var radius = node.Kind == WardleyMapNodeKind.Anchor ? 7.5 : 6;
        if (!string.IsNullOrWhiteSpace(node.Strategy)) canvas.DrawCircle(placement.X, placement.Y, radius + 7, ApplyOpacity(color, 0.32));
        canvas.DrawCircle(placement.X, placement.Y, radius + 2, theme.PlotBackground);
        canvas.DrawCircle(placement.X, placement.Y, radius, color);
        if (node.Inertia) canvas.DrawLine(placement.X + 16, placement.Y - 9, placement.X + 16, placement.Y + 9, theme.Text, 4);
        var labelX = placement.X + (node.LabelOffsetX ?? (node.Kind == WardleyMapNodeKind.Anchor ? -45 : 10));
        var labelY = placement.Y + (node.LabelOffsetY ?? -18);
        DrawAlignedText(canvas, node.Label, labelX, labelY, 120, node.Kind == WardleyMapNodeKind.Anchor ? VisualTextAlignment.Center : VisualTextAlignment.Left, theme.Text, Math.Max(8.5, theme.SubtitleFontSize - 2), true);
    }

    private static void DrawWardleyAnnotation(RgbaCanvas canvas, WardleyMapAnnotation annotation, VisualBlockRendering.WardleyMapLayout layout) {
        var x = VisualBlockRendering.ProjectWardleyX(layout.Plot, annotation.Evolution);
        var y = VisualBlockRendering.ProjectWardleyY(layout.Plot, annotation.Visibility);
        canvas.DrawCircle(x, y, 11, ChartColor.Black);
        canvas.DrawCircle(x, y, 9, ChartColor.White);
        DrawAlignedText(canvas, annotation.Number.ToString(System.Globalization.CultureInfo.InvariantCulture), x - 8, y - 6, 16, VisualTextAlignment.Center, ChartColor.Black, 9, true);
        if (annotation.Text.Length > 0) DrawAlignedText(canvas, annotation.Text, x + 13, y - 10, 150, VisualTextAlignment.Left, ChartColor.Black, 9, true);
    }

    private static void DrawWardleyMarker(RgbaCanvas canvas, WardleyMapMarker marker, VisualBlockRendering.WardleyMapLayout layout) {
        var x = VisualBlockRendering.ProjectWardleyX(layout.Plot, marker.Evolution);
        var y = VisualBlockRendering.ProjectWardleyY(layout.Plot, marker.Visibility);
        var color = marker.Kind == WardleyMapMarkerKind.Accelerator ? ChartColor.FromRgb(16, 185, 129) : ChartColor.FromRgb(239, 68, 68);
        if (marker.Kind == WardleyMapMarkerKind.Accelerator) {
            canvas.DrawLine(x, y, x + 44, y, color, 8);
            canvas.DrawLine(x + 44, y, x + 34, y - 9, color, 4);
            canvas.DrawLine(x + 44, y, x + 34, y + 9, color, 4);
        } else {
            canvas.DrawLine(x + 44, y, x, y, color, 8);
            canvas.DrawLine(x, y, x + 10, y - 9, color, 4);
            canvas.DrawLine(x, y, x + 10, y + 9, color, 4);
        }

        DrawAlignedText(canvas, marker.Label, x - 24, y + 14, 98, VisualTextAlignment.Center, ChartColor.Black, 9, true);
    }
}
