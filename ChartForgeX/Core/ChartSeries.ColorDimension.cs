using System;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Gets the independent color measurement's name and number display policy for Scatter or Bubble observations.</summary>
    public ChartColorDimensionOptions ColorDimension { get; } = new();

    /// <summary>Configures this Scatter or Bubble series' existing color presentation options.</summary>
    /// <param name="configure">Edits this series' existing options.</param>
    /// <returns>The current series.</returns>
    public ChartSeries ConfigureColorDimension(Action<ChartColorDimensionOptions> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        if (!SupportsNumericColor) throw new InvalidOperationException("Series '" + Name + "' requires the Scatter or Bubble kind for color presentation.");
        configure(ColorDimension); return this;
    }
}
