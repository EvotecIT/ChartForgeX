using System;
using ChartForgeX.Core;

namespace ChartForgeX.VisualBlocks;

public sealed partial class MetricCard {
    /// <summary>Sets a numeric metric using the same display policy as chart labels and axes.</summary>
    public MetricCard WithMetric(string label, double value, ChartValueFormat format, string? unit = null) =>
        WithMetric(label, (format ?? throw new ArgumentNullException(nameof(format))).Format(value), unit: unit);
}

public sealed partial class ChartTableCell {
    /// <summary>Creates a numeric cell using the shared chart display policy.</summary>
    public static ChartTableCell FromValue(double value, ChartValueFormat format) =>
        new((format ?? throw new ArgumentNullException(nameof(format))).Format(value));
}
