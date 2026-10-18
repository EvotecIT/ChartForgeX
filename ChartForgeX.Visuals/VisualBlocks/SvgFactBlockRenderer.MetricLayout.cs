using System;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgFactBlockRenderer {
    /// <summary>Fits required metric rows only when their established measured geometry exceeds the available height.</summary>
    private static MetricTextLayout LayoutMetricText(MetricCard card, ChartRect content, double labelSize, double valueSize, double valueYOffset, bool inset) {
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
            return new MetricTextLayout(labelSize, labelBaseline, valueSize, valueBaseline, surfaceY, surfaceHeight, footerBaseline);

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
        return new MetricTextLayout(labelSize, labelBaseline, valueSize, valueBaseline, surfaceY, surfaceHeight, footerBaseline, compacted: true);
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
        internal MetricTextLayout(double labelSize, double labelBaseline, double valueSize, double valueBaseline, double surfaceY, double surfaceHeight, double footerBaseline, bool compacted = false) {
            LabelSize = labelSize; LabelBaseline = labelBaseline; ValueSize = valueSize; ValueBaseline = valueBaseline;
            SurfaceY = surfaceY; SurfaceHeight = surfaceHeight; FooterBaseline = footerBaseline;
            Compacted = compacted;
        }
        internal double LabelSize { get; }
        internal double LabelBaseline { get; }
        internal double ValueSize { get; }
        internal double ValueBaseline { get; }
        internal double SurfaceY { get; }
        internal double SurfaceHeight { get; }
        internal double FooterBaseline { get; }
        internal bool Compacted { get; }
    }
}
