using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualBlocks;

public sealed partial class PngVisualBlockRenderer {
    private static void DrawVennDiagram(RgbaCanvas canvas, VennDiagramBlock block) {
        var options = block.Options;
        var theme = options.ResolvedTheme;
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

        foreach (var intersection in block.Intersections) DrawVennIntersectionStyle(canvas, placements, intersection);
        foreach (var caption in VisualBlockRendering.VennCaptions(block, placements, body)) {
            var style = caption.Style.Clone(); style.Color = caption.Color;
            canvas.DrawStyledText(caption.Bounds.Left, caption.Bounds.Top, caption.Bounds.Width, caption.Text,
                style, TextWrapMode.NoWrap, maximumLines: 1, trimming: TextTrimming.None);
        }
    }

    private static void DrawVennIntersectionStyle(RgbaCanvas canvas, IReadOnlyList<VisualBlockRendering.VennSetPlacement> placements, VennIntersection intersection) {
        if (!intersection.Fill.HasValue && !intersection.Stroke.HasValue) return;
        var center = VisualBlockRendering.VennRegionCenter(placements, intersection.SetIds);
        var radius = Math.Max(18, placements[0].Radius * (intersection.SetIds.Count == 2 ? 0.34 : 0.24));
        if (intersection.Fill.HasValue) canvas.DrawCircle(center.X, center.Y, radius, intersection.Fill.Value.WithAlpha(88));
        if (intersection.Stroke.HasValue) canvas.DrawCircleOutline(center.X, center.Y, radius, intersection.Stroke.Value, 2);
    }

}
