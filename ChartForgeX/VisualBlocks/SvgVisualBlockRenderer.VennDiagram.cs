using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;
using ChartForgeX.Themes;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgVisualBlockRenderer {
    private static void RenderVennDiagram(SvgMarkupWriter writer, VennDiagramBlock block) {
        var options = block.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        RenderBlockHeading(writer, block, ref y, content.X, content.Width);
        var body = new ChartRect(content.X, y, content.Width, Math.Max(1, options.Size.Height - options.Padding.Bottom - y));
        var placements = VisualBlockRendering.VennSetPlacements(block, body);

        foreach (var placement in placements) {
            var accent = placement.Set.Stroke ?? VisualBlockRendering.PaletteAt(theme, placement.Index);
            var fill = (placement.Set.Fill ?? accent).WithAlpha(70);
            writer.StartElement("circle")
                .Attribute("data-cfx-role", "venn-set")
                .Attribute("data-set-id", placement.Set.Id)
                .Attribute("cx", F(placement.X))
                .Attribute("cy", F(placement.Y))
                .Attribute("r", F(placement.Radius))
                .Attribute("fill", fill.ToCss())
                .Attribute("stroke", accent.ToCss())
                .Attribute("stroke-width", 2)
                .EndEmptyElement()
                .Line();
        }

        foreach (var placement in placements) {
            var labelY = placement.Y - placement.Radius * 0.52;
            var style = VisualBlockRendering.FitBodyTextStyle(theme, placement.Set.Label, 13.5, placement.Radius * 1.16, placement.Radius * .24, 800);
            WriteText(writer, placement.Set.Label, placement.X - placement.Radius * 0.58, labelY, placement.Radius * 1.16, TextAlignment.Center, placement.Set.TextColor ?? theme.Text, theme.FontFamily, style.FontSize, "800");
        }

        foreach (var intersection in block.Intersections) WriteVennIntersectionStyle(writer, placements, intersection);
        foreach (var intersection in block.Intersections) WriteVennRegionText(writer, placements, intersection.SetIds, intersection.Label, intersection.TextColor ?? theme.Text, theme, 12.5, "700");
        foreach (var textNode in block.TextNodes) WriteVennRegionText(writer, placements, textNode.SetIds, textNode.Label, textNode.TextColor ?? theme.MutedText, theme, 11.5, "600");
    }

    private static void WriteVennIntersectionStyle(SvgMarkupWriter writer, IReadOnlyList<VisualBlockRendering.VennSetPlacement> placements, VennIntersection intersection) {
        if (!intersection.Fill.HasValue && !intersection.Stroke.HasValue) return;
        var center = VisualBlockRendering.VennRegionCenter(placements, intersection.SetIds);
        var radius = Math.Max(18, placements[0].Radius * (intersection.SetIds.Count == 2 ? 0.34 : 0.24));
        writer.StartElement("circle")
            .Attribute("data-cfx-role", "venn-intersection-style")
            .Attribute("data-set-ids", string.Join(",", intersection.SetIds))
            .Attribute("cx", F(center.X))
            .Attribute("cy", F(center.Y))
            .Attribute("r", F(radius))
            .Attribute("fill", (intersection.Fill ?? ChartColor.Transparent).WithAlpha(intersection.Fill.HasValue ? (byte)88 : (byte)0).ToCss())
            .Attribute("stroke", (intersection.Stroke ?? intersection.Fill ?? ChartColor.Transparent).ToCss())
            .Attribute("stroke-width", intersection.Stroke.HasValue ? 2 : 0)
            .EndEmptyElement()
            .Line();
    }

    private static void WriteVennRegionText(SvgMarkupWriter writer, IReadOnlyList<VisualBlockRendering.VennSetPlacement> placements, IReadOnlyList<string> setIds, string label, ChartColor color, ChartTheme theme, double defaultSize, string weight) {
        if (label.Length == 0) return;
        var center = VisualBlockRendering.VennRegionCenter(placements, setIds);
        var maxWidth = Math.Max(42, placements[0].Radius * (setIds.Count == 1 ? 0.95 : 0.72));
        var yOffset = setIds.Count == 1 ? placements[0].Radius * 0.12 : setIds.Count == 2 ? 5 : 18;
        var style = VisualBlockRendering.FitBodyTextStyle(theme, label, defaultSize, maxWidth, placements[0].Radius * .24, int.Parse(weight, System.Globalization.CultureInfo.InvariantCulture));
        WriteText(writer, label, center.X - maxWidth / 2, center.Y + yOffset, maxWidth, TextAlignment.Center, color, theme.FontFamily, style.FontSize, weight);
    }
}
