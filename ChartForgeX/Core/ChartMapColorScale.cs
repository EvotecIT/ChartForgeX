using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Defines the value-to-color ramp used by region and tile maps. A scale holds two or more colour stops from the
/// lowest to the highest value; a diverging scale also marks the stop drawn at its midpoint value.
/// </summary>
public sealed class ChartMapColorScale {
    private readonly ChartColor[] _colors;
    private readonly int? _midpointIndex;

    /// <summary>
    /// Gets the low-value color: the first stop.
    /// </summary>
    public ChartColor LowColor => _colors[0];

    /// <summary>
    /// Gets the midpoint color of a diverging map scale, or null for a sequential scale.
    /// </summary>
    public ChartColor? MidpointColor => _midpointIndex.HasValue ? _colors[_midpointIndex.Value] : null;

    /// <summary>
    /// Gets the high-value color: the last stop.
    /// </summary>
    public ChartColor HighColor => _colors[_colors.Length - 1];

    /// <summary>
    /// Gets every colour stop from the lowest to the highest value, including the midpoint colour of a diverging scale.
    /// </summary>
    public IReadOnlyList<ChartColor> Colors => Array.AsReadOnly(_colors);

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
        ChartColor[] colors,
        int? midpointIndex,
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

        _colors = colors;
        _midpointIndex = midpointIndex;
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
        new(new[] { lowColor, highColor }, null, null, null, null, null, null, null, null);

    /// <summary>
    /// Creates a sequential map scale with evenly spaced colour stops, for example every step of a design-token ramp.
    /// </summary>
    /// <param name="colors">The colours from the lowest to the highest value; at least two.</param>
    /// <returns>A map color scale.</returns>
    public static ChartMapColorScale Sequential(IEnumerable<ChartColor> colors) {
        var stops = Materialize(colors, nameof(colors));
        if (stops.Count < 2) throw new ArgumentException("A sequential map color scale needs at least two colours.", nameof(colors));
        return new(stops.ToArray(), null, null, null, null, null, null, null, null);
    }

    /// <summary>
    /// Creates a three-color diverging map scale.
    /// </summary>
    /// <param name="lowColor">The color for low values.</param>
    /// <param name="midpointColor">The color for the midpoint value.</param>
    /// <param name="highColor">The color for high values.</param>
    /// <param name="midpointValue">An optional midpoint value. When omitted, the midpoint is halfway between the effective minimum and maximum.</param>
    /// <returns>A map color scale.</returns>
    public static ChartMapColorScale Diverging(ChartColor lowColor, ChartColor midpointColor, ChartColor highColor, double? midpointValue = null) =>
        new(new[] { lowColor, midpointColor, highColor }, 1, midpointValue, null, null, null, null, null, null);

    /// <summary>
    /// Creates a diverging map scale with any number of steps on each side of the midpoint. The low colours are spread
    /// evenly from the minimum to the midpoint value and the high colours from the midpoint value to the maximum.
    /// </summary>
    /// <param name="lowColors">The colours below the midpoint, from the lowest value up to the step next to the midpoint; at least one.</param>
    /// <param name="midpointColor">The color for the midpoint value.</param>
    /// <param name="highColors">The colours above the midpoint, from the step next to the midpoint up to the highest value; at least one.</param>
    /// <param name="midpointValue">An optional midpoint value. When omitted, the midpoint is halfway between the effective minimum and maximum.</param>
    /// <returns>A map color scale.</returns>
    public static ChartMapColorScale Diverging(IEnumerable<ChartColor> lowColors, ChartColor midpointColor, IEnumerable<ChartColor> highColors, double? midpointValue = null) {
        var low = Materialize(lowColors, nameof(lowColors));
        var high = Materialize(highColors, nameof(highColors));
        if (low.Count == 0) throw new ArgumentException("A diverging map color scale needs at least one colour below the midpoint.", nameof(lowColors));
        if (high.Count == 0) throw new ArgumentException("A diverging map color scale needs at least one colour above the midpoint.", nameof(highColors));
        var stops = new List<ChartColor>(low.Count + 1 + high.Count);
        stops.AddRange(low);
        stops.Add(midpointColor);
        stops.AddRange(high);
        return new(stops.ToArray(), low.Count, midpointValue, null, null, null, null, null, null);
    }

    /// <summary>
    /// Creates a copy with explicit color interpolation bounds.
    /// </summary>
    /// <param name="minimumValue">The minimum value used by the color scale.</param>
    /// <param name="maximumValue">The maximum value used by the color scale.</param>
    /// <returns>A map color scale.</returns>
    public ChartMapColorScale WithValueRange(double minimumValue, double maximumValue) =>
        new(_colors, _midpointIndex, MidpointValue, minimumValue, maximumValue, NoDataColor, LowLabel, MidpointLabel, HighLabel);

    /// <summary>
    /// Creates a copy with an explicit diverging midpoint value.
    /// </summary>
    /// <param name="midpointValue">The midpoint value used by the color scale.</param>
    /// <returns>A map color scale.</returns>
    public ChartMapColorScale WithMidpoint(double midpointValue) =>
        new(_colors, _midpointIndex, midpointValue, MinimumValue, MaximumValue, NoDataColor, LowLabel, MidpointLabel, HighLabel);

    /// <summary>
    /// Creates a copy with a custom no-data color.
    /// </summary>
    /// <param name="noDataColor">The color used for regions without values.</param>
    /// <returns>A map color scale.</returns>
    public ChartMapColorScale WithNoDataColor(ChartColor noDataColor) =>
        new(_colors, _midpointIndex, MidpointValue, MinimumValue, MaximumValue, noDataColor, LowLabel, MidpointLabel, HighLabel);

    /// <summary>
    /// Creates a copy with custom legend labels.
    /// </summary>
    /// <param name="lowLabel">The low-end legend label.</param>
    /// <param name="midpointLabel">The optional midpoint legend label.</param>
    /// <param name="highLabel">The high-end legend label.</param>
    /// <returns>A map color scale.</returns>
    public ChartMapColorScale WithLabels(string? lowLabel, string? midpointLabel, string? highLabel) =>
        new(_colors, _midpointIndex, MidpointValue, MinimumValue, MaximumValue, NoDataColor, lowLabel, midpointLabel, highLabel);

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

        var last = _colors.Length - 1;
        if (!_midpointIndex.HasValue) return Along(0, last, Ratio(value, min, max));

        var midpointIndex = _midpointIndex.Value;
        var midpoint = MidpointValue ?? (min + max) / 2.0;
        midpoint = Math.Max(min, Math.Min(max, midpoint));
        if (value <= midpoint) return Along(0, midpointIndex, Ratio(value, min, midpoint));
        return Along(midpointIndex, last, Ratio(value, midpoint, max));
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

    /// <summary>Gets the index of the midpoint stop, or null for a sequential scale.</summary>
    internal int? MidpointIndex => _midpointIndex;

    /// <summary>Returns the value at which the stop <paramref name="index"/> is drawn exactly.</summary>
    internal double StopValue(int index, double sourceMinimum, double sourceMaximum) {
        var min = EffectiveMinimum(sourceMinimum);
        var max = EffectiveMaximum(sourceMaximum);
        if (max <= min + 0.000001) max = min + 1;
        var last = _colors.Length - 1;
        if (!_midpointIndex.HasValue) return min + (max - min) * index / last;
        var midpointIndex = _midpointIndex.Value;
        var midpoint = EffectiveMidpoint(sourceMinimum, sourceMaximum);
        return index <= midpointIndex
            ? min + (midpoint - min) * index / midpointIndex
            : midpoint + (max - midpoint) * (index - midpointIndex) / (last - midpointIndex);
    }

    // Measured from the first stop of the arm, so a one-step arm blends by the ratio itself, exactly as the two- and
    // three-colour scales always have (adding and subtracting the arm offset would change rounded channels).
    private ChartColor Along(int from, int to, double ratio) {
        var scaled = (to - from) * ratio;
        var step = (int)Math.Floor(scaled);
        if (step >= to - from) return _colors[to];
        return Blend(_colors[from + step], _colors[from + step + 1], scaled - step);
    }

    /// <summary>
    /// Returns true when the midpoint of a diverging scale sits at the minimum or maximum, so one arm has no range and its
    /// stops would all be drawn at the same value.
    /// </summary>
    internal bool MidpointCollapses(double sourceMinimum, double sourceMaximum) {
        if (!_midpointIndex.HasValue) return false;
        var min = EffectiveMinimum(sourceMinimum);
        var max = EffectiveMaximum(sourceMaximum);
        if (max <= min + 0.000001) max = min + 1;
        var midpoint = EffectiveMidpoint(sourceMinimum, sourceMaximum);
        return midpoint <= min + 0.000001 || midpoint >= max - 0.000001;
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

    private static List<ChartColor> Materialize(IEnumerable<ChartColor> colors, string parameterName) {
        if (colors == null) throw new ArgumentNullException(parameterName);
        return new List<ChartColor>(colors);
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
