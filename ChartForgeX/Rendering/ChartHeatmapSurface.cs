using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static class ChartHeatmapSurface {
    /// <summary>Resolves numeric cells from the shared prepared theme without a renderer-specific theme path.</summary>
    internal static ChartColor CellColor(Chart chart, VisualThemeColors colors, ChartColor? high, double value, double min, double max) =>
        CellBlend(chart, colors, high, value, min, max).Color;

    internal static ChartColorBlend CellBlend(Chart chart, VisualThemeColors colors, ChartColor? high, double value, double min, double max,
        SvgColorRole highRole = SvgColorRole.Series) => chart.Options.HeatmapRelativeScale && value == 0 && min >= 0
            ? ZeroBlend(colors) : ColorBlend(chart, colors, high, value, min, max, highRole);

    internal static ChartColor Color(Chart chart, VisualThemeColors colors, ChartColor? high, double value, double min, double max) =>
        ColorBlend(chart, colors, high, value, min, max).Color;

    internal static ChartColorBlend ColorBlend(Chart chart, VisualThemeColors colors, ChartColor? high, double value, double min, double max,
        SvgColorRole highRole = SvgColorRole.Series) {
        var ratio = Ratio(chart, value, min, max);
        if (chart.Options.HeatmapScale == ChartHeatmapScale.Semantic)
            return SemanticBlend(colors.Status.Critical.Fill, colors.Status.Medium.Fill, colors.Status.Pass.Fill, ratio);
        if (!high.HasValue && colors.SequentialRamp.Count > 0) return RampBlend(colors.SequentialRamp, ratio);
        return new ChartColorBlend(colors.Surface, SvgColorRole.Surface, high ?? colors.Palette[0], highRole, .18 + ratio * .82);
    }

    internal static ChartColor MapColor(Chart chart, VisualThemeColors colors, ChartColor? pointColor, ChartColor? highColor, double value, double min, double max) =>
        MapBlend(chart, colors, pointColor, highColor, value, min, max).Color;

    internal static ChartColorBlend MapBlend(Chart chart, VisualThemeColors colors, ChartColor? pointColor, ChartColor? highColor,
        double value, double min, double max, SvgColorRole highRole = SvgColorRole.Series) => pointColor.HasValue
            ? ChartColorBlend.Solid(pointColor.Value, SvgColorRole.Series)
            : chart.Options.MapColorScale?.BlendFor(value, min, max) ?? ColorBlend(chart, colors, highColor, value, min, max, highRole);

    internal static ChartColor MapNoDataColor(Chart chart, VisualThemeColors colors) =>
        MapNoDataBlend(chart, colors).Color;

    internal static ChartColorBlend MapNoDataBlend(Chart chart, VisualThemeColors colors) => chart.Options.MapColorScale?.NoDataColor is ChartColor color
        ? ChartColorBlend.Solid(color, SvgColorRole.Ramp) : new ChartColorBlend(colors.Surface, SvgColorRole.Surface, colors.Border, SvgColorRole.Grid, .46);

    internal static ChartColor CalendarColor(VisualThemeColors colors, ChartColor? high, double value, double min, double max) => CalendarBlend(colors, high, value, min, max).Color;

    internal static ChartColorBlend CalendarBlend(VisualThemeColors colors, ChartColor? high, double value, double min, double max,
        SvgColorRole highRole = SvgColorRole.Series) {
        var ratio = CalendarRatio(value, min, max);
        return !high.HasValue && colors.SequentialRamp.Count > 0 ? RampBlend(colors.SequentialRamp, ratio)
            : new ChartColorBlend(colors.Surface, SvgColorRole.Surface, high ?? colors.Palette[0], highRole, .30 + ratio * .70);
    }

    internal static ChartColor ZeroColor(VisualThemeColors colors) => ZeroBlend(colors).Color;
    internal static ChartColorBlend ZeroBlend(VisualThemeColors colors) => new(colors.Surface, SvgColorRole.Surface, colors.MutedForeground, SvgColorRole.Text, .14);
    internal static ChartColor CalendarEmptyColor(VisualThemeColors colors) => CalendarEmptyBlend(colors).Color;
    internal static ChartColorBlend CalendarEmptyBlend(VisualThemeColors colors) => new(colors.Surface, SvgColorRole.Surface, colors.MutedForeground, SvgColorRole.Text, .30);
    public static double CategoricalLabelFontSize(double configuredSize) => Math.Max(8, configuredSize);

    // Auto categorical labels keep the configured size in both renderers; Always may explicitly fit smaller text.
    public static bool CategoricalLabelFits(double cellWidth, double cellHeight, double textWidth, double textHeight) =>
        cellWidth >= textWidth + 12 && cellHeight >= textHeight + 10;

    /// <summary>
    /// Returns the colour of a matrix or hexbin heatmap cell. In a count heatmap (<see cref="ChartOptions.HeatmapRelativeScale"/>)
    /// a zero means nothing happened, so it takes the neutral <see cref="ZeroColor(Chart)"/> instead of the weakest ramp step.
    /// Maps keep using <see cref="Color(Chart, ChartColor?, double, double, double)"/>, where zero can be a real magnitude.
    /// </summary>
    public static ChartColor CellColor(Chart chart, ChartColor? highColor, double value, double min, double max) => CellBlend(chart, highColor, value, min, max).Color;

    /// <summary>Returns the colour of a matrix or hexbin heatmap cell as a blend, for SVG colour variables (see <see cref="CellColor(Chart, ChartColor?, double, double, double)"/>).</summary>
    public static ChartColorBlend CellBlend(Chart chart, ChartColor? highColor, double value, double min, double max) =>
        (chart.Options.HeatmapRelativeScale || chart.Options.Theme.UseGraphiteLayout) && value == 0 && min >= 0 ? ZeroBlend(chart) : ColorBlend(chart, highColor, value, min, max);

    /// <summary>
    /// Returns whether a matrix or hexbin heatmap cell is strong, so the text on it takes the surface colour
    /// (<see cref="ChartMarkText"/>). It follows the branches of <see cref="CellBlend(Chart, ChartColor?, double, double, double)"/> and depends on where the value sits
    /// on the scale, never on the colour of the cell in one theme: semantic cells are status colours, so strong; a neutral
    /// zero is weak; a ramp cell is strong from <see cref="StrongRampRatio"/> of the ramp, where token ramps (weakest step
    /// nearest the surface) have moved far enough from the surface in light and dark themes alike; a tint of a series
    /// colour is strong only near the full colour (<see cref="StrongTintAmount"/>).
    /// </summary>
    public static bool IsStrongCell(Chart chart, ChartColor? highColor, double value, double min, double max) {
        if (chart.Options.HeatmapRelativeScale && value == 0 && min >= 0) return false;
        if (chart.Options.HeatmapScale == ChartHeatmapScale.Semantic) return true;
        var ratio = Ratio(chart, value, min, max);
        if (!highColor.HasValue && chart.Options.Theme.SequentialRampValue != null) return ratio >= StrongRampRatio;
        return 0.18 + ratio * 0.82 >= StrongTintAmount;
    }

    /// <summary>The position on a sequential ramp (0 weakest, 1 strongest) from which cells count as strong.</summary>
    internal const double StrongRampRatio = 0.35;

    /// <summary>The share of the series colour in a surface-to-series tint from which cells count as strong.</summary>
    internal const double StrongTintAmount = 0.9;

    public static ChartColor Color(Chart chart, ChartColor? highColor, double value, double min, double max) => ColorBlend(chart, highColor, value, min, max).Color;

    /// <summary>Returns <see cref="Color(Chart, ChartColor?, double, double, double)"/> as a blend of its theme, series, or ramp colours.</summary>
    public static ChartColorBlend ColorBlend(Chart chart, ChartColor? highColor, double value, double min, double max) {
        var ratio = Ratio(chart, value, min, max);
        if (chart.Options.HeatmapScale == ChartHeatmapScale.Semantic) return SemanticBlend(chart, ratio);
        if (!highColor.HasValue && chart.Options.Theme.SequentialRampValue is { } ramp) return chart.Options.Theme.UseGraphiteLayout ? ChartColorBlend.Solid(ramp[Math.Max(1, Math.Min(ramp.Length - 1, (int)Math.Ceiling(ratio * (ramp.Length - 1))))], SvgColorRole.Ramp) : RampBlend(ramp, ratio);
        return new ChartColorBlend(chart.Options.Theme.PlotBackground, SvgColorRole.Surface, highColor ?? chart.Options.Theme.Palette[0], SvgColorRole.Series, 0.18 + ratio * 0.82);
    }

    public static ChartColor MapColor(Chart chart, ChartColor? pointColor, ChartColor highColor, double value, double min, double max) {
        var scale = chart.Options.MapColorScale;
        if (scale == null) return Color(chart, pointColor ?? highColor, value, min, max);
        if (pointColor.HasValue) return pointColor.Value;
        return scale.ColorFor(value, min, max);
    }

    public static double MapRatio(Chart chart, double value, double min, double max) {
        var scale = chart.Options.MapColorScale;
        if (scale == null) return Ratio(chart, value, min, max);
        if (scale.Mode == ChartColorScaleMode.Discrete)
            return scale.Bands.Count == 1 ? 0 : scale.BandIndex(value) / (double)(scale.Bands.Count - 1);
        var effectiveMin = scale.EffectiveMinimum(min);
        var effectiveMax = scale.EffectiveMaximum(max);
        return ContinuousRatio(value, effectiveMin, effectiveMax);
    }

    public static double MapScaleValue(Chart chart, double min, double max, double ratio) {
        var scale = chart.Options.MapColorScale;
        if (scale == null && chart.Options.HeatmapRelativeScale)
            return InterpolateObservedRange(Math.Min(0, min), max, Clamp(ratio, 0, 1));
        var effectiveMin = scale?.EffectiveMinimum(min) ?? min;
        var effectiveMax = scale?.EffectiveMaximum(max) ?? max;
        return ChartMath.InterpolateRange(effectiveMin, effectiveMax, Clamp(ratio, 0, 1));
    }

    /// <summary>The most swatches a horizontal map scale draws; scales with more stops are sampled evenly.</summary>
    public const int MaximumMapScaleSteps = 11;

    /// <summary>
    /// Returns the values of the swatches in a horizontal map scale. Two- and three-colour scales draw five evenly spaced
    /// swatches; a scale with more stops draws one swatch per stop (up to <see cref="MaximumMapScaleSteps"/>), each at the
    /// value where its stop is exact, so every step of a many-step ramp shows in the legend. When the midpoint of a
    /// diverging scale sits at the minimum or maximum, one arm has no range, so the swatches are spaced evenly instead.
    /// </summary>
    public static double[] MapScaleSteps(Chart chart, double min, double max) {
        if (chart.Options.MapColorScale?.Mode == ChartColorScaleMode.Discrete)
            throw new InvalidOperationException("Discrete scale legends draw bands rather than sampled numeric values.");
        var count = MapScaleStepCount(chart);
        var values = new double[count];
        if (SwatchPerStop(chart, min, max, out var scale)) {
            for (var i = 0; i < count; i++) values[i] = scale!.StopValue(i, min, max);
            return values;
        }

        for (var i = 0; i < count; i++) values[i] = MapScaleValue(chart, min, max, i / (double)(count - 1));
        return values;
    }

    /// <summary>Returns how many swatches a horizontal map scale draws (see <see cref="MapScaleSteps"/>).</summary>
    public static int MapScaleStepCount(Chart chart) {
        var stops = chart.Options.MapColorScale?.Colors.Count ?? 0;
        return stops <= 3 ? 5 : Math.Min(stops, MaximumMapScaleSteps);
    }

    /// <summary>
    /// Returns the swatch under the midpoint label of a horizontal map scale: the midpoint stop when each stop has its
    /// own swatch, the end swatch when a many-step scale's midpoint sits at that end, otherwise the middle swatch.
    /// </summary>
    public static int MapScaleMidpointStep(Chart chart, double min, double max, int stepCount) {
        if (SwatchPerStop(chart, min, max, out var scale) && scale!.MidpointIndex is int midpoint) return midpoint;
        if (scale != null && scale.Colors.Count > 3 && scale.MidpointCollapses(min, max)) {
            return scale.EffectiveMidpoint(min, max) <= scale.EffectiveMinimum(min) ? 0 : stepCount - 1;
        }

        return stepCount / 2;
    }

    private static bool SwatchPerStop(Chart chart, double min, double max, out ChartColorScale? scale) {
        scale = chart.Options.MapColorScale;
        return scale != null && scale.Colors.Count > 3 && scale.Colors.Count <= MaximumMapScaleSteps && !scale.MidpointCollapses(min, max);
    }

    public static double MapScaleMidpoint(Chart chart, double min, double max) {
        var scale = chart.Options.MapColorScale;
        if (scale == null) return MapScaleValue(chart, min, max, 0.5);
        return scale.EffectiveMidpoint(min, max);
    }

    public static string MapLowLabel(Chart chart) => chart.Options.MapColorScale?.LowLabel ?? chart.Options.Labels.Less;

    public static string? MapMidpointLabel(Chart chart) => chart.Options.MapColorScale?.MidpointLabel;

    public static string MapHighLabel(Chart chart) => chart.Options.MapColorScale?.HighLabel ?? chart.Options.Labels.More;

    public static ChartColor SemanticColor(Chart chart, double ratio) => SemanticBlend(chart, ratio).Color;

    private static ChartColorBlend SemanticBlend(Chart chart, double ratio) {
        var t = chart.Options.Theme;
        return SemanticBlend(t.Negative, t.Warning, t.Positive, ratio);
    }

    private static ChartColorBlend SemanticBlend(ChartColor negative, ChartColor warning, ChartColor positive, double ratio) {
        if (ratio < 0.60) return new ChartColorBlend(negative, SvgColorRole.Status, warning, SvgColorRole.Status, ratio / 0.60 * 0.42);
        if (ratio < 0.80) return new ChartColorBlend(warning, SvgColorRole.Status, positive, SvgColorRole.Status, (ratio - 0.60) / 0.20 * 0.5);
        return new ChartColorBlend(warning, SvgColorRole.Status, positive, SvgColorRole.Status, 0.65 + (ratio - 0.80) / 0.20 * 0.35);
    }

    /// <summary>
    /// Returns the colour ratio for a heatmap value. With <see cref="ChartOptions.HeatmapRelativeScale"/> the ratio is
    /// relative to the observed range (from zero for non-negative data) instead of treating 0–100 values as percentages.
    /// </summary>
    public static double Ratio(Chart chart, double value, double min, double max) {
        if (!chart.Options.HeatmapRelativeScale) return Ratio(value, min, max);
        var floor = Math.Min(0, min);
        var span = max - floor;
        if (span <= 0) return max == 0 ? 0 : 1;
        return ObservedRangeRatio(value, floor, max);
    }

    /// <summary>Interpolates piecewise-linearly along a ramp ordered weakest to strongest.</summary>
    public static ChartColor RampColor(IReadOnlyList<ChartColor> ramp, double ratio) => RampBlend(ramp, ratio).Color;

    /// <summary>Returns <see cref="RampColor"/> as a blend of the two ramp steps around the ratio.</summary>
    public static ChartColorBlend RampBlend(IReadOnlyList<ChartColor> ramp, double ratio) {
        if (ramp.Count == 1) return ChartColorBlend.Solid(ramp[0], SvgColorRole.Ramp);
        var position = Clamp(ratio, 0, 1) * (ramp.Count - 1);
        var index = Math.Min(ramp.Count - 2, (int)Math.Floor(position));
        return new ChartColorBlend(ramp[index], SvgColorRole.Ramp, ramp[index + 1], SvgColorRole.Ramp, position - index);
    }

    public static double Ratio(double value, double min, double max) {
        if (min >= -0.000001 && max <= 100.000001) return Clamp(value / 100, 0, 1);
        return Clamp((value - min) / Math.Max(0.000001, max - min), 0, 1);
    }

    /// <summary>
    /// Returns the positive/warning/negative status of a cell only for the <see cref="ChartHeatmapScale.Semantic"/> scale.
    /// Sequential (count and intensity) heatmaps carry no status; use <see cref="Level"/> instead.
    /// </summary>
    public static string? CellStatus(Chart chart, double ratio) => chart.Options.HeatmapScale == ChartHeatmapScale.Semantic ? Status(ratio) : null;

    /// <summary>Returns the neutral intensity level 0–4 (0 is the weakest) used for sequential heatmap cells and scale steps.</summary>
    public static int Level(double ratio) => (int)Math.Ceiling(Clamp(ratio, 0, 1) * 4);

    public static string Status(double ratio) {
        if (ratio < 0.60) return "negative";
        if (ratio < 0.80) return "warning";
        return "positive";
    }

    public static ChartColor CalendarColor(Chart chart, ChartSeries series, ChartColor? pointColor, double value, double min, double max) => CalendarBlend(chart, series, pointColor, value, min, max).Color;

    /// <summary>Returns <see cref="CalendarColor(Chart, ChartSeries, ChartColor?, double, double, double)"/> as a blend of its ramp, series, and surface colours.</summary>
    public static ChartColorBlend CalendarBlend(Chart chart, ChartSeries series, ChartColor? pointColor, double value, double min, double max) {
        var ratio = CalendarRatio(value, min, max);
        if (!pointColor.HasValue && !series.Color.HasValue && chart.Options.Theme.SequentialRampValue is { } ramp) return RampBlend(ramp, ratio);
        // Counts use a neutral single-hue ramp from the first categorical colour; status colours stay reserved for status.
        var high = pointColor ?? series.Color ?? chart.Options.Theme.Palette[0];
        return new ChartColorBlend(chart.Options.Theme.PlotBackground, SvgColorRole.Surface, high, SvgColorRole.Series, 0.30 + ratio * 0.70);
    }

    /// <summary>
    /// Returns the colour of a zero count: the surface behind the cells, shifted a little towards the muted text colour so
    /// the cell stays visible without reading as activity. It differs from <see cref="CalendarEmptyColor(Chart)"/>, which marks
    /// days without data.
    /// </summary>
    public static ChartColor ZeroColor(Chart chart) => ZeroBlend(chart).Color;

    /// <summary>Returns <see cref="ZeroColor(Chart)"/> as a blend of the backdrop and the muted text colour.</summary>
    public static ChartColorBlend ZeroBlend(Chart chart) => chart.Options.Theme.UseGraphiteLayout ? ChartColorBlend.Solid(chart.Options.Theme.Neutral3, SvgColorRole.Surface) : TowardsMutedText(chart, 0.14);

    /// <summary>
    /// Returns the colour of a calendar day without data: the surface behind the cells shifted further towards the muted
    /// text colour than a zero count, the same rule on light and dark themes.
    /// </summary>
    public static ChartColor CalendarEmptyColor(Chart chart) => CalendarEmptyBlend(chart).Color;

    /// <summary>Returns <see cref="CalendarEmptyColor(Chart)"/> as a blend of the backdrop and the muted text colour.</summary>
    public static ChartColorBlend CalendarEmptyBlend(Chart chart) => TowardsMutedText(chart, 0.30);

    /// <summary>Blends the backdrop towards the muted text colour, by <paramref name="share"/> of its opacity, into an opaque colour.</summary>
    private static ChartColorBlend TowardsMutedText(Chart chart, double share) {
        var ink = chart.Options.Theme.MutedText;
        var backdrop = ChartStateMark.Backdrop(chart);
        var amount = share * ink.A / 255.0;
        var result = ChartColor.FromRgb(
            (byte)Math.Round(backdrop.R + (ink.R - backdrop.R) * amount),
            (byte)Math.Round(backdrop.G + (ink.G - backdrop.G) * amount),
            (byte)Math.Round(backdrop.B + (ink.B - backdrop.B) * amount));
        return new ChartColorBlend(backdrop, SvgColorRole.Surface, ChartColor.FromRgb(ink.R, ink.G, ink.B), SvgColorRole.Text, amount, result);
    }

    public static double CalendarRatio(double value, double min, double max) =>
        max <= min ? 0 : ObservedRangeRatio(value, min, max);

    public static double InterpolateObservedRange(double min, double max, double ratio) => ChartMath.InterpolateRange(min, max, ratio);

    private static double ObservedRangeRatio(double value, double min, double max) {
        if (value <= min) return 0;
        if (value >= max) return 1;
        return Clamp(ChartMath.Normalize(value, min, max), 0, 1);
    }

    public static ChartColor MapNoDataColor(Chart chart) =>
        chart.Options.MapColorScale?.NoDataColor ?? ChartColorMath.Blend(chart.Options.Theme.PlotBackground, chart.Options.Theme.Grid, 0.46);

    private static double Clamp(double value, double min, double max) {
        if (double.IsNaN(value)) return min;
        if (value < min) return min;
        return value > max ? max : value;
    }

    private static double ContinuousRatio(double value, double min, double max) =>
        max <= min ? 0 : ObservedRangeRatio(value, min, max);
}
