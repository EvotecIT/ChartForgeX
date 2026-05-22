using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgVisualBlockRenderer {
    private static void RenderSegmentedMetricCapsuleLoop(SvgMarkupWriter writer, SegmentedMetricBlock card) {
        var options = card.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        RenderSegmentedMetricHeading(writer, card, ref y, content.X, content.Width);
        y += 8;

        var hasAction = card.ActionLabel.Length > 0;
        var layout = VisualBlockRendering.CapsuleLoopLayout(options, content, y, hasAction);

        writer.StartElement("g")
            .Attribute("data-cfx-role", "segmented-metric-capsule-loop")
            .Attribute("data-cfx-total", VisualBlockRendering.SegmentedTotal(card))
            .Attribute("data-cfx-loop-height", layout.RingHeight)
            .Attribute("data-cfx-stroke", layout.Stroke)
            .EndStartElement().Line();
        RenderCapsuleTrack(writer, card, layout.RingX, layout.RingY, layout.PathWidth, layout.PathHeight, layout.Stroke);
        RenderCapsuleSegments(writer, card, layout.RingX, layout.RingY, layout.PathWidth, layout.PathHeight, layout.Stroke);
        writer.EndElement().Line();

        RenderCapsuleLegend(writer, card, layout.LegendX, layout.LegendY, layout.LegendWidth, layout.LegendHeight);
        if (hasAction) RenderFooterAction(writer, card.ActionLabel, card.ActionSymbol, card.ActionUrl, options.Size.Height - layout.FooterHeight, layout.FooterHeight, content.X, content.Width, theme);
    }

    private static void RenderCapsuleTrack(SvgMarkupWriter writer, SegmentedMetricBlock card, double x, double y, double width, double height, double stroke) {
        var theme = card.Options.Theme;
        var points = VisualBlockRendering.CapsuleLoopPoints(x, y, width, height, stroke, 0, 1, 360);
        writer.StartElement("path")
            .Attribute("data-cfx-role", "segmented-metric-capsule-track-shadow")
            .Attribute("d", PolylinePath(points))
            .Attribute("fill", "none")
            .Attribute("stroke", theme.MutedText.WithAlpha(14).ToCss())
            .Attribute("stroke-width", stroke + 2)
            .Attribute("stroke-linecap", "round")
            .Attribute("stroke-linejoin", "round")
            .Attribute("transform", "translate(0 1.4)")
            .EndEmptyElement().Line();
        writer.StartElement("path")
            .Attribute("data-cfx-role", "segmented-metric-capsule-track")
            .Attribute("d", PolylinePath(points))
            .Attribute("fill", "none")
            .Attribute("stroke", theme.CardBackground.WithAlpha(170).ToCss())
            .Attribute("stroke-width", stroke)
            .Attribute("stroke-linecap", "round")
            .Attribute("stroke-linejoin", "round")
            .EndEmptyElement().Line();
    }

    private static void RenderCapsuleSegments(SvgMarkupWriter writer, SegmentedMetricBlock card, double x, double y, double width, double height, double stroke) {
        var theme = card.Options.Theme;
        var slices = VisualBlockRendering.SegmentedSlices(card);
        foreach (var slice in slices) {
            var color = VisualBlockRendering.SegmentedItemColor(theme, slice.Item, slice.Index);
            var start = Math.Max(0, slice.Start - 0.002);
            var end = Math.Min(1, slice.End + 0.002);
            var points = VisualBlockRendering.CapsuleLoopPoints(x, y, width, height, stroke, start, end, 360);
            if (points.Count < 2) continue;
            writer.StartElement("path")
                .Attribute("data-cfx-role", "segmented-metric-capsule-segment")
                .Attribute("data-cfx-label", slice.Item.Label)
                .Attribute("data-cfx-value", slice.Item.Value)
                .Attribute("data-cfx-share", slice.Share)
                .Attribute("data-cfx-start", slice.Start)
                .Attribute("data-cfx-end", slice.End)
                .Attribute("d", PolylinePath(points))
                .Attribute("fill", "none")
                .Attribute("stroke", color.ToCss())
                .Attribute("stroke-width", stroke)
                .Attribute("stroke-linecap", "round")
                .Attribute("stroke-linejoin", "round")
                .EndEmptyElement().Line();
            if (slice.Share >= 0.075) RenderCapsuleSegmentLabel(writer, card, slice, color, x, y, width, height, stroke);
        }
    }

    private static void RenderCapsuleSegmentLabel(SvgMarkupWriter writer, SegmentedMetricBlock card, SegmentedMetricSlice slice, ChartColor color, double x, double y, double width, double height, double stroke) {
        var theme = card.Options.Theme;
        var label = (slice.Share * 100).ToString("0", CultureInfo.InvariantCulture) + "%";
        var fontSize = Math.Max(9, Math.Min(12, stroke * 0.42));
        var maxWidth = Math.Max(28, stroke * 1.9);
        var point = VisualBlockRendering.CapsuleLoopPoint(x, y, width, height, stroke, (slice.Start + slice.End) / 2);
        var foreground = VisualBlockRendering.CapsuleLoopLabelForeground(color);
        writer.StartElement("text")
            .Attribute("data-cfx-role", "segmented-metric-capsule-label")
            .Attribute("x", point.X)
            .Attribute("y", point.Y + fontSize * 0.34)
            .Attribute("text-anchor", "middle")
            .Attribute("fill", foreground.ToCss())
            .Attribute("stroke", VisualBlockRendering.CapsuleLoopLabelHalo(foreground).ToCss())
            .Attribute("stroke-width", 2.4)
            .Attribute("paint-order", "stroke")
            .Attribute("font-family", theme.FontFamily)
            .Attribute("font-size", fontSize)
            .Attribute("font-weight", "850")
            .Text(VisualBlockRendering.FitText(label, fontSize, maxWidth))
            .EndElement().Line();
    }

    private static string PolylinePath(IReadOnlyList<ChartPoint> points) {
        if (points.Count == 0) return string.Empty;
        var builder = new System.Text.StringBuilder();
        for (var i = 0; i < points.Count; i++) {
            builder.Append(i == 0 ? "M " : " L ");
            builder.Append(F(points[i].X)).Append(' ').Append(F(points[i].Y));
        }

        return builder.ToString();
    }

    private static void RenderCapsuleLegend(SvgMarkupWriter writer, SegmentedMetricBlock card, double x, double y, double width, double height) {
        var theme = card.Options.Theme;
        var rowHeight = Math.Max(26, Math.Min(34, height / Math.Max(1, card.Items.Count)));
        var total = VisualBlockRendering.SegmentedTotal(card);
        var bottom = y + height;
        writer.StartElement("g").Attribute("data-cfx-role", "segmented-metric-capsule-legend").EndStartElement().Line();
        for (var i = 0; i < card.Items.Count && y + rowHeight <= bottom + 1; i++) {
            var item = card.Items[i];
            var color = VisualBlockRendering.SegmentedItemColor(theme, item, i);
            var value = VisualBlockRendering.SegmentedCapsuleLegendValue(card, item, total);
            writer.StartElement("rect").Attribute("data-cfx-role", "segmented-metric-capsule-swatch").Attribute("x", x).Attribute("y", y + rowHeight * 0.26).Attribute("width", 12).Attribute("height", 12).Attribute("rx", 3).Attribute("fill", color.ToCss()).EndEmptyElement().Line();
            WriteText(writer, item.Label, x + 22, y + rowHeight * 0.68, width * 0.56, VisualTextAlignment.Left, theme.MutedText, theme.FontFamily, Math.Max(11, theme.SubtitleFontSize), "550");
            WriteText(writer, value, x + width * 0.60, y + rowHeight * 0.68, width * 0.40, VisualTextAlignment.Right, theme.Text, theme.FontFamily, Math.Max(11, theme.SubtitleFontSize), "800");
            y += rowHeight;
        }

        writer.EndElement().Line();
    }
}
