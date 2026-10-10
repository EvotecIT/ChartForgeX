using System;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    private ChartPolarGridShape _polarGridShape;
    private int? _polarGridRingCount;

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
    /// Gets or sets an exact number of equally spaced concentric radar/polar guides from one through one hundred.
    /// Null uses the radial axis's generated ticks. This does not change the value domain or series coordinates.
    /// </summary>
    public int? PolarGridRingCount {
        get => _polarGridRingCount;
        set {
            if (value.HasValue && (value.Value < 1 || value.Value > 100)) throw new ArgumentOutOfRangeException(nameof(value));
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
