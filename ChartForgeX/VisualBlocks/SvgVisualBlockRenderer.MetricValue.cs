using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgVisualBlockRenderer {
    private static void RenderMetricValueSurface(SvgMarkupWriter writer, MetricCard card, ChartRect content, double detailBottom, double labelSize, double valueSize, double valueWidth, ChartColor statusColor) {
        var theme = card.Options.Theme;
        var y = content.Y + labelSize + 18;
        var height = Math.Max(1, detailBottom - y - 4);
        var radius = Math.Min(20, Math.Max(14, theme.PlotCornerRadius + 7));
        writer.StartElement("rect").Attribute("data-cfx-role", "metric-value-surface").Attribute("x", content.X).Attribute("y", y).Attribute("width", content.Width).Attribute("height", height).Attribute("rx", radius).Attribute("fill", theme.PlotBackground.ToCss()).Attribute("stroke", theme.PlotBorder.WithAlpha(140).ToCss()).EndEmptyElement().Line();
        var badgeColor = card.Status == VisualStatus.None ? VisualBlockRendering.PaletteAt(theme, 0) : statusColor;
        var hasBadge = card.Icon != VisualIcon.None || card.Symbol.Length > 0;
        var valueX = content.X + 22;
        if (hasBadge) {
            var badgeRadius = Math.Min(24, Math.Max(17, height * 0.30));
            var cx = content.X + 28 + badgeRadius;
            var cy = y + height / 2;
            var symbolMaxWidth = badgeRadius * 1.62;
            var symbolSize = VisualBlockRendering.FitFontSize(card.Symbol, symbolMaxWidth, Math.Max(10, badgeRadius * 0.46), 7.5);
            writer.StartElement("circle").Attribute("data-cfx-role", "metric-value-surface-badge").Attribute("cx", cx).Attribute("cy", cy).Attribute("r", badgeRadius).Attribute("fill", badgeColor.WithAlpha(36).ToCss()).Attribute("stroke", theme.PlotBorder.WithAlpha(155).ToCss()).EndEmptyElement().Line();
            if (card.Icon != VisualIcon.None) WriteIcon(writer, card.Icon, cx, cy, badgeRadius * 0.62, badgeColor);
            else writer.StartElement("text").Attribute("data-cfx-role", "metric-symbol").Attribute("x", cx).Attribute("y", cy).Attribute("text-anchor", "middle").Attribute("dominant-baseline", "central").Attribute("alignment-baseline", "central").Attribute("fill", ChartColorMath.TextOnBackground(badgeColor).ToCss()).Attribute("font-family", theme.FontFamily).Attribute("font-size", symbolSize).Attribute("font-weight", "850").Text(VisualBlockRendering.FitText(card.Symbol, symbolSize, symbolMaxWidth)).EndElement().Line();
            valueX = cx + badgeRadius + 24;
        }

        var surfaceValueWidth = Math.Max(1, Math.Min(valueWidth, content.X + content.Width - valueX - 22));
        RenderMetricValueText(writer, card, valueX, y + height * 0.5 + valueSize * 0.34, valueSize, surfaceValueWidth, theme.Text, theme.MutedText);
    }

    private static void RenderMetricValueText(SvgMarkupWriter writer, MetricCard card, double x, double y, double valueSize, double maxWidth, ChartColor valueColor, ChartColor unitColor) {
        var unitSize = MetricUnitFontSize(valueSize);
        var gap = card.Unit.Length == 0 ? 0 : Math.Max(6, valueSize * 0.14);
        var desiredUnitWidth = card.Unit.Length == 0 ? 0 : VisualBlockRendering.EstimateTextWidth(card.Unit, unitSize);
        var valueWidth = Math.Max(1, maxWidth - desiredUnitWidth - gap);
        var fittedValue = VisualBlockRendering.FitText(card.Value, valueSize, valueWidth);
        writer.StartElement("text").Attribute("data-cfx-role", "metric-value").Attribute("x", x).Attribute("y", y).Attribute("fill", valueColor.ToCss()).Attribute("font-family", card.Options.Theme.FontFamily).Attribute("font-size", valueSize).Attribute("font-weight", "850").Text(fittedValue).EndElement().Line();
        if (card.Unit.Length == 0) return;
        var valueTextWidth = VisualBlockRendering.EstimateTextWidth(fittedValue, valueSize);
        var unitWidth = Math.Max(1, maxWidth - valueTextWidth - gap);
        writer.StartElement("text").Attribute("data-cfx-role", "metric-unit").Attribute("x", x + valueTextWidth + gap).Attribute("y", y).Attribute("fill", unitColor.ToCss()).Attribute("font-family", card.Options.Theme.FontFamily).Attribute("font-size", unitSize).Attribute("font-weight", "650").Text(VisualBlockRendering.FitText(card.Unit, unitSize, unitWidth)).EndElement().Line();
    }

    private static double MetricValueFontSize(MetricCard card, double requestedSize, double maxWidth) {
        var unitSize = MetricUnitFontSize(requestedSize);
        var estimatedWidth = VisualBlockRendering.EstimateTextWidth(card.Value, requestedSize);
        if (card.Unit.Length > 0) estimatedWidth += Math.Max(6, requestedSize * 0.14) + VisualBlockRendering.EstimateTextWidth(card.Unit, unitSize);
        if (estimatedWidth <= maxWidth) return requestedSize;
        return Math.Max(22, Math.Floor(requestedSize * maxWidth / Math.Max(1, estimatedWidth)));
    }

    private static double MetricUnitFontSize(double valueSize) => Math.Max(13, Math.Min(24, valueSize * 0.42));
}
