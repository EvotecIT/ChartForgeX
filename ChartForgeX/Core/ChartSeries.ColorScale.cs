using System;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    private ChartColorScale? _colorScale;

    /// <summary>
    /// Gets or sets the numeric color scale for Scatter, Bubble, DottedMap, RegionMap or inset TileMap observations.
    /// Null preserves ordinary paint while retaining authored color measurements.
    /// </summary>
    public ChartColorScale? ColorScale {
        get => _colorScale;
        set {
            if (value != null && !SupportsNumericColor)
                throw new InvalidOperationException("Series '" + Name + "' requires Scatter, Bubble, DottedMap, RegionMap or inset TileMap for numeric color scales.");
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

    private bool SupportsNumericColor => Kind is ChartSeriesKind.Scatter or ChartSeriesKind.Bubble or ChartSeriesKind.DottedMap or ChartSeriesKind.RegionMap or ChartSeriesKind.TileMap;

    /// <summary>Rejects color facts that have no logical observation in the selected family.</summary>
    internal void ValidateColorValues(ChartOptions? options = null) {
        var supported = SupportsNumericColor;
        if (Kind == ChartSeriesKind.TileMap && !MapInset.HasValue && (ColorScale != null || ColorDimension.HasOverrides))
            throw new InvalidOperationException("TileMap series color overrides require a map inset; standalone TileMaps use the chart's map color defaults.");
        if (ColorDimension.HasOverrides && !supported)
            throw new InvalidOperationException("Series '" + Name + "' requires Scatter, Bubble, DottedMap, RegionMap or inset TileMap for color presentation.");
        if (supported) ColorDimension.ValidateLegend(ColorScale ?? (Kind is ChartSeriesKind.RegionMap or ChartSeriesKind.TileMap ? options?.MapColorScale : null));
        for (var index = 0; index < RenderPoints.Count; index++) {
            if (!RenderPoints[index].ColorValue.HasValue) continue;
            if (!supported)
                throw new InvalidOperationException("Series '" + Name + "' requires the Scatter or Bubble kind for numeric color observations.");
            if (Kind == ChartSeriesKind.Bubble && index % 2 != 0)
                throw new InvalidOperationException("Bubble color measurements belong to the value point, not the size point.");
        }
    }
}
