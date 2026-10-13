using System;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    private double _polarAxisScaleFactor = 1;
    private double _polarAxisLabelFactor = 1;

    /// <summary>Gets or sets the spoke radius relative to the radar or polar value-axis radius. Finite nonnegative factors preserve the observations and value domain; layout fits visible spokes inside the plot.</summary>
    public double PolarAxisScaleFactor {
        get => _polarAxisScaleFactor;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            _polarAxisScaleFactor = value;
        }
    }

    /// <summary>Gets or sets the category-label anchor radius relative to the radar or polar value-axis radius. Finite nonnegative factors retain the shared theme's label gap; layout reserves measured caption space.</summary>
    public double PolarAxisLabelFactor {
        get => _polarAxisLabelFactor;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            _polarAxisLabelFactor = value;
        }
    }
}
