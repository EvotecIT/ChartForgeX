using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using PowerForge.Fluent;

namespace ChartForgeX.Core;

/// <summary>Circle and status-ring presentation. Configure it through <see cref="ChartOptions.Circle"/>.</summary>
[Fluent(Copy = nameof(Clone))]
public sealed class ChartCircleOptions {
    internal ChartCircleOptions Clone() => (ChartCircleOptions)MemberwiseClone();
    private double _circleRadiusScale = 1.0;

    private double _circleStrokeScale = 1.0;


    /// <summary>
    /// Gets or sets the relative radius used by circle charts.
    /// </summary>
    public double RadiusScale {
        get => _circleRadiusScale;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value < 0.65 || value > 1.35) throw new ArgumentOutOfRangeException(nameof(value), value, "Circle radius scale must be between 0.65 and 1.35.");
            _circleRadiusScale = value;
        }
    }


    /// <summary>
    /// Gets or sets the relative stroke thickness used by circle charts.
    /// </summary>
    public double StrokeScale {
        get => _circleStrokeScale;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value < 0.55 || value > 1.8) throw new ArgumentOutOfRangeException(nameof(value), value, "Circle stroke scale must be between 0.55 and 1.8.");
            _circleStrokeScale = value;
        }
    }


    /// <summary>
    /// Gets or sets a value indicating whether circle charts render the status marker and status label.
    /// </summary>
    public bool ShowStatusLabel { get; set; } = true;
}
