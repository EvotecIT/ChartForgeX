using System;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Maps finite numeric values to continuous color stops or discrete bands. Continuous scales infer their domain
/// from source values unless fixed bounds are supplied; an inferred constant domain uses the low color.
/// </summary>
public sealed class ChartColorScale {
    private readonly ChartColor[] _colors;
    private readonly int? _midpointIndex;
    private readonly ChartColorBand[] _bands;

    /// <summary>Gets the color-selection mode.</summary>
    public ChartColorScaleMode Mode => _bands.Length > 0 ? ChartColorScaleMode.Discrete
        : _midpointIndex.HasValue ? ChartColorScaleMode.Diverging : ChartColorScaleMode.Sequential;

    /// <summary>Gets the detached read-only discrete bands, or an empty list for continuous scales.</summary>
    public IReadOnlyList<ChartColorBand> Bands => Array.AsReadOnly(_bands);

    /// <summary>
    /// Gets the low-value color: the first stop.
    /// </summary>
    public ChartColor LowColor => _colors[0];

    /// <summary>
    /// Gets the midpoint color of a diverging scale, or null for other modes.
    /// </summary>
    public ChartColor? MidpointColor => _midpointIndex.HasValue ? _colors[_midpointIndex.Value] : null;

    /// <summary>
    /// Gets the high-value color: the last stop.
    /// </summary>
    public ChartColor HighColor => _colors[_colors.Length - 1];

    /// <summary>
    /// Gets every continuous stop or discrete band color, ordered from the lowest to the highest value.
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
    /// Gets the optional color for missing data. Renderers supply their theme fallback when this is null.
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

    private ChartColorScale(
        ChartColor[] colors,
        int? midpointIndex,
        double? midpointValue,
        double? minimumValue,
        double? maximumValue,
        ChartColor? noDataColor,
        string? lowLabel,
        string? midpointLabel,
        string? highLabel,
        ChartColorBand[]? bands = null) {
        ValidateNullableFinite(midpointValue, nameof(midpointValue));
        ValidateNullableFinite(minimumValue, nameof(minimumValue));
        ValidateNullableFinite(maximumValue, nameof(maximumValue));
        if (minimumValue.HasValue && maximumValue.HasValue && maximumValue.Value <= minimumValue.Value) throw new ArgumentOutOfRangeException(nameof(maximumValue), maximumValue, "Color scale maximum must be greater than the minimum.");

        _colors = colors;
        _midpointIndex = midpointIndex;
        _bands = bands ?? Array.Empty<ChartColorBand>();
        MidpointValue = midpointValue;
        MinimumValue = minimumValue;
        MaximumValue = maximumValue;
        NoDataColor = noDataColor;
        LowLabel = NormalizeLabel(lowLabel);
        MidpointLabel = NormalizeLabel(midpointLabel);
        HighLabel = NormalizeLabel(highLabel);
    }

    /// <summary>
    /// Creates a two-color sequential scale.
    /// </summary>
    /// <param name="lowColor">The color for low values.</param>
    /// <param name="highColor">The color for high values.</param>
    /// <returns>A color scale.</returns>
    public static ChartColorScale Sequential(ChartColor lowColor, ChartColor highColor) =>
        new(new[] { lowColor, highColor }, null, null, null, null, null, null, null, null);

    /// <summary>
    /// Creates a sequential scale with evenly spaced colour stops, for example every step of a design-token ramp.
    /// </summary>
    /// <param name="colors">The colours from the lowest to the highest value; at least two.</param>
    /// <returns>A color scale.</returns>
    public static ChartColorScale Sequential(IEnumerable<ChartColor> colors) {
        var stops = Materialize(colors, nameof(colors));
        if (stops.Count < 2) throw new ArgumentException("A sequential color scale needs at least two colours.", nameof(colors));
        return new(stops.ToArray(), null, null, null, null, null, null, null, null);
    }

    /// <summary>
    /// Creates a three-color diverging scale.
    /// </summary>
    /// <param name="lowColor">The color for low values.</param>
    /// <param name="midpointColor">The color for the midpoint value.</param>
    /// <param name="highColor">The color for high values.</param>
    /// <param name="midpointValue">An optional midpoint value. When omitted, the midpoint is halfway between the effective minimum and maximum.</param>
    /// <returns>A color scale.</returns>
    public static ChartColorScale Diverging(ChartColor lowColor, ChartColor midpointColor, ChartColor highColor, double? midpointValue = null) =>
        new(new[] { lowColor, midpointColor, highColor }, 1, midpointValue, null, null, null, null, null, null);

    /// <summary>
    /// Creates a diverging scale with any number of steps on each side of the midpoint. The low colours are spread
    /// evenly from the minimum to the midpoint value and the high colours from the midpoint value to the maximum.
    /// </summary>
    /// <param name="lowColors">The colours below the midpoint, from the lowest value up to the step next to the midpoint; at least one.</param>
    /// <param name="midpointColor">The color for the midpoint value.</param>
    /// <param name="highColors">The colours above the midpoint, from the step next to the midpoint up to the highest value; at least one.</param>
    /// <param name="midpointValue">An optional midpoint value. When omitted, the midpoint is halfway between the effective minimum and maximum.</param>
    /// <returns>A color scale.</returns>
    public static ChartColorScale Diverging(IEnumerable<ChartColor> lowColors, ChartColor midpointColor, IEnumerable<ChartColor> highColors, double? midpointValue = null) {
        var low = Materialize(lowColors, nameof(lowColors));
        var high = Materialize(highColors, nameof(highColors));
        if (low.Count == 0) throw new ArgumentException("A diverging color scale needs at least one colour below the midpoint.", nameof(lowColors));
        if (high.Count == 0) throw new ArgumentException("A diverging color scale needs at least one colour above the midpoint.", nameof(highColors));
        var stops = new List<ChartColor>(low.Count + 1 + high.Count);
        stops.AddRange(low);
        stops.Add(midpointColor);
        stops.AddRange(high);
        return new(stops.ToArray(), low.Count, midpointValue, null, null, null, null, null, null);
    }

    /// <summary>Creates a discrete scale with strictly ascending exclusive upper bounds and one final unbounded band.</summary>
    /// <param name="bands">Ordered bands. Boundary equality selects the next band; the final upper bound must be null.</param>
    /// <returns>An immutable scale whose colors do not depend on the observed domain.</returns>
    public static ChartColorScale Discrete(IEnumerable<ChartColorBand> bands) {
        if (bands == null) throw new ArgumentNullException(nameof(bands));
        var copy = new List<ChartColorBand>(bands).ToArray();
        if (copy.Length == 0) throw new ArgumentException("A discrete color scale needs at least one band.", nameof(bands));
        var colors = new ChartColor[copy.Length];
        for (var index = 0; index < copy.Length; index++) {
            var band = copy[index] ?? throw new ArgumentException("Color scale bands cannot be null.", nameof(bands));
            if (band.UpperBound.HasValue != (index < copy.Length - 1))
                throw new ArgumentException("Only the final color band must have an unbounded upper limit.", nameof(bands));
            if (index > 0 && band.UpperBound.HasValue && band.UpperBound.Value <= copy[index - 1].UpperBound!.Value)
                throw new ArgumentException("Color band upper bounds must be strictly ascending.", nameof(bands));
            colors[index] = band.Color;
        }
        return new(colors, null, null, null, null, null, null, null, null, copy);
    }

    /// <summary>
    /// Creates a copy with explicit color interpolation bounds.
    /// </summary>
    /// <param name="minimumValue">The minimum value used by the color scale.</param>
    /// <param name="maximumValue">The maximum value used by the color scale.</param>
    /// <returns>A color scale.</returns>
    public ChartColorScale WithValueRange(double minimumValue, double maximumValue) {
        EnsureContinuous();
        return new(_colors, _midpointIndex, MidpointValue, minimumValue, maximumValue, NoDataColor, LowLabel, MidpointLabel, HighLabel);
    }

    /// <summary>
    /// Creates a copy with an explicit diverging midpoint value.
    /// </summary>
    /// <param name="midpointValue">The midpoint value used by the color scale.</param>
    /// <returns>A color scale.</returns>
    public ChartColorScale WithMidpoint(double midpointValue) {
        ValidateFinite(midpointValue, nameof(midpointValue));
        if (Mode != ChartColorScaleMode.Diverging) throw new InvalidOperationException("Only a diverging color scale has a configurable midpoint.");
        return new(_colors, _midpointIndex, midpointValue, MinimumValue, MaximumValue, NoDataColor, LowLabel, MidpointLabel, HighLabel);
    }

    /// <summary>
    /// Creates a copy with a custom no-data color.
    /// </summary>
    /// <param name="noDataColor">The color used for regions without values.</param>
    /// <returns>A color scale.</returns>
    public ChartColorScale WithNoDataColor(ChartColor noDataColor) =>
        new(_colors, _midpointIndex, MidpointValue, MinimumValue, MaximumValue, noDataColor, LowLabel, MidpointLabel, HighLabel, _bands);

    /// <summary>
    /// Creates a copy with custom legend labels.
    /// </summary>
    /// <param name="lowLabel">The low-end legend label.</param>
    /// <param name="midpointLabel">The optional midpoint legend label.</param>
    /// <param name="highLabel">The high-end legend label.</param>
    /// <returns>A color scale.</returns>
    public ChartColorScale WithLabels(string? lowLabel, string? midpointLabel, string? highLabel) {
        EnsureContinuous();
        return new(_colors, _midpointIndex, MidpointValue, MinimumValue, MaximumValue, NoDataColor, lowLabel, midpointLabel, highLabel);
    }

    /// <summary>Resolves a finite value using discrete bands or the continuous scale's explicit domain.</summary>
    /// <param name="value">The finite numeric value to color.</param>
    /// <returns>The selected color. Continuous scales without fixed bounds require the source-domain overload.</returns>
    public ChartColor ColorFor(double value) {
        ValidateFinite(value, nameof(value));
        if (Mode == ChartColorScaleMode.Discrete) return _bands[BandIndex(value)].Color;
        if (!MinimumValue.HasValue || !MaximumValue.HasValue)
            throw new InvalidOperationException("A continuous color scale requires source bounds or an explicit value range.");
        return ColorFor(value, MinimumValue.Value, MaximumValue.Value);
    }

    /// <summary>
    /// Resolves a value to a color within this scale.
    /// </summary>
    /// <param name="value">The value to color.</param>
    /// <param name="sourceMinimum">The minimum source value when the scale has no explicit minimum.</param>
    /// <param name="sourceMaximum">The maximum source value when the scale has no explicit maximum.</param>
    /// <returns>The selected color. Discrete bands ignore the finite ordered source domain; a constant continuous domain uses the low color.</returns>
    public ChartColor ColorFor(double value, double sourceMinimum, double sourceMaximum) => BlendFor(value, sourceMinimum, sourceMaximum).Color;

    /// <summary>Resolves the same scale decision while retaining its stop provenance for prepared SVG.</summary>
    internal ChartColorBlend BlendFor(double value, double sourceMinimum, double sourceMaximum) {
        ValidateFinite(value, nameof(value));
        ValidateSourceRange(sourceMinimum, sourceMaximum);
        if (Mode == ChartColorScaleMode.Discrete) return ChartColorBlend.Solid(_bands[BandIndex(value)].Color, SvgColorRole.Ramp);
        var (min, max) = ResolveRange(sourceMinimum, sourceMaximum);
        if (max == min) return ChartColorBlend.Solid(LowColor, SvgColorRole.Ramp);

        var last = _colors.Length - 1;
        if (!_midpointIndex.HasValue) return Along(0, last, Ratio(value, min, max));

        var midpointIndex = _midpointIndex.Value;
        var midpoint = MidpointValue ?? ChartMath.InterpolateRange(min, max, .5);
        midpoint = Math.Max(min, Math.Min(max, midpoint));
        if (value <= midpoint) return Along(0, midpointIndex, Ratio(value, min, midpoint));
        return Along(midpointIndex, last, Ratio(value, midpoint, max));
    }

    /// <summary>
    /// Gets the effective minimum value used by this scale.
    /// </summary>
    /// <param name="sourceMinimum">The source data minimum.</param>
    /// <returns>The effective minimum.</returns>
    public double EffectiveMinimum(double sourceMinimum) {
        EnsureContinuous();
        ValidateFinite(sourceMinimum, nameof(sourceMinimum));
        return MinimumValue ?? sourceMinimum;
    }

    /// <summary>
    /// Gets the effective maximum value used by this scale.
    /// </summary>
    /// <param name="sourceMaximum">The source data maximum.</param>
    /// <returns>The effective maximum.</returns>
    public double EffectiveMaximum(double sourceMaximum) {
        EnsureContinuous();
        ValidateFinite(sourceMaximum, nameof(sourceMaximum));
        return MaximumValue ?? sourceMaximum;
    }

    /// <summary>
    /// Gets the effective midpoint value used by this scale.
    /// </summary>
    /// <param name="sourceMinimum">The source data minimum.</param>
    /// <param name="sourceMaximum">The source data maximum.</param>
    /// <returns>The effective midpoint.</returns>
    public double EffectiveMidpoint(double sourceMinimum, double sourceMaximum) {
        var (min, max) = ResolveRange(sourceMinimum, sourceMaximum);
        return Math.Max(min, Math.Min(max, MidpointValue ?? ChartMath.InterpolateRange(min, max, .5)));
    }

    /// <summary>Gets the index of the midpoint stop, or null for a sequential scale.</summary>
    internal int? MidpointIndex => _midpointIndex;

    /// <summary>Returns the value at which the stop <paramref name="index"/> is drawn exactly.</summary>
    internal double StopValue(int index, double sourceMinimum, double sourceMaximum) {
        var (min, max) = ResolveRange(sourceMinimum, sourceMaximum);
        var last = _colors.Length - 1;
        if (!_midpointIndex.HasValue) return ChartMath.InterpolateRange(min, max, index / (double)last);
        var midpointIndex = _midpointIndex.Value;
        var midpoint = EffectiveMidpoint(sourceMinimum, sourceMaximum);
        return index <= midpointIndex
            ? ChartMath.InterpolateRange(min, midpoint, index / (double)midpointIndex)
            : ChartMath.InterpolateRange(midpoint, max, (index - midpointIndex) / (double)(last - midpointIndex));
    }

    // Measured from the first stop of the arm, so a one-step arm blends by the ratio itself, exactly as the two- and
    // three-colour scales always have (adding and subtracting the arm offset would change rounded channels).
    private ChartColorBlend Along(int from, int to, double ratio) {
        var scaled = (to - from) * ratio;
        var step = (int)Math.Floor(scaled);
        if (step >= to - from) return ChartColorBlend.Solid(_colors[to], SvgColorRole.Ramp);
        var first = _colors[from + step]; var second = _colors[from + step + 1]; var amount = scaled - step;
        return new ChartColorBlend(first, SvgColorRole.Ramp, second, SvgColorRole.Ramp, amount);
    }

    /// <summary>
    /// Returns true when the midpoint of a diverging scale sits at the minimum or maximum, so one arm has no range and its
    /// stops would all be drawn at the same value.
    /// </summary>
    internal bool MidpointCollapses(double sourceMinimum, double sourceMaximum) {
        if (!_midpointIndex.HasValue) return false;
        var (min, max) = ResolveRange(sourceMinimum, sourceMaximum);
        var midpoint = EffectiveMidpoint(sourceMinimum, sourceMaximum);
        return midpoint <= min || midpoint >= max;
    }

    private static double Ratio(double value, double min, double max) {
        if (max <= min) return 1;
        if (value < min) return 0;
        if (value >= max) return 1;
        if (value == min) return 0;
        return Math.Max(0, Math.Min(1, ChartMath.Normalize(value, min, max)));
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
        if (value.HasValue) ValidateFinite(value.Value, parameterName);
    }

    private static void ValidateFinite(double value, string parameterName) {
        if (!ChartMath.IsFinite(value)) throw new ArgumentOutOfRangeException(parameterName, value, "Color scale values must be finite.");
    }

    private static void ValidateSourceRange(double sourceMinimum, double sourceMaximum) {
        ValidateFinite(sourceMinimum, nameof(sourceMinimum));
        ValidateFinite(sourceMaximum, nameof(sourceMaximum));
        if (sourceMaximum < sourceMinimum) throw new ArgumentOutOfRangeException(nameof(sourceMaximum), sourceMaximum, "Source color scale bounds must be ordered.");
    }

    private (double Minimum, double Maximum) ResolveRange(double sourceMinimum, double sourceMaximum) {
        EnsureContinuous();
        ValidateSourceRange(sourceMinimum, sourceMaximum);
        return (MinimumValue ?? sourceMinimum, MaximumValue ?? sourceMaximum);
    }

    private void EnsureContinuous() {
        if (Mode == ChartColorScaleMode.Discrete) throw new InvalidOperationException("Discrete color scales use band bounds and names rather than a continuous domain or endpoint labels.");
    }

    /// <summary>Finds a finite value's band using exclusive upper bounds.</summary>
    internal int BandIndex(double value) {
        ValidateFinite(value, nameof(value));
        if (Mode != ChartColorScaleMode.Discrete) throw new InvalidOperationException("Only a discrete color scale has numeric bands.");
        for (var index = 0; index < _bands.Length - 1; index++)
            if (value < _bands[index].UpperBound!.Value) return index;
        return _bands.Length - 1;
    }
}
