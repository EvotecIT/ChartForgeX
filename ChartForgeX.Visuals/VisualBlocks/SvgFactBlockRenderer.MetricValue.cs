using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;

using static ChartForgeX.VisualBlocks.SvgVisualBlockRenderer;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgFactBlockRenderer {
    private static void RenderMetricValueSurface(SvgMarkupWriter writer, MetricCard card, ChartRect content, MetricTextLayout textLayout, ChartColor statusColor) {
        var theme = card.Options.ResolvedTheme;
        var y = textLayout.SurfaceY;
        var height = textLayout.SurfaceHeight;
        var valueSize = textLayout.ValueSize;
        var radius = Math.Min(20, Math.Max(14, theme.PlotCornerRadius + 7));
        writer.StartElement("rect").Attribute("data-cfx-role", "metric-value-surface").Attribute("x", content.X).Attribute("y", y).Attribute("width", content.Width).Attribute("height", height).Attribute("rx", radius).Attribute("fill", theme.PlotBackground.ToCss()).Attribute("stroke", theme.PlotBorder.WithAlpha(140).ToCss()).EndEmptyElement().Line();
        var badgeColor = card.Status == VisualStatus.None ? VisualBlockRendering.PaletteAt(theme, 0) : statusColor;
        var hasBadge = card.Icon != VisualIcon.None || card.Symbol.Length > 0;
        var valueX = textLayout.ValuePlacement.X;
        if (hasBadge) {
            var badgeRadius = textLayout.ValuePlacement.BadgeRadius;
            var cx = textLayout.ValuePlacement.BadgeX;
            var cy = textLayout.ValuePlacement.BadgeY;
            var symbolMaxWidth = badgeRadius * 1.62;
            var symbolSize = VisualBlockRendering.FitFontSize(card.Symbol, symbolMaxWidth, Math.Max(10, badgeRadius * 0.46), 7.5);
            if (textLayout.ValuePlacement.Measured) symbolSize = ChartTextFitting.FitFontSize(0, symbolSize,
                size => VisualBlockRendering.MeasureTextWidth(card.Symbol, size, theme.FontFamily, 700) <= symbolMaxWidth);
            writer.StartElement("circle").Attribute("data-cfx-role", "metric-value-surface-badge").Attribute("cx", cx).Attribute("cy", cy).Attribute("r", badgeRadius).Attribute("fill", badgeColor.WithAlpha(36).ToCss()).Attribute("stroke", theme.PlotBorder.WithAlpha(155).ToCss()).EndEmptyElement().Line();
            if (card.Icon != VisualIcon.None) WriteIcon(writer, card.Icon, cx, cy, badgeRadius * 0.62, badgeColor);
            else writer.StartElement("text").Attribute("data-cfx-role", "metric-symbol").Attribute("x", cx).Attribute("y", cy).Attribute("text-anchor", "middle").Attribute("dominant-baseline", "central").Attribute("alignment-baseline", "central").Attribute("fill", theme.Text.ToCss()).Attribute("font-family", theme.FontFamily).Attribute("font-size", symbolSize).Attribute("font-weight", "850").Text(VisualBlockRendering.FitText(card.Symbol, symbolSize, symbolMaxWidth)).EndElement().Line();
        }

        RenderMetricValueText(writer, card, valueX, textLayout.ValueBaseline, valueSize, textLayout.ValuePlacement.Width,
            theme.Text, theme.MutedText, textLayout.ValuePlacement.Measured);
    }

    private static void RenderMetricValueText(SvgMarkupWriter writer, MetricCard card, double x, double y, double valueSize, double maxWidth,
        ChartColor valueColor, ChartColor unitColor, bool measured = false) {
        var unitSize = MetricUnitFontSize(valueSize);
        var gap = card.Unit.Length == 0 ? 0 : Math.Max(6, valueSize * 0.14);
        var family = card.Options.ResolvedTheme.FontFamily;
        var desiredUnitWidth = card.Unit.Length == 0 ? 0 : measured ? VisualBlockRendering.MeasureTextWidth(card.Unit, unitSize, family, 650)
            : VisualBlockRendering.EstimateTextWidth(card.Unit, unitSize);
        var valueWidth = Math.Max(1, maxWidth - desiredUnitWidth - gap);
        var fittedValue = measured ? VisualBlockRendering.FitMeasuredText(card.Value, valueSize, valueWidth, family, 700)
            : VisualBlockRendering.FitText(card.Value, valueSize, valueWidth);
        writer.StartElement("text").Attribute("data-cfx-role", "metric-value").Attribute("x", x).Attribute("y", y).Attribute("fill", valueColor.ToCss()).Attribute("font-family", card.Options.ResolvedTheme.FontFamily).Attribute("font-size", valueSize).Attribute("font-weight", "850").Text(fittedValue).EndElement().Line();
        if (card.Unit.Length == 0) return;
        var valueTextWidth = measured ? VisualBlockRendering.MeasureTextWidth(fittedValue, valueSize, family, 700)
            : VisualBlockRendering.EstimateTextWidth(fittedValue, valueSize);
        var unitWidth = Math.Max(1, maxWidth - valueTextWidth - gap);
        var fittedUnit = measured ? VisualBlockRendering.FitMeasuredText(card.Unit, unitSize, unitWidth, family, 650)
            : VisualBlockRendering.FitText(card.Unit, unitSize, unitWidth);
        writer.StartElement("text").Attribute("data-cfx-role", "metric-unit").Attribute("x", x + valueTextWidth + gap).Attribute("y", y).Attribute("fill", unitColor.ToCss()).Attribute("font-family", family).Attribute("font-size", unitSize).Attribute("font-weight", "650").Text(fittedUnit).EndElement().Line();
    }

    private static double MetricValueFontSize(MetricCard card, double requestedSize, double maxWidth) {
        var unitSize = MetricUnitFontSize(requestedSize);
        var estimatedWidth = VisualBlockRendering.EstimateTextWidth(card.Value, requestedSize);
        if (card.Unit.Length > 0) estimatedWidth += Math.Max(6, requestedSize * 0.14) + VisualBlockRendering.EstimateTextWidth(card.Unit, unitSize);
        if (estimatedWidth <= maxWidth) return requestedSize;
        return Math.Max(22, Math.Floor(requestedSize * maxWidth / Math.Max(1, estimatedWidth)));
    }

    private static double MetricUnitFontSize(double valueSize) => Math.Min(valueSize, Math.Max(13, Math.Min(24, valueSize * 0.42)));
}
