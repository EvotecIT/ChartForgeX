using System;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Gets the names and positive/negative fill overrides for a difference area.</summary>
    public ChartDifferenceAreaOptions DifferenceArea { get; private set; } = new();

    /// <summary>Configures paired readout names and fills without changing authored values or boundary order.</summary>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The current series.</returns>
    public ChartSeries ConfigureDifferenceArea(Action<ChartDifferenceAreaOptions> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        DifferenceArea = ChartOptionConfiguration.Configure(DifferenceArea, configure, options => options.Clone()); return this;
    }

    internal void ValidateDifferenceArea(ChartOptions options) {
        if (Kind != ChartSeriesKind.DifferenceArea) {
            if (DifferenceArea.IsConfigured) throw new InvalidOperationException("Series '" + Name + "' requires DifferenceArea for difference options.");
            return;
        }
        var axis = ValueAxis == ChartAxisSide.Secondary ? options.SecondaryYAxis : options.YAxis;
        if (axis.Scale == ChartScaleKind.Time)
            throw new NotSupportedException("Difference areas require a numeric Linear, Logarithmic or SymmetricLogarithmic value axis; date/time coordinates belong on X.");
        if (RenderPoints.Count % 2 != 0) throw new InvalidOperationException("Difference areas require complete First/Second pairs.");
        for (var item = 0; item < RenderPoints.Count; item += 2) {
            var first = RenderPoints[item]; var second = RenderPoints[item + 1];
            if (first.X != second.X) throw new InvalidOperationException("Difference First and Second values must share the same X coordinate.");
            if (item > 0 && !(first.BreakBefore || second.BreakBefore) && first.X <= RenderPoints[item - 2].X)
                throw new InvalidOperationException("Difference areas require increasing X coordinates within each connected segment.");
        }
    }
}
