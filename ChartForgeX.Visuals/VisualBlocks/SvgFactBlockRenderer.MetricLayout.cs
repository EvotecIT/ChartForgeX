using System;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgFactBlockRenderer {
    /// <summary>Fits required metric rows only when their established measured geometry exceeds the available height.</summary>
    private static MetricTextLayout LayoutMetricText(MetricCard card, ChartRect content, double labelSize, double valueSize, double valueYOffset,
        bool inset, double valueWidth, ChartRect? badgeBounds) {
        var bottom = content.Bottom;
        var label = MeasureMetricLine(card, labelSize, 700);
        var value = MeasureMetricValueLine(card, valueSize);
        var labelBaseline = content.Y + labelSize;
        var surfaceY = content.Y + labelSize + 18;
        var surfaceHeight = Math.Max(1, bottom - surfaceY - 4);
        var valueBaseline = inset ? surfaceY + surfaceHeight * .5 + valueSize * .34
            : content.Y + labelSize + valueSize + 14 + valueYOffset;
        var footerBaseline = bottom - (card.Trend.Length > 0 && card.Caption.Length > 0 ? 5 : 0);
        var footer = MeasureMetricFooter(card);
        var valueBottom = footer.Height > 0 ? footerBaseline - footer.Ascent - 2 : bottom;
        var valueTop = Math.Max(inset ? surfaceY : content.Y, labelBaseline + label.Descent + 2);
        var valueLimit = inset ? Math.Min(valueBottom, surfaceY + surfaceHeight) : valueBottom;
        if (labelBaseline - label.Ascent >= content.Y && valueBaseline - value.Ascent >= valueTop
            && valueBaseline + value.Descent <= valueLimit)
            return ArrangeMetricValue(card, content, new MetricTextLayout(labelSize, labelBaseline, valueSize, valueBaseline,
                surfaceY, surfaceHeight, footerBaseline), valueWidth, inset, badgeBounds);

        // Compact cards keep both facts. Spend the decorative gap first, then fit the
        // measured value line; the unit participates in the same baseline and bounds.
        if (footer.Height > 0) {
            footerBaseline = Math.Min(footerBaseline, bottom - footer.Descent);
            valueBottom = footerBaseline - footer.Ascent - 2;
        }
        var labelHeight = Math.Max(1, (valueBottom - content.Y - 2) * .4);
        if (label.Height > labelHeight) {
            labelSize = ChartTextFitting.FitFontSize(0, labelSize, size => MeasureMetricLine(card, size, 700).Height <= labelHeight);
            label = MeasureMetricLine(card, labelSize, 700);
        }
        labelBaseline = content.Y + label.Ascent;
        var rowTop = labelBaseline + label.Descent + 2;
        surfaceY = rowTop;
        surfaceHeight = Math.Max(1, valueBottom - surfaceY);
        valueSize = Math.Max(.001, ChartTextFitting.FitFontSize(0, valueSize,
            size => MeasureMetricValueLine(card, size).Height <= Math.Max(1, valueBottom - rowTop)));
        value = MeasureMetricValueLine(card, valueSize);
        valueBaseline = rowTop + value.Ascent;
        if (inset) valueBaseline += Math.Max(0, (surfaceHeight - value.Height) / 2);
        return ArrangeMetricValue(card, content, new MetricTextLayout(labelSize, labelBaseline, valueSize, valueBaseline,
            surfaceY, surfaceHeight, footerBaseline, compacted: true), valueWidth, inset, badgeBounds);
    }

    /// <summary>Reserves a measured value lane around visible badges without changing geometry that already fits.</summary>
    private static MetricTextLayout ArrangeMetricValue(MetricCard card, ChartRect content, MetricTextLayout layout,
        double valueWidth, bool inset, ChartRect? badgeBounds) {
        var x = inset ? content.X + 22 : content.X;
        var radius = 0.0;
        var badgeX = 0.0;
        var badgeY = 0.0;
        var measured = false;
        if (inset) {
            if (card.Icon != VisualIcon.None || card.Symbol.Length > 0) {
                var preferredRadius = Math.Min(24, Math.Max(17, layout.SurfaceHeight * .30));
                radius = Math.Min(preferredRadius, Math.Max(.5, (layout.SurfaceHeight - 1) / 2));
                measured = radius < preferredRadius;
                badgeX = content.X + 28 + radius;
                badgeY = layout.SurfaceY + layout.SurfaceHeight / 2;
                x = badgeX + radius + 24;
            }
            valueWidth = Math.Max(1, Math.Min(valueWidth, content.Right - x - 22));
        }
        var line = MeasureMetricValueLine(card, layout.ValueSize);
        var textWidth = Math.Max(MetricValueWidth(card, layout.ValueSize),
            LayoutMetricValueText(card, layout.ValueSize, valueWidth, measured: false).Width);
        if (!inset && badgeBounds is { } badge && layout.ValueBaseline - line.Ascent < badge.Bottom + 2
            && layout.ValueBaseline + line.Descent > badge.Top - 2 && x < badge.Right + 2 && x + textWidth > badge.Left - 2) {
            if (card.BadgePlacement == MetricCardBadgePlacement.TopLeft) {
                var right = x + valueWidth;
                x = Math.Max(x, badge.Right + 12);
                valueWidth = Math.Max(1, right - x);
            } else valueWidth = Math.Max(1, Math.Min(valueWidth, badge.Left - 10 - x));
            measured = true;
        }
        var valueSize = layout.ValueSize;
        measured |= textWidth > valueWidth || (card.Unit.Length > 0 &&
            VisualBlockRendering.MeasureTextWidth(card.Value, valueSize, card.Options.ResolvedTheme.FontFamily, 700)
                > VisualBlockRendering.EstimateTextWidth(card.Value, valueSize));
        if (measured) {
            // Long facts retain readable emphasis and use the existing end-ellipsis
            // contract after fitting, rather than forcing every character into tiny text.
            valueSize = ChartTextFitting.FitFontSize(Math.Min(22, valueSize), valueSize,
                size => MetricValueWidth(card, size) <= valueWidth);
        }
        return new MetricTextLayout(layout.LabelSize, layout.LabelBaseline, valueSize, layout.ValueBaseline,
            layout.SurfaceY, layout.SurfaceHeight, layout.FooterBaseline, layout.Compacted,
            new MetricValuePlacement(x, valueWidth, badgeX, badgeY, radius, measured));
    }

    private static double MetricValueWidth(MetricCard card, double size) {
        var family = card.Options.ResolvedTheme.FontFamily;
        var width = VisualBlockRendering.MeasureTextWidth(card.Value, size, family, 700);
        if (card.Unit.Length > 0) width += Math.Max(6, size * .14) +
            VisualBlockRendering.MeasureTextWidth(card.Unit, MetricUnitFontSize(size), family, 650);
        return width;
    }

    private static MetricLine MeasureMetricValueLine(MetricCard card, double valueSize) {
        // RenderSurface normalizes the emphasized metric value to weight 700.
        var value = MeasureMetricLine(card, valueSize, 700);
        if (card.Unit.Length == 0) return value;
        var unit = MeasureMetricLine(card, MetricUnitFontSize(valueSize), 650);
        return new MetricLine(Math.Max(value.Ascent, unit.Ascent), Math.Max(value.Descent, unit.Descent));
    }

    private static MetricLine MeasureMetricFooter(MetricCard card) {
        var size = card.Options.ResolvedTheme.SubtitleFontSize;
        var trend = card.Trend.Length == 0 ? default : MeasureMetricLine(card, Math.Max(10, size), 700);
        var caption = card.Caption.Length == 0 ? default : MeasureMetricLine(card, Math.Max(10, size - 1), 400);
        return new MetricLine(Math.Max(trend.Ascent, caption.Ascent), Math.Max(trend.Descent, caption.Descent));
    }

    private static MetricLine MeasureMetricLine(MetricCard card, double size, int weight) {
        var style = new TextStyle { Font = new FontSpec { Family = card.Options.ResolvedTheme.FontFamily, Weight = weight },
            FontSize = Math.Max(.001, size), LineHeight = 1 };
        var height = TextLayoutEngine.Measure("Mg", style).Height;
        var ascent = VisualBlockRendering.TextAscent(style);
        return new MetricLine(ascent, Math.Max(0, height - ascent));
    }

    private readonly struct MetricLine {
        internal MetricLine(double ascent, double descent) { Ascent = ascent; Descent = descent; }
        internal double Ascent { get; }
        internal double Descent { get; }
        internal double Height => Ascent + Descent;
    }

    private readonly struct MetricTextLayout {
        internal MetricTextLayout(double labelSize, double labelBaseline, double valueSize, double valueBaseline, double surfaceY, double surfaceHeight,
            double footerBaseline, bool compacted = false, MetricValuePlacement valuePlacement = default) {
            LabelSize = labelSize; LabelBaseline = labelBaseline; ValueSize = valueSize; ValueBaseline = valueBaseline;
            SurfaceY = surfaceY; SurfaceHeight = surfaceHeight; FooterBaseline = footerBaseline;
            Compacted = compacted;
            ValuePlacement = valuePlacement;
        }
        internal double LabelSize { get; }
        internal double LabelBaseline { get; }
        internal double ValueSize { get; }
        internal double ValueBaseline { get; }
        internal double SurfaceY { get; }
        internal double SurfaceHeight { get; }
        internal double FooterBaseline { get; }
        internal bool Compacted { get; }
        internal MetricValuePlacement ValuePlacement { get; }
    }

    private readonly struct MetricValuePlacement {
        internal MetricValuePlacement(double x, double width, double badgeX, double badgeY, double badgeRadius, bool measured) {
            X = x; Width = width; BadgeX = badgeX; BadgeY = badgeY; BadgeRadius = badgeRadius; Measured = measured;
        }
        internal double X { get; }
        internal double Width { get; }
        internal double BadgeX { get; }
        internal double BadgeY { get; }
        internal double BadgeRadius { get; }
        internal bool Measured { get; }
    }
}
