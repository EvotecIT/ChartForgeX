using System;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    private ChartPolarGridShape _polarGridShape;
    private double? _polarGridRingCount;

    /// <summary>
    /// Gets or sets the concentric grid shape for radar and polar line charts.
    /// Automatic preserves each native chart family's default. Series forms and value scales are unchanged.
    /// </summary>
    public ChartPolarGridShape PolarGridShape {
        get => _polarGridShape;
        set {
            if (!Enum.IsDefined(typeof(ChartPolarGridShape), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _polarGridShape = value;
        }
    }

    /// <summary>
    /// Gets or sets the number of equally spaced radial intervals from zero through one hundred.
    /// Fractional values draw the ceiling number of guides with the authored interval divisor;
    /// the outer guide can extend beyond the value axis, and the scene fits that guide inside its viewport.
    /// Zero hides concentric guides; spokes and observations remain visible. Null uses the radial axis's generated ticks.
    /// This does not change the value domain or observed values.
    /// </summary>
    public double? PolarGridRingCount {
        get => _polarGridRingCount;
        set {
            if (value.HasValue && (double.IsNaN(value.Value) || double.IsInfinity(value.Value) || value.Value < 0 || value.Value > 100)) throw new ArgumentOutOfRangeException(nameof(value));
            _polarGridRingCount = value;
        }
    }
}

public sealed partial class Chart {
    /// <summary>Sets the concentric grid shape shared by radar or polar line series.</summary>
    /// <param name="shape">The grid shape; Automatic keeps the native family default.</param>
    /// <returns>The current chart.</returns>
    public Chart WithPolarGridShape(ChartPolarGridShape shape) {
        Options.PolarGridShape = shape;
        return this;
    }
}
