using System;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    private double _legendMaximumHeightFraction = 0.35;
    private int? _legendMaximumRows;

    /// <summary>Gets or sets the fraction of chart height available to a legend, including its spacing.</summary>
    /// <remarks>Defaults to 35 percent. At least one row is retained when the budget can fit a readable row; smaller budgets omit the legend while leaving plotted data unchanged.</remarks>
    public double LegendMaximumHeightFraction {
        get => _legendMaximumHeightFraction;
        set {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value), value, "Legend height fraction must be greater than zero and at most one.");
            _legendMaximumHeightFraction = value;
        }
    }

    /// <summary>Gets or sets an additional row limit, including the overflow summary row. Null uses the height budget alone.</summary>
    public int? LegendMaximumRows {
        get => _legendMaximumRows;
        set {
            if (value.HasValue && value.Value < 1) throw new ArgumentOutOfRangeException(nameof(value), value, "Legend row limit must be greater than zero.");
            _legendMaximumRows = value;
        }
    }
}
