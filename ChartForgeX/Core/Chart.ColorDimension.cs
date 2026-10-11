using System;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Configures the existing numeric region/tile-map measurement's name and display policy.</summary>
    /// <param name="configure">Edits this chart's existing map color presentation options.</param>
    /// <returns>The current chart.</returns>
    public Chart ConfigureMapColorDimension(Action<ChartColorDimensionOptions> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        configure(Options.MapColorDimension); return this;
    }

    /// <summary>Configures the existing numeric rectangular, hexagonal or calendar heatmap measurement's presentation.</summary>
    /// <param name="configure">Edits this chart's existing heatmap color presentation options.</param>
    /// <returns>The current chart.</returns>
    public Chart ConfigureHeatmapColorDimension(Action<ChartColorDimensionOptions> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        configure(Options.HeatmapColorDimension); return this;
    }
}
