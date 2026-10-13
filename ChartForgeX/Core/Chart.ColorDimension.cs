using System;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Configures the existing numeric region/tile-map measurement's name and display policy.</summary>
    /// <param name="configure">Edits this chart's existing map color presentation options.</param>
    /// <returns>The current chart.</returns>
    public Chart ConfigureMapColorDimension(Action<ChartColorDimensionOptions> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        Options.MapColorDimension = ChartOptionConfiguration.Configure(Options.MapColorDimension, configure, options => options.Clone()); return this;
    }

    /// <summary>Configures the existing numeric rectangular, hexagonal or calendar heatmap measurement's presentation.</summary>
    /// <param name="configure">Edits this chart's existing heatmap color presentation options.</param>
    /// <returns>The current chart.</returns>
    public Chart ConfigureHeatmapColorDimension(Action<ChartColorDimensionOptions> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        Options.HeatmapColorDimension = ChartOptionConfiguration.Configure(Options.HeatmapColorDimension, configure, options => options.Clone()); return this;
    }
}
