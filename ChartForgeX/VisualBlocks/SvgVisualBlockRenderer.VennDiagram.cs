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

        foreach (var intersection in block.Intersections) WriteVennIntersectionStyle(writer, placements, intersection);
        foreach (var caption in VisualBlockRendering.VennCaptions(block, placements, body))
            WriteText(writer, caption.Text, caption.Bounds.Left, caption.Baseline, caption.Bounds.Width, TextAlignment.Left, caption.Color, theme.FontFamily, caption.Style.FontSize, caption.Style.Font.Weight.ToString(System.Globalization.CultureInfo.InvariantCulture));
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

}
