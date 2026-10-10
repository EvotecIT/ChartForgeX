using System;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Gets the canonical marker configuration for numeric point glyphs. Dotted maps support only the existing radius override.</summary>
    public ChartMarkerOptions Markers { get; } = new();

    /// <summary>Gets the radar form and fill configuration for this series.</summary>
    public ChartRadarOptions Radar { get; } = new();

    /// <summary>Configures marker geometry, visibility and paint without changing the source observations.</summary>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The current series.</returns>
    public ChartSeries ConfigureMarkers(Action<ChartMarkerOptions> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        configure(Markers); return this;
    }

    /// <summary>Configures the form and area opacity of a radar series.</summary>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The current series.</returns>
    public ChartSeries ConfigureRadar(Action<ChartRadarOptions> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        configure(Radar); return this;
    }

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
