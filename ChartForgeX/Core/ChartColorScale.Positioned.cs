using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

namespace ChartForgeX.Core;

public sealed partial class ChartColorScale {
    /// <summary>Gets detached absolute continuous anchors, or an empty list for unpositioned and discrete scales.</summary>
    public IReadOnlyList<ChartColorStop> Stops => Array.AsReadOnly(_stops);

    internal bool HasPositionedStops => _stops.Length > 0;

    /// <summary>Creates a sequential scale with strictly ascending absolute numeric anchors and a fixed endpoint domain.</summary>
    /// <param name="stops">At least two ordered value/color anchors. Values outside the endpoints use their colors.</param>
    /// <returns>An immutable continuous color scale.</returns>
    public static ChartColorScale Sequential(IEnumerable<ChartColorStop> stops) {
        var copy = MaterializeStops(stops, nameof(stops));
        if (copy.Count < 2) throw new ArgumentException("A sequential color scale needs at least two stops.", nameof(stops));
        return FromStops(copy.ToArray(), null);
    }

    /// <summary>Creates a diverging scale with absolute numeric anchors on both sides of a fixed midpoint anchor.</summary>
    /// <param name="lowStops">At least one ascending anchor, all below the midpoint.</param>
    /// <param name="midpoint">The exact midpoint value and color.</param>
    /// <param name="highStops">At least one ascending anchor, all above the midpoint.</param>
    /// <returns>An immutable continuous color scale with a fixed endpoint domain.</returns>
    public static ChartColorScale Diverging(IEnumerable<ChartColorStop> lowStops, ChartColorStop midpoint, IEnumerable<ChartColorStop> highStops) {
        if (midpoint == null) throw new ArgumentNullException(nameof(midpoint));
        var low = MaterializeStops(lowStops, nameof(lowStops));
        var high = MaterializeStops(highStops, nameof(highStops));
        if (low.Count == 0 || low[low.Count - 1].Value >= midpoint.Value)
            throw new ArgumentException("A diverging scale needs ascending low stops below its midpoint.", nameof(lowStops));
        if (high.Count == 0 || high[0].Value <= midpoint.Value)
            throw new ArgumentException("A diverging scale needs ascending high stops above its midpoint.", nameof(highStops));
        var copy = new List<ChartColorStop>(low.Count + 1 + high.Count);
        copy.AddRange(low); copy.Add(midpoint); copy.AddRange(high);
        return FromStops(copy.ToArray(), low.Count);
    }

    private static ChartColorScale FromStops(ChartColorStop[] stops, int? midpointIndex) {
        var colors = new ChartColor[stops.Length];
        for (var index = 0; index < stops.Length; index++) colors[index] = stops[index].Color;
        return new(colors, midpointIndex, midpointIndex.HasValue ? stops[midpointIndex.Value].Value : null,
            stops[0].Value, stops[stops.Length - 1].Value, null, null, null, null, stops: stops);
    }

    private static List<ChartColorStop> MaterializeStops(IEnumerable<ChartColorStop> stops, string parameterName) {
        if (stops == null) throw new ArgumentNullException(parameterName);
        var copy = new List<ChartColorStop>(stops);
        for (var index = 0; index < copy.Count; index++) {
            if (copy[index] == null) throw new ArgumentException("Color scale stops cannot be null.", parameterName);
            if (index > 0 && copy[index].Value <= copy[index - 1].Value)
                throw new ArgumentException("Color stop values must be strictly ascending.", parameterName);
        }
        return copy;
    }

    private ChartColorScale WithPositionedRange(double minimumValue, double maximumValue) {
        ValidateFinite(minimumValue, nameof(minimumValue)); ValidateFinite(maximumValue, nameof(maximumValue));
        if (maximumValue <= minimumValue)
            throw new ArgumentOutOfRangeException(nameof(maximumValue), maximumValue, "Color scale maximum must be greater than the minimum.");
        if (_stops.Length > 2 && minimumValue >= _stops[1].Value)
            throw new ArgumentOutOfRangeException(nameof(minimumValue), minimumValue, "The minimum must remain below every interior color stop.");
        if (_stops.Length > 2 && maximumValue <= _stops[_stops.Length - 2].Value)
            throw new ArgumentOutOfRangeException(nameof(maximumValue), maximumValue, "The maximum must remain above every interior color stop.");
        var stops = (ChartColorStop[])_stops.Clone();
        stops[0] = new ChartColorStop(minimumValue, LowColor);
        stops[stops.Length - 1] = new ChartColorStop(maximumValue, HighColor);
        return CopyStops(stops);
    }

    private ChartColorScale WithPositionedMidpoint(double midpointValue) {
        var index = _midpointIndex!.Value;
        if (midpointValue <= _stops[index - 1].Value || midpointValue >= _stops[index + 1].Value)
            throw new ArgumentOutOfRangeException(nameof(midpointValue), midpointValue, "The midpoint must remain between its neighboring color stops.");
        var stops = (ChartColorStop[])_stops.Clone();
        stops[index] = new ChartColorStop(midpointValue, _colors[index]);
        return CopyStops(stops);
    }

    private ChartColorScale CopyStops(ChartColorStop[] stops) =>
        new(_colors, _midpointIndex, _midpointIndex.HasValue ? stops[_midpointIndex.Value].Value : null,
            stops[0].Value, stops[stops.Length - 1].Value, NoDataColor, LowLabel, MidpointLabel, HighLabel, stops: stops);

    private ChartColorBlend AlongPositioned(double value) {
        if (value <= _stops[0].Value) return ChartColorBlend.Solid(LowColor, SvgColorRole.Ramp);
        for (var index = 1; index < _stops.Length; index++) {
            var stop = _stops[index];
            if (value == stop.Value) return ChartColorBlend.Solid(stop.Color, SvgColorRole.Ramp);
            if (value < stop.Value) return new ChartColorBlend(_colors[index - 1], SvgColorRole.Ramp, stop.Color,
                SvgColorRole.Ramp, Ratio(value, _stops[index - 1].Value, stop.Value));
        }
        return ChartColorBlend.Solid(HighColor, SvgColorRole.Ramp);
    }
}
