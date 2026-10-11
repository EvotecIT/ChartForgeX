using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Themes;

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
            var style = VisualBlockRendering.FitBodyTextStyle(theme, placement.Set.Label, 13.5, placement.Radius * 1.16, placement.Radius * .24, 800);
            DrawAlignedTextAtBaseline(canvas, placement.Set.Label, placement.X - placement.Radius * .58, placement.Y - placement.Radius * .52, placement.Radius * 1.16, TextAlignment.Center, placement.Set.TextColor ?? theme.Text, style.FontSize, true);
        }

        foreach (var intersection in block.Intersections) DrawVennIntersectionStyle(canvas, placements, intersection);
        foreach (var intersection in block.Intersections) DrawVennRegionText(canvas, placements, intersection.SetIds, intersection.Label, intersection.TextColor ?? theme.Text, theme, 12.5, 700);
        foreach (var textNode in block.TextNodes) DrawVennRegionText(canvas, placements, textNode.SetIds, textNode.Label, textNode.TextColor ?? theme.MutedText, theme, 11.5, 600);
    }

    private static void DrawVennIntersectionStyle(RgbaCanvas canvas, IReadOnlyList<VisualBlockRendering.VennSetPlacement> placements, VennIntersection intersection) {
        if (!intersection.Fill.HasValue && !intersection.Stroke.HasValue) return;
        var center = VisualBlockRendering.VennRegionCenter(placements, intersection.SetIds);
        var radius = Math.Max(18, placements[0].Radius * (intersection.SetIds.Count == 2 ? 0.34 : 0.24));
        if (intersection.Fill.HasValue) canvas.DrawCircle(center.X, center.Y, radius, intersection.Fill.Value.WithAlpha(88));
        if (intersection.Stroke.HasValue) canvas.DrawCircleOutline(center.X, center.Y, radius, intersection.Stroke.Value, 2);
    }

    private static void DrawVennRegionText(RgbaCanvas canvas, IReadOnlyList<VisualBlockRendering.VennSetPlacement> placements, IReadOnlyList<string> setIds, string label, ChartColor color, ChartTheme theme, double defaultSize, int weight) {
        if (label.Length == 0) return;
        var center = VisualBlockRendering.VennRegionCenter(placements, setIds);
        var maxWidth = Math.Max(42, placements[0].Radius * (setIds.Count == 1 ? 0.95 : 0.72));
        var yOffset = setIds.Count == 1 ? placements[0].Radius * 0.12 : setIds.Count == 2 ? 5 : 18;
        var style = VisualBlockRendering.FitBodyTextStyle(theme, label, defaultSize, maxWidth, placements[0].Radius * .24, weight);
        DrawAlignedTextAtBaseline(canvas, label, center.X - maxWidth / 2, center.Y + yOffset, maxWidth, TextAlignment.Center, color, style.FontSize, true);
    }
}
