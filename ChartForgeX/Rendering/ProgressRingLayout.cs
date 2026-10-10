using System;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal readonly struct ProgressRingLayout {
    private ProgressRingLayout(double outerRadius, double strokeWidth, double gap, double centerRadius) {
        OuterRadius = outerRadius;
        StrokeWidth = strokeWidth;
        Gap = gap;
        CenterRadius = centerRadius;
    }

    internal double OuterRadius { get; }
    internal double StrokeWidth { get; }
    internal double Gap { get; }
    internal double CenterRadius { get; }

    internal double RadiusAt(int index) => OuterRadius - StrokeWidth / 2 - index * (StrokeWidth + Gap);

    internal static double RequestedCenterRadius(Chart chart, ChartSeries series, double outerRadius, string centerLabel, double valueFontSize, double nameFontSize, TextStyleOverride style) {
        if (!chart.Options.ShowProgressRingCenterLabel || series.ShowDataLabels == false) return 0;
        var measurement = new TextMeasurementContext(style.FontFamily ?? chart.Options.Theme.FontFamily);
        var value = style.TransformText(centerLabel, CultureInfo.InvariantCulture);
        var name = style.TransformText(series.Name, CultureInfo.InvariantCulture);
        var widest = Math.Max(Width(value, valueFontSize), Width(name, nameFontSize));
        var gap = Math.Max(4, Math.Min(8, outerRadius * 0.10));
        var height = valueFontSize * 1.2 + gap + nameFontSize * 1.2;
        return Math.Max(widest / 2.0 + 10, height / 2.0 + 8);

        double Width(string text, double size) {
            if (style.OpenTypeLanguageTag == null && style.Variations == null) return measurement.Measure(text, size, bold: true);
            var resolved = new TextStyle { Font = FontSpec.FromFamily(style.FontFamily ?? chart.Options.Theme.FontFamily), FontSize = size, OpenTypeLanguageTag = style.OpenTypeLanguageTag == "normal" ? null : style.OpenTypeLanguageTag };
            resolved.Font.Variations = style.Variations ?? FontVariationSettings.Default;
        resolved.Font.ColorPaletteIndex = style.ColorPaletteIndex ?? 0;
            resolved.Font.Weight = TypographyFontResolver.FontSpecWeight(style.ResolveFontWeight(700));
            resolved.Font.Italic = style.Italic;
            return TextLayoutEngine.Measure(text, resolved).Width;
        }
    }

    internal static ProgressRingLayout Create(double outerRadius, int count, double strokeScale, double requestedCenterRadius = 0) {
        if (count <= 0) return new ProgressRingLayout(outerRadius, 0, 0, Math.Max(0, outerRadius));

        var preferredGap = Math.Max(5, outerRadius * 0.035);
        var preferredStroke = Math.Max(5, Math.Min(24, ((outerRadius - 18) / count - preferredGap) * strokeScale));
        var defaultCenterRadius = Math.Min(outerRadius * 0.40, Math.Max(26, outerRadius * 0.24));
        var maximumRequestedCenterRadius = Math.Max(0, outerRadius * 0.55);
        var boundedRequestedCenterRadius = Math.Min(maximumRequestedCenterRadius, Math.Max(0, requestedCenterRadius));
        var minimumCenterRadius = Math.Min(Math.Max(0, outerRadius - 2.5), Math.Max(defaultCenterRadius, boundedRequestedCenterRadius));
        var availableBand = Math.Max(0.5, outerRadius - minimumCenterRadius - 2);
        var preferredBand = preferredStroke * count + preferredGap * Math.Max(0, count - 1);

        if (preferredBand <= availableBand) {
            var centerRadius = Math.Max(minimumCenterRadius, outerRadius - preferredBand - 2);
            return new ProgressRingLayout(outerRadius, preferredStroke, preferredGap, centerRadius);
        }

        var slot = availableBand / count;
        var strokeShare = Math.Max(0.55, Math.Min(0.90, 0.72 * strokeScale));
        var stroke = slot * strokeShare;
        var gap = count > 1 ? Math.Max(0, (availableBand - stroke * count) / (count - 1)) : 0;
        return new ProgressRingLayout(outerRadius, stroke, gap, minimumCenterRadius);
    }
}
