using System;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    private double? _maximumBarWidth;

    /// <summary>Maximum regular bar thickness in drawing pixels, or null to use the theme's 64-pixel default. Histograms retain their bin widths.</summary>
    public double? MaximumBarWidth {
        get => _maximumBarWidth;
        set {
            if (value.HasValue && (double.IsNaN(value.Value) || double.IsInfinity(value.Value) || value.Value <= 0))
                throw new ArgumentOutOfRangeException(nameof(value), "Use a positive finite bar width.");
            _maximumBarWidth = value;
        }
    }
}
