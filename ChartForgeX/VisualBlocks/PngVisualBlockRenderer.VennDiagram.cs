using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.VisualBlocks;

public sealed partial class PngVisualBlockRenderer {
    private static void DrawVennDiagram(RgbaCanvas canvas, VennDiagramBlock block) {
        var options = block.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        DrawHeading(canvas, block, ref y, content.X, content.Width);
        var body = new ChartRect(content.X, y, content.Width, Math.Max(1, options.Size.Height - options.Padding.Bottom - y));
        var placements = VisualBlockRendering.VennSetPlacements(block, body);

        foreach (var placement in placements) {
            var accent = placement.Set.Stroke ?? VisualBlockRendering.PaletteAt(theme, placement.Index);
            var fill = (placement.Set.Fill ?? accent).WithAlpha(70);
            canvas.DrawCircle(placement.X, placement.Y, placement.Radius, fill);
            canvas.DrawCircleOutline(placement.X, placement.Y, placement.Radius, accent, 2);
        }

        foreach (var placement in placements) {
            DrawCenteredTextMiddle(canvas, placement.Set.Label, placement.X, placement.Y - placement.Radius * 0.52, Math.Max(11, theme.SubtitleFontSize + 1), placement.Set.TextColor ?? theme.Text, true, placement.Radius * 1.16);
        }

        foreach (var intersection in block.Intersections) DrawVennIntersectionStyle(canvas, placements, intersection);
        foreach (var intersection in block.Intersections) DrawVennRegionText(canvas, placements, intersection.SetIds, intersection.Label, intersection.TextColor ?? theme.Text, Math.Max(10, theme.SubtitleFontSize), true);
        foreach (var textNode in block.TextNodes) DrawVennRegionText(canvas, placements, textNode.SetIds, textNode.Label, textNode.TextColor ?? theme.MutedText, Math.Max(9, theme.SubtitleFontSize - 1), true);
    }

    private static void DrawVennIntersectionStyle(RgbaCanvas canvas, IReadOnlyList<VisualBlockRendering.VennSetPlacement> placements, VennIntersection intersection) {
        if (!intersection.Fill.HasValue && !intersection.Stroke.HasValue) return;
        var center = VisualBlockRendering.VennRegionCenter(placements, intersection.SetIds);
        var radius = Math.Max(18, placements[0].Radius * (intersection.SetIds.Count == 2 ? 0.34 : 0.24));
        if (intersection.Fill.HasValue) canvas.DrawCircle(center.X, center.Y, radius, intersection.Fill.Value.WithAlpha(88));
        if (intersection.Stroke.HasValue) canvas.DrawCircleOutline(center.X, center.Y, radius, intersection.Stroke.Value, 2);
    }

    private static void DrawVennRegionText(RgbaCanvas canvas, IReadOnlyList<VisualBlockRendering.VennSetPlacement> placements, IReadOnlyList<string> setIds, string label, ChartColor color, double fontSize, bool emphasized) {
        if (label.Length == 0) return;
        var center = VisualBlockRendering.VennRegionCenter(placements, setIds);
        var maxWidth = Math.Max(42, placements[0].Radius * (setIds.Count == 1 ? 0.95 : 0.72));
        var yOffset = setIds.Count == 1 ? placements[0].Radius * 0.12 : setIds.Count == 2 ? 5 : 18;
        DrawCenteredTextMiddle(canvas, label, center.X, center.Y + yOffset, fontSize, color, emphasized, maxWidth);
    }
}
