namespace ChartForgeX.Core;

/// <summary>One immutable fitted sample, separate from raw source observations and the two derived endpoint handles.</summary>
public readonly struct ChartTrendLineSample {
    internal ChartTrendLineSample(double x, double value, double? lower = null, double? upper = null) {
        X = x; Value = value; Lower = lower; Upper = upper;
    }
    /// <summary>Gets the original source-coordinate X value.</summary>
    public double X { get; }
    /// <summary>Gets the fitted response, or the conditional median for a log-fitted model.</summary>
    public double Value { get; }
    /// <summary>Gets the lower pointwise interval, or null when intervals are disabled.</summary>
    public double? Lower { get; }
    /// <summary>Gets the upper pointwise interval, or null when intervals are disabled.</summary>
    public double? Upper { get; }
}
