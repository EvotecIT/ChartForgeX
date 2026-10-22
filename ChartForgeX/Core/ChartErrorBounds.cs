using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>A finite, closed source-coordinate uncertainty interval independent of its physical axis.</summary>
public readonly struct ChartErrorBounds {
    /// <summary>Gets the inclusive lower source bound.</summary>
    public double Lower { get; }
    /// <summary>Gets the inclusive upper source bound.</summary>
    public double Upper { get; }

    /// <summary>Creates increasing or equal uncertainty bounds without changing their units.</summary>
    /// <param name="lower">The finite inclusive lower bound.</param>
    /// <param name="upper">The finite inclusive upper bound; may equal lower.</param>
    public ChartErrorBounds(double lower, double upper) {
        ChartPrimitiveGuards.Finite(lower, nameof(lower)); ChartPrimitiveGuards.Finite(upper, nameof(upper));
        if (upper < lower) throw new ArgumentOutOfRangeException(nameof(upper), upper, "The upper uncertainty bound cannot precede the lower bound.");
        Lower = lower; Upper = upper;
    }

    /// <summary>Creates bounds using the existing UTC date conversion shared by native time axes.</summary>
    /// <param name="lower">The inclusive lower instant.</param>
    /// <param name="upper">The inclusive upper instant.</param>
    public ChartErrorBounds(DateTime lower, DateTime upper) : this(ChartDateTime.ToOADate(lower), ChartDateTime.ToOADate(upper)) { }
}
