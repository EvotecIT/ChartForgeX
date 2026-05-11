using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static class ChartHeatmapSurface {
    public static ChartColor Color(Chart chart, ChartColor? highColor, double value, double min, double max) {
        var ratio = Ratio(value, min, max);
        if (chart.Options.HeatmapScale == ChartHeatmapScale.Semantic) return SemanticColor(chart, ratio);
        return ChartColorMath.Blend(chart.Options.Theme.PlotBackground, highColor ?? chart.Options.Theme.Palette[0], 0.18 + ratio * 0.82);
    }

    public static ChartColor MapColor(Chart chart, ChartColor? pointColor, ChartColor highColor, double value, double min, double max) {
        var scale = chart.Options.MapColorScale;
        if (scale == null) return Color(chart, pointColor ?? highColor, value, min, max);
        if (pointColor.HasValue) return pointColor.Value;
        return scale.ColorFor(value, min, max);
    }

    public static double MapRatio(Chart chart, double value, double min, double max) {
        var scale = chart.Options.MapColorScale;
        if (scale == null) return Ratio(value, min, max);
        var effectiveMin = scale.EffectiveMinimum(min);
        var effectiveMax = scale.EffectiveMaximum(max);
        return ContinuousRatio(value, effectiveMin, effectiveMax);
    }

    public static double MapScaleValue(Chart chart, double min, double max, double ratio) {
        var scale = chart.Options.MapColorScale;
        var effectiveMin = scale?.EffectiveMinimum(min) ?? min;
        var effectiveMax = scale?.EffectiveMaximum(max) ?? max;
        if (effectiveMax <= effectiveMin + 0.000001) effectiveMax = effectiveMin + 1;
        return effectiveMin + (effectiveMax - effectiveMin) * Clamp(ratio, 0, 1);
    }

    public static double MapScaleMidpoint(Chart chart, double min, double max) {
        var scale = chart.Options.MapColorScale;
        if (scale == null) return MapScaleValue(chart, min, max, 0.5);
        return scale.EffectiveMidpoint(min, max);
    }

    public static string MapLowLabel(Chart chart) => chart.Options.MapColorScale?.LowLabel ?? "Less";

    public static string? MapMidpointLabel(Chart chart) => chart.Options.MapColorScale?.MidpointLabel;

    public static string MapHighLabel(Chart chart) => chart.Options.MapColorScale?.HighLabel ?? "More";

    public static ChartColor SemanticColor(Chart chart, double ratio) {
        var t = chart.Options.Theme;
        if (ratio < 0.60) return ChartColorMath.Blend(t.Negative, t.Warning, ratio / 0.60 * 0.42);
        if (ratio < 0.80) return ChartColorMath.Blend(t.Warning, t.Positive, (ratio - 0.60) / 0.20 * 0.5);
        return ChartColorMath.Blend(t.Warning, t.Positive, 0.65 + (ratio - 0.80) / 0.20 * 0.35);
    }

    public static double Ratio(double value, double min, double max) {
        if (min >= -0.000001 && max <= 100.000001) return Clamp(value / 100, 0, 1);
        return Clamp((value - min) / Math.Max(0.000001, max - min), 0, 1);
    }

    public static string Status(double ratio) {
        if (ratio < 0.60) return "negative";
        if (ratio < 0.80) return "warning";
        return "positive";
    }

    public static ChartColor CalendarColor(Chart chart, ChartSeries series, ChartColor? pointColor, double value, double min, double max) {
        var ratio = CalendarRatio(value, min, max);
        var high = pointColor ?? series.Color ?? chart.Options.Theme.Positive;
        return ChartColorMath.Blend(chart.Options.Theme.PlotBackground, high, 0.30 + ratio * 0.70);
    }

    public static ChartColor CalendarEmptyColor(Chart chart) {
        var t = chart.Options.Theme;
        var light = ChartColorMath.RelativeLuminance(t.PlotBackground) > 0.70;
        return light ? ChartColorMath.Blend(t.PlotBackground, t.MutedText, 0.30) : ChartColorMath.Blend(t.PlotBackground, t.Grid, 0.72);
    }

    public static double CalendarRatio(double value, double min, double max) =>
        Clamp((value - min) / Math.Max(0.000001, max - min), 0, 1);

    public static ChartColor MapNoDataColor(Chart chart) =>
        chart.Options.MapColorScale?.NoDataColor ?? ChartColorMath.Blend(chart.Options.Theme.PlotBackground, chart.Options.Theme.Grid, 0.46);

    private static double Clamp(double value, double min, double max) {
        if (double.IsNaN(value)) return min;
        if (value < min) return min;
        return value > max ? max : value;
    }

    private static double ContinuousRatio(double value, double min, double max) =>
        Clamp((value - min) / Math.Max(0.000001, max - min), 0, 1);
}
