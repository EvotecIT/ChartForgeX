using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualBlocks;

public sealed partial class PngVisualBlockRenderer {
    private static void DrawMetricValueSurface(RgbaCanvas canvas, MetricCard card, ChartRect content, double detailBottom, double labelSize, double valueSize, double valueWidth, ChartColor statusColor) {
        var theme = card.Options.Theme;
        var y = content.Y + labelSize + 18;
        var height = Math.Max(1, detailBottom - y - 4);
        var radius = Math.Min(20, Math.Max(14, theme.PlotCornerRadius + 7));
        canvas.FillRoundedRectVerticalGradient(content.X, y, content.Width, height, radius, ChartSurfacePolish.GradientTop(theme.PlotBackground), ChartSurfacePolish.GradientBottom(theme.CardBackground));
        canvas.StrokeRoundedRect(content.X, y, content.Width, height, radius, theme.PlotBorder.WithAlpha(140), 1);
        var badgeColor = card.Status == VisualStatus.None ? VisualBlockRendering.PaletteAt(theme, 0) : statusColor;
        var hasBadge = card.Icon != VisualIcon.None || card.Symbol.Length > 0;
        var valueX = content.X + 22;
        if (hasBadge) {
            var badgeRadius = Math.Min(24, Math.Max(17, height * 0.30));
            var cx = content.X + 28 + badgeRadius;
            var cy = y + height / 2;
            canvas.DrawCircle(cx, cy, badgeRadius, badgeColor.WithAlpha(36));
            canvas.DrawCircleOutline(cx, cy, badgeRadius, theme.PlotBorder.WithAlpha(155), 1);
            if (card.Icon != VisualIcon.None) DrawIcon(canvas, card.Icon, cx, cy, badgeRadius * 0.62, badgeColor);
            else {
                var symbolMaxWidth = badgeRadius * 1.62;
                var symbolSize = MetricSymbolFontSize(card.Symbol, Math.Max(10, badgeRadius * 0.46), symbolMaxWidth);
                DrawCenteredTextMiddle(canvas, card.Symbol, cx, cy, symbolSize, ChartColorMath.TextOnBackground(badgeColor), true, symbolMaxWidth);
            }

            valueX = cx + badgeRadius + 24;
        }

        var surfaceValueWidth = Math.Max(1, Math.Min(valueWidth, content.X + content.Width - valueX - 22));
        DrawMetricValueText(canvas, card, valueX, y + (height - valueSize) * 0.52, valueSize, surfaceValueWidth, theme.Text, theme.MutedText);
    }

    private static void DrawMetricValueText(RgbaCanvas canvas, MetricCard card, double x, double y, double valueSize, double maxWidth, ChartColor valueColor, ChartColor unitColor) {
        var unitSize = MetricUnitFontSize(valueSize);
        var gap = card.Unit.Length == 0 ? 0 : Math.Max(6, valueSize * 0.14);
        var desiredUnitWidth = card.Unit.Length == 0 ? 0 : RgbaCanvas.MeasureTextWidth(card.Unit, unitSize, null);
        var valueWidth = Math.Max(1, maxWidth - desiredUnitWidth - gap);
        var fittedValue = FitText(card.Value, valueSize, valueWidth);
        canvas.DrawTextEmphasized(x, y, fittedValue, valueColor, valueSize);
        if (card.Unit.Length == 0) return;
        var valueTextWidth = RgbaCanvas.MeasureTextEmphasizedWidth(fittedValue, valueSize, null);
        var unitWidth = Math.Max(1, maxWidth - valueTextWidth - gap);
        canvas.DrawText(x + valueTextWidth + gap, y + valueSize * 0.42, FitText(card.Unit, unitSize, unitWidth), unitColor, unitSize);
    }

    private static double MetricValueFontSize(MetricCard card, double requestedSize, double maxWidth) {
        var unitSize = MetricUnitFontSize(requestedSize);
        var measuredWidth = RgbaCanvas.MeasureTextEmphasizedWidth(card.Value, requestedSize, null);
        if (card.Unit.Length > 0) measuredWidth += Math.Max(6, requestedSize * 0.14) + RgbaCanvas.MeasureTextWidth(card.Unit, unitSize, null);
        if (measuredWidth <= maxWidth) return requestedSize;
        return Math.Max(22, Math.Floor(requestedSize * maxWidth / Math.Max(1, measuredWidth)));
    }

    private static double MetricUnitFontSize(double valueSize) => Math.Max(13, Math.Min(24, valueSize * 0.42));
}
