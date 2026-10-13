using System;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    private ChartColorScale? _colorScale;

    /// <summary>
    /// Gets or sets the numeric color scale for Scatter or Bubble observations.
    /// Null preserves ordinary paint while retaining authored color measurements.
    /// </summary>
    public ChartColorScale? ColorScale {
        get => _colorScale;
        set {
            if (value != null && !SupportsNumericColor)
                throw new InvalidOperationException("Series '" + Name + "' requires the Scatter or Bubble kind for numeric color scales.");
            _colorScale = value;
        }
    }

    /// <summary>Sets the numeric color scale for Scatter or Bubble observations.</summary>
    /// <param name="scale">The immutable scale, or null to restore ordinary paint without clearing source color measurements.</param>
    /// <returns>The current series.</returns>
    public ChartSeries WithColorScale(ChartColorScale? scale) {
        ColorScale = scale;
        return this;
    }

    private bool SupportsNumericColor => Kind is ChartSeriesKind.Scatter or ChartSeriesKind.Bubble;

    /// <summary>Rejects color facts that have no logical observation in the selected family.</summary>
    internal void ValidateColorValues() {
        var supported = SupportsNumericColor;
        if (ColorDimension.HasOverrides && !supported)
            throw new InvalidOperationException("Series '" + Name + "' requires the Scatter or Bubble kind for color presentation.");
        if (supported) ColorDimension.ValidateLegend(ColorScale);
        for (var index = 0; index < RenderPoints.Count; index++) {
            if (!RenderPoints[index].ColorValue.HasValue) continue;
            if (!supported)
                throw new InvalidOperationException("Series '" + Name + "' requires the Scatter or Bubble kind for numeric color observations.");
            if (Kind == ChartSeriesKind.Bubble && index % 2 != 0)
                throw new InvalidOperationException("Bubble color measurements belong to the value point, not the size point.");
        }
    }
}
