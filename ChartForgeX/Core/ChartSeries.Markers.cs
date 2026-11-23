using System;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Gets the canonical marker configuration for numeric point glyphs. Dotted maps support only the existing radius override.</summary>
    public ChartMarkerOptions Markers {
        get => _markers;
        set => _markers = value ?? throw new ArgumentNullException(nameof(value));
    }
    private ChartMarkerOptions _markers = new();

    /// <summary>Gets the radar form, fill and outline configuration for this series.</summary>
    public ChartRadarOptions Radar {
        get => _radar;
        set => _radar = value ?? throw new ArgumentNullException(nameof(value));
    }
    private ChartRadarOptions _radar = new();

    internal void ValidateMarkerAndRadarOptions() {
        if (Kind == ChartSeriesKind.Bubble && Markers.Radius.HasValue)
            throw new InvalidOperationException("Bubble series use ChartOptions.Bubble radius bounds; use Markers.Enabled to hide glyphs.");
        if (Markers.IsConfigured && !ChartSeriesKindTraits.SupportsMarkers(Kind)
            && (Kind != ChartSeriesKind.DottedMap || Markers.HasNonRadiusConfiguration))
            throw new InvalidOperationException("Series '" + Name + "' of kind " + Kind + " does not support marker options.");
        if (Radar.IsConfigured && Kind != ChartSeriesKind.Radar)
            throw new InvalidOperationException("Series '" + Name + "' requires the Radar kind for radar options.");
        if (Kind == ChartSeriesKind.Radar && Radar.Form == ChartLineAreaForm.Line && Radar.FillOpacity.HasValue)
            throw new InvalidOperationException("Series '" + Name + "' requires the radar Area form for fill opacity.");
    }
}
