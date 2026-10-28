using System;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Gets the independent color measurement's name and number display policy for Scatter, Bubble and map datasets (TileMap overrides require an inset).</summary>
    public ChartColorDimensionOptions ColorDimension { get; private set; } = new();

    /// <summary>Configures this numeric color dataset's existing presentation options.</summary>
    /// <param name="configure">Edits this series' existing options.</param>
    /// <returns>The current series.</returns>
    public ChartSeries ConfigureColorDimension(Action<ChartColorDimensionOptions> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        if (!SupportsNumericColor) throw new InvalidOperationException("Series '" + Name + "' requires Scatter, Bubble, DottedMap, RegionMap or inset TileMap for color presentation.");
        ColorDimension = ChartOptionConfiguration.Configure(ColorDimension, configure, options => options.Clone()); return this;
    }
}
