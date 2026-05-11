using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Defines the value-to-color ramp used by region and tile maps.
/// </summary>
public sealed class ChartMapColorScale {
    /// <summary>
    /// Gets the low-value color.
    /// </summary>
    public ChartColor LowColor { get; }

    /// <summary>
    /// Gets the optional midpoint color for diverging map scales.
    /// </summary>
    public ChartColor? MidpointColor { get; }

    /// <summary>
    /// Gets the high-value color.
    /// </summary>
    public ChartColor HighColor { get; }

    /// <summary>
    /// Gets the optional midpoint value. When omitted, the midpoint is halfway between the effective minimum and maximum.
    /// </summary>
    public double? MidpointValue { get; }

    /// <summary>
    /// Gets the optional minimum value used for color interpolation.
    /// </summary>
    public double? MinimumValue { get; }

    /// <summary>
    /// Gets the optional maximum value used for color interpolation.
    /// </summary>
    public double? MaximumValue { get; }

    /// <summary>
    /// Gets the optional color used for regions with no data.
    /// </summary>
    public ChartColor? NoDataColor { get; }

    /// <summary>
    /// Gets the optional low-end legend label.
    /// </summary>
    public string? LowLabel { get; }

    /// <summary>
    /// Gets the optional midpoint legend label.
    /// </summary>
    public string? MidpointLabel { get; }

    /// <summary>
    /// Gets the optional high-end legend label.
    /// </summary>
    public string? HighLabel { get; }

    private ChartMapColorScale(
        ChartColor lowColor,
        ChartColor? midpointColor,
        ChartColor highColor,
        double? midpointValue,
        double? minimumValue,
        double? maximumValue,
        ChartColor? noDataColor,
        string? lowLabel,
        string? midpointLabel,
        string? highLabel) {
        ValidateNullableFinite(midpointValue, nameof(midpointValue));
        ValidateNullableFinite(minimumValue, nameof(minimumValue));
        ValidateNullableFinite(maximumValue, nameof(maximumValue));
        if (minimumValue.HasValue && maximumValue.HasValue && maximumValue.Value <= minimumValue.Value) throw new ArgumentOutOfRangeException(nameof(maximumValue), maximumValue, "Map color scale maximum must be greater than the minimum.");

        LowColor = lowColor;
        MidpointColor = midpointColor;
        HighColor = highColor;
        MidpointValue = midpointValue;
        MinimumValue = minimumValue;
        MaximumValue = maximumValue;
        NoDataColor = noDataColor;
        LowLabel = NormalizeLabel(lowLabel);
        MidpointLabel = NormalizeLabel(midpointLabel);
        HighLabel = NormalizeLabel(highLabel);
    }

    /// <summary>
    /// Creates a two-color sequential map scale.
    /// </summary>
    /// <param name="lowColor">The color for low values.</param>
    /// <param name="highColor">The color for high values.</param>
    /// <returns>A map color scale.</returns>
    public static ChartMapColorScale Sequential(ChartColor lowColor, ChartColor highColor) =>
        new(lowColor, null, highColor, null, null, null, null, null, null, null);

    /// <summary>
    /// Creates a three-color diverging map scale.
    /// </summary>
    /// <param name="lowColor">The color for low values.</param>
    /// <param name="midpointColor">The color for the midpoint value.</param>
    /// <param name="highColor">The color for high values.</param>
    /// <param name="midpointValue">An optional midpoint value. When omitted, the midpoint is halfway between the effective minimum and maximum.</param>
    /// <returns>A map color scale.</returns>
    public static ChartMapColorScale Diverging(ChartColor lowColor, ChartColor midpointColor, ChartColor highColor, double? midpointValue = null) =>
        new(lowColor, midpointColor, highColor, midpointValue, null, null, null, null, null, null);

    /// <summary>
    /// Creates a copy with explicit color interpolation bounds.
    /// </summary>
    /// <param name="minimumValue">The minimum value used by the color scale.</param>
    /// <param name="maximumValue">The maximum value used by the color scale.</param>
    /// <returns>A map color scale.</returns>
    public ChartMapColorScale WithValueRange(double minimumValue, double maximumValue) =>
        new(LowColor, MidpointColor, HighColor, MidpointValue, minimumValue, maximumValue, NoDataColor, LowLabel, MidpointLabel, HighLabel);

    /// <summary>
    /// Creates a copy with an explicit diverging midpoint value.
    /// </summary>
    /// <param name="midpointValue">The midpoint value used by the color scale.</param>
    /// <returns>A map color scale.</returns>
    public ChartMapColorScale WithMidpoint(double midpointValue) =>
        new(LowColor, MidpointColor, HighColor, midpointValue, MinimumValue, MaximumValue, NoDataColor, LowLabel, MidpointLabel, HighLabel);

    /// <summary>
    /// Creates a copy with a custom no-data color.
    /// </summary>
    /// <param name="noDataColor">The color used for regions without values.</param>
    /// <returns>A map color scale.</returns>
    public ChartMapColorScale WithNoDataColor(ChartColor noDataColor) =>
        new(LowColor, MidpointColor, HighColor, MidpointValue, MinimumValue, MaximumValue, noDataColor, LowLabel, MidpointLabel, HighLabel);

    /// <summary>
    /// Creates a copy with custom legend labels.
    /// </summary>
    /// <param name="lowLabel">The low-end legend label.</param>
    /// <param name="midpointLabel">The optional midpoint legend label.</param>
    /// <param name="highLabel">The high-end legend label.</param>
    /// <returns>A map color scale.</returns>
    public ChartMapColorScale WithLabels(string? lowLabel, string? midpointLabel, string? highLabel) =>
        new(LowColor, MidpointColor, HighColor, MidpointValue, MinimumValue, MaximumValue, NoDataColor, lowLabel, midpointLabel, highLabel);

    /// <summary>
    /// Resolves a value to a color within this scale.
    /// </summary>
    /// <param name="value">The value to color.</param>
    /// <param name="sourceMinimum">The minimum source value when the scale has no explicit minimum.</param>
    /// <param name="sourceMaximum">The maximum source value when the scale has no explicit maximum.</param>
    /// <returns>The interpolated color.</returns>
    public ChartColor ColorFor(double value, double sourceMinimum, double sourceMaximum) {
        var min = EffectiveMinimum(sourceMinimum);
        var max = EffectiveMaximum(sourceMaximum);
        if (max <= min + 0.000001) max = min + 1;

        if (!MidpointColor.HasValue) return Blend(LowColor, HighColor, Ratio(value, min, max));

        var midpoint = MidpointValue ?? (min + max) / 2.0;
        midpoint = Math.Max(min, Math.Min(max, midpoint));
        if (value <= midpoint) return Blend(LowColor, MidpointColor.Value, Ratio(value, min, midpoint));
        return Blend(MidpointColor.Value, HighColor, Ratio(value, midpoint, max));
    }

    /// <summary>
    /// Gets the effective minimum value used by this scale.
    /// </summary>
    /// <param name="sourceMinimum">The source data minimum.</param>
    /// <returns>The effective minimum.</returns>
    public double EffectiveMinimum(double sourceMinimum) => MinimumValue ?? sourceMinimum;

    /// <summary>
    /// Gets the effective maximum value used by this scale.
    /// </summary>
    /// <param name="sourceMaximum">The source data maximum.</param>
    /// <returns>The effective maximum.</returns>
    public double EffectiveMaximum(double sourceMaximum) => MaximumValue ?? sourceMaximum;

    /// <summary>
    /// Gets the effective midpoint value used by this scale.
    /// </summary>
    /// <param name="sourceMinimum">The source data minimum.</param>
    /// <param name="sourceMaximum">The source data maximum.</param>
    /// <returns>The effective midpoint.</returns>
    public double EffectiveMidpoint(double sourceMinimum, double sourceMaximum) {
        var min = EffectiveMinimum(sourceMinimum);
        var max = EffectiveMaximum(sourceMaximum);
        if (max <= min + 0.000001) max = min + 1;
        return Math.Max(min, Math.Min(max, MidpointValue ?? (min + max) / 2.0));
    }

    private static double Ratio(double value, double min, double max) {
        if (max <= min + 0.000001) return 1;
        var ratio = (value - min) / (max - min);
        if (double.IsNaN(ratio)) return 0;
        if (ratio < 0) return 0;
        return ratio > 1 ? 1 : ratio;
    }

    private static ChartColor Blend(ChartColor a, ChartColor b, double amount) {
        amount = Math.Max(0, Math.Min(1, amount));
        return ChartColor.FromRgba(
            (byte)Math.Round(a.R + (b.R - a.R) * amount),
            (byte)Math.Round(a.G + (b.G - a.G) * amount),
            (byte)Math.Round(a.B + (b.B - a.B) * amount),
            (byte)Math.Round(a.A + (b.A - a.A) * amount));
    }

    private static string? NormalizeLabel(string? value) {
        if (value == null) return null;
        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static void ValidateNullableFinite(double? value, string parameterName) {
        if (value.HasValue && (double.IsNaN(value.Value) || double.IsInfinity(value.Value))) throw new ArgumentOutOfRangeException(parameterName, value, "Map color scale values must be finite.");
    }
}
