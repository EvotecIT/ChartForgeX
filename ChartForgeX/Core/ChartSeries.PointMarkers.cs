using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    private readonly List<ChartMarkerOptions?> _pointMarkers = new();
    internal bool HasPointMarkers => _pointMarkers.Any(value => value != null);

    /// <summary>Configures detached marker overrides for one Scatter observation. Unspecified fields inherit the series marker options.</summary>
    /// <param name="index">The zero-based existing numeric observation ordinal.</param>
    /// <param name="configure">Configures a copy; a throwing callback leaves the installed overrides unchanged. Explicit point colors retain fill precedence.</param>
    /// <returns>The current series.</returns>
    /// <remarks>Item replacement preserves these overrides. Insert/remove/keyed transactions remap them with source identity; clearing data removes them. Other families reject.</remarks>
    public ChartSeries ConfigurePointMarkers(int index, Action<ChartMarkerOptions> configure) {
        RequirePointMarkers(index);
        var current = index < _pointMarkers.Count ? _pointMarkers[index] : null;
        var candidate = ChartOptionConfiguration.Configure(current ?? new ChartMarkerOptions(), configure, options => options.Clone());
        while (_pointMarkers.Count <= index) _pointMarkers.Add(null);
        _pointMarkers[index] = candidate;
        return this;
    }

    /// <summary>Returns detached authored overrides for one Scatter observation, or null when it inherits the series markers.</summary>
    /// <param name="index">The zero-based existing numeric observation ordinal.</param>
    /// <returns>A mutable copy of only the authored overrides.</returns>
    public ChartMarkerOptions? GetPointMarkers(int index) {
        RequirePointMarkers(index);
        return index < _pointMarkers.Count ? _pointMarkers[index]?.Clone() : null;
    }

    /// <summary>Removes one Scatter observation's marker overrides without changing source data or identity.</summary>
    /// <param name="index">The zero-based existing numeric observation ordinal.</param>
    /// <returns>The current series.</returns>
    public ChartSeries UseSeriesMarkers(int index) {
        RequirePointMarkers(index);
        if (index < _pointMarkers.Count) _pointMarkers[index] = null;
        return this;
    }

    internal ChartMarkerOptions ResolvePointMarkers(int index) => Kind == ChartSeriesKind.Scatter
        && index >= 0 && index < _pointMarkers.Count && _pointMarkers[index] is { } overrides ? Markers.Overlay(overrides) : Markers;

    private void RequirePointMarkers(int index) {
        if (Kind != ChartSeriesKind.Scatter) throw new NotSupportedException("Per-observation marker overrides currently support Scatter series.");
        ValidatePointIndex(index);
    }
}
