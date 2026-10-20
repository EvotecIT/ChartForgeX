using System;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    private ChartColorScale? _colorScale;

    /// <summary>
    /// Gets or sets the numeric color scale for Scatter, Bubble, DottedMap or RegionMap observations.
    /// Null preserves ordinary paint while retaining authored color measurements.
    /// </summary>
    public ChartColorScale? ColorScale {
        get => _colorScale;
        set {
            if (value != null && !SupportsNumericColor)
                throw new InvalidOperationException("Series '" + Name + "' requires Scatter, Bubble, DottedMap or RegionMap for numeric color scales.");
            _colorScale = value;
        }
    }

    /// <summary>Sets this series' independent numeric color scale.</summary>
    /// <param name="scale">The immutable scale, or null to restore ordinary paint without clearing source color measurements.</param>
    /// <returns>The current series.</returns>
    public ChartSeries WithColorScale(ChartColorScale? scale) {
        ColorScale = scale;
        return this;
    }

    private bool SupportsNumericColor => Kind is ChartSeriesKind.Scatter or ChartSeriesKind.Bubble or ChartSeriesKind.DottedMap or ChartSeriesKind.RegionMap;

    /// <summary>Rejects color facts that have no logical observation in the selected family.</summary>
    internal void ValidateColorValues(ChartOptions? options = null) {
        var supported = SupportsNumericColor;
        if (ColorDimension.HasOverrides && !supported)
            throw new InvalidOperationException("Series '" + Name + "' requires Scatter, Bubble, DottedMap or RegionMap for color presentation.");
        if (supported) ColorDimension.ValidateLegend(ColorScale ?? (Kind == ChartSeriesKind.RegionMap ? options?.MapColorScale : null));
        for (var index = 0; index < RenderPoints.Count; index++) {
            if (!RenderPoints[index].ColorValue.HasValue) continue;
            if (!supported)
                throw new InvalidOperationException("Series '" + Name + "' requires the Scatter or Bubble kind for numeric color observations.");
            if (Kind == ChartSeriesKind.Bubble && index % 2 != 0)
                throw new InvalidOperationException("Bubble color measurements belong to the value point, not the size point.");
        }
    }
}
