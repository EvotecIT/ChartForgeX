using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;
using ChartForgeX.Themes;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgVisualBlockRenderer {
    private static void RenderVennDiagram(SvgMarkupWriter writer, VennDiagramBlock block) {
        var options = block.Options;
        var theme = options.ResolvedTheme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        RenderBlockHeading(writer, block, ref y, content.X, content.Width);
        var body = new ChartRect(content.X, y, content.Width, Math.Max(1, options.Size.Height - options.Padding.Bottom - y));
        var placements = VisualBlockRendering.VennSetPlacements(block, body);
        var layout = VisualBlockRendering.VennLayout(block, body);
        writer.StartElement("g").Attribute("data-cfx-role", "venn-layout").Attribute("data-max-relative-area-error", layout.MaximumRelativeAreaError.ToString("R", System.Globalization.CultureInfo.InvariantCulture)).Attribute("data-area-accurate", layout.IsAreaAccurate ? "true" : "false").EndStartElement();

        foreach (var placement in placements) {
            var accent = placement.Set.Stroke ?? VisualBlockRendering.PaletteAt(theme, placement.Index);
            var fill = placement.Set.Fill ?? accent.WithAlpha(70);
            if (placement.Set.FillOpacity.HasValue) fill = fill.WithAlpha((byte)Math.Round(255 * placement.Set.FillOpacity.Value));
            writer.StartElement("circle")
                .Attribute("data-cfx-role", "venn-set")
                .Attribute("data-set-id", placement.Set.Id)
                .Attribute("cx", F(placement.X))
                .Attribute("cy", F(placement.Y))
                .Attribute("r", F(placement.Radius))
                .Attribute("fill", fill.ToCss())
                .Attribute("stroke", accent.ToCss())
                .Attribute("stroke-width", placement.Set.StrokeWidth)
                .EndEmptyElement()
                .Line();
        }

        for (var index = 0; index < block.Intersections.Count; index++) WriteVennIntersectionStyle(writer, block.Intersections[index], layout.Regions[index]);
        foreach (var caption in VisualBlockRendering.VennCaptions(block, placements, body))
            WriteText(writer, caption.Text, caption.Bounds.Left, caption.Baseline, caption.Bounds.Width, TextAlignment.Left, caption.Color, theme.FontFamily, caption.Style.FontSize, caption.Style.Font.Weight.ToString(System.Globalization.CultureInfo.InvariantCulture));
        writer.EndElement().Line();
    }

    private static void WriteVennIntersectionStyle(SvgMarkupWriter writer, VennIntersection intersection, VennRegionPlacement region) {
        if (region.Path.Length == 0) return;
        writer.StartElement("path")
            .Attribute("data-cfx-role", "venn-intersection")
            .Attribute("data-set-ids", string.Join(",", intersection.SetIds))
            .Attribute("data-requested-area", region.RequestedArea.ToString("R", System.Globalization.CultureInfo.InvariantCulture))
            .Attribute("data-actual-area", region.ActualArea.ToString("R", System.Globalization.CultureInfo.InvariantCulture))
            .Attribute("d", region.Path)
            .Attribute("fill", (intersection.FillOpacity.HasValue ? (intersection.Fill ?? ChartColor.Transparent).WithAlpha((byte)Math.Round(255 * intersection.FillOpacity.Value)) : intersection.Fill ?? ChartColor.Transparent).ToCss())
            .Attribute("stroke", (intersection.Stroke ?? ChartColor.Transparent).ToCss())
            .Attribute("stroke-width", intersection.Stroke.HasValue ? intersection.StrokeWidth : 0)
            .EndEmptyElement().Line();
    }
}
