using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgVisualBlockRenderer {
    private static void RenderSegmentedMetricCapsuleRing(SvgMarkupWriter writer, SegmentedMetricBlock card) {
        var options = card.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        RenderSegmentedMetricHeading(writer, card, ref y, content.X, content.Width);
        y += 8;

        var hasAction = card.ActionLabel.Length > 0;
        var layout = VisualBlockRendering.CapsuleRingLayout(options, content, y, hasAction);

        writer.StartElement("g")
            .Attribute("data-cfx-role", "segmented-metric-capsule-ring")
            .Attribute("data-cfx-total", VisualBlockRendering.SegmentedTotal(card))
            .EndStartElement().Line();
        RenderCapsuleTrack(writer, card, layout.RingX, layout.RingY, layout.PathWidth, layout.PathHeight, layout.Stroke);
        RenderCapsuleSegments(writer, card, layout.RingX, layout.RingY, layout.PathWidth, layout.PathHeight, layout.Stroke);
        writer.EndElement().Line();

        RenderCapsuleLegend(writer, card, layout.LegendX, layout.LegendY, layout.LegendWidth, layout.LegendHeight);
        if (hasAction) RenderFooterAction(writer, card.ActionLabel, card.ActionSymbol, card.ActionUrl, options.Size.Height - layout.FooterHeight, layout.FooterHeight, content.X, content.Width, theme);
    }

    private static void RenderCapsuleTrack(SvgMarkupWriter writer, SegmentedMetricBlock card, double x, double y, double width, double height, double stroke) {
        var theme = card.Options.Theme;
        writer.StartElement("path")
            .Attribute("data-cfx-role", "segmented-metric-capsule-track-shadow")
            .Attribute("d", ClosedPathFromPoints(VisualBlockRendering.CapsuleRingBandSegmentPoints(x, y + 1.2, width, height, stroke + 2, 0, 1, VisualBlockRendering.CapsuleRingSamples)))
            .Attribute("fill", theme.MutedText.WithAlpha(8).ToCss())
            .EndEmptyElement().Line();
        writer.StartElement("path")
            .Attribute("data-cfx-role", "segmented-metric-capsule-track")
            .Attribute("d", ClosedPathFromPoints(VisualBlockRendering.CapsuleRingBandSegmentPoints(x, y, width, height, stroke, 0, 1, VisualBlockRendering.CapsuleRingSamples)))
            .Attribute("fill", theme.CardBackground.WithAlpha(160).ToCss())
            .EndEmptyElement().Line();
    }

    private static void RenderCapsuleSegments(SvgMarkupWriter writer, SegmentedMetricBlock card, double x, double y, double width, double height, double stroke) {
        var theme = card.Options.Theme;
        var slices = VisualBlockRendering.SegmentedSlices(card);
        foreach (var slice in slices) {
            var color = VisualBlockRendering.SegmentedItemColor(theme, slice.Item, slice.Index);
            var segmentGap = 0.0;
            var start = slice.Start;
            var end = slice.End;
            if (end - start <= 0.001) continue;
            var path = ClosedPathFromPoints(VisualBlockRendering.CapsuleRingBandSegmentPoints(x, y, width, height, stroke, start, end, VisualBlockRendering.CapsuleRingSamples));
            writer.StartElement("path")
                .Attribute("data-cfx-role", "segmented-metric-capsule-segment")
                .Attribute("data-cfx-label", slice.Item.Label)
                .Attribute("data-cfx-value", slice.Item.Value)
                .Attribute("data-cfx-share", slice.Share)
                .Attribute("data-cfx-gap", segmentGap)
                .Attribute("data-cfx-samples", VisualBlockRendering.CapsuleRingSamples)
                .Attribute("d", path)
                .Attribute("fill", color.ToCss())
                .EndEmptyElement().Line();
            if (slice.Share >= 0.075) RenderCapsuleSegmentLabel(writer, card, slice, color, x, y, width, height, stroke);
        }

        RenderCapsuleSeparators(writer, card, slices, x, y, width, height, stroke);
    }

    private static void RenderCapsuleSeparators(SvgMarkupWriter writer, SegmentedMetricBlock card, IReadOnlyList<SegmentedMetricSlice> slices, double x, double y, double width, double height, double stroke) {
        var theme = card.Options.Theme;
        var gap = VisualBlockRendering.CapsuleRingBoundaryGapRatio(width, height, stroke);
        for (var i = 1; i < slices.Count; i++) {
            var ratio = slices[i].Start;
            writer.StartElement("path")
                .Attribute("data-cfx-role", "segmented-metric-capsule-separator")
                .Attribute("data-cfx-ratio", ratio)
                .Attribute("d", ClosedPathFromPoints(VisualBlockRendering.CapsuleRingBandSegmentPoints(x, y, width, height, stroke + 0.8, Math.Max(0, ratio - gap), Math.Min(1, ratio + gap), Math.Max(8, VisualBlockRendering.CapsuleRingSamples / 6))))
                .Attribute("fill", theme.CardBackground.ToCss())
                .EndEmptyElement().Line();
        }
    }

    private static void RenderCapsuleSegmentLabel(SvgMarkupWriter writer, SegmentedMetricBlock card, SegmentedMetricSlice slice, ChartColor color, double x, double y, double width, double height, double stroke) {
        var theme = card.Options.Theme;
        var point = VisualBlockRendering.CapsuleRingPoint(x, y, width, height, (slice.Start + slice.End) / 2);
        var label = (slice.Share * 100).ToString("0", CultureInfo.InvariantCulture) + "%";
        var maxWidth = Math.Max(34, stroke * 1.9);
        var foreground = VisualBlockRendering.CapsuleRingLabelForeground(color);
        writer.StartElement("text")
            .Attribute("data-cfx-role", "segmented-metric-capsule-label")
            .Attribute("x", point.X)
            .Attribute("y", point.Y + Math.Max(3, theme.SubtitleFontSize * 0.32))
            .Attribute("text-anchor", "middle")
            .Attribute("fill", foreground.ToCss())
            .Attribute("stroke", VisualBlockRendering.CapsuleRingLabelHalo(foreground).ToCss())
            .Attribute("stroke-width", 2.4)
            .Attribute("paint-order", "stroke")
            .Attribute("font-family", theme.FontFamily)
            .Attribute("font-size", Math.Max(9, Math.Min(12, stroke * 0.42)))
            .Attribute("font-weight", "850")
            .Text(VisualBlockRendering.FitText(label, Math.Max(9, Math.Min(12, stroke * 0.42)), maxWidth))
            .EndElement().Line();
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

    private static string PathFromPoints(IReadOnlyList<ChartPoint> points) {
        var builder = new StringBuilder();
        for (var i = 0; i < points.Count; i++) {
            if (i == 0) builder.Append("M ");
            else builder.Append(" L ");
            builder.Append(F(points[i].X)).Append(' ').Append(F(points[i].Y));
        }

        return builder.ToString();
    }

    private static string ClosedPathFromPoints(IReadOnlyList<ChartPoint> points) {
        if (points.Count == 0) return string.Empty;
        return PathFromPoints(points) + " Z";
    }
}
