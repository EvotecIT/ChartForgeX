using System;

namespace ChartForgeX.Core;

/// <summary>Describes one measured value and its scale for a gauge or circular progress chart.</summary>
public readonly struct ChartGaugeValue {
    /// <summary>Gets the authored value, before display clamping.</summary>
    public double Value { get; }
    /// <summary>Gets the scale minimum.</summary>
    public double Minimum { get; }
    /// <summary>Gets the scale maximum.</summary>
    public double Maximum { get; }

    /// <summary>Creates a measured value with a finite positive scale. Values outside the scale remain available as authored facts.</summary>
    public ChartGaugeValue(double value, double minimum = 0, double maximum = 100) {
        ChartGuards.Finite(value, nameof(value)); ChartGuards.Finite(minimum, nameof(minimum)); ChartGuards.Finite(maximum, nameof(maximum));
        if (maximum <= minimum || double.IsInfinity(maximum - minimum))
            throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "Gauge maximum must define a finite positive range above minimum.");
        Value = value; Minimum = minimum; Maximum = maximum;
    }
}
