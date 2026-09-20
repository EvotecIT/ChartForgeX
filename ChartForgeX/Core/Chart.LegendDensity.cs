namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Limits legend height and rows, disclosing additional entries in a summary row.</summary>
    /// <param name="maximumHeightFraction">Fraction of chart height available for the legend, greater than zero and at most one.</param>
    /// <param name="maximumRows">Optional maximum row count, including the summary row.</param>
    /// <returns>The current chart.</returns>
    public Chart WithLegendBudget(double maximumHeightFraction = 0.35, int? maximumRows = null) {
        var validated = new ChartOptions { LegendMaximumHeightFraction = maximumHeightFraction, LegendMaximumRows = maximumRows };
        Options.LegendMaximumHeightFraction = validated.LegendMaximumHeightFraction;
        Options.LegendMaximumRows = validated.LegendMaximumRows;
        return this;
    }
}
