using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Core;

/// <summary>Describes one measured value, its scale and optional gauge references.</summary>
public readonly struct ChartGaugeValue {
    private readonly IReadOnlyList<ChartGaugeTarget>? _targets;
    /// <summary>Gets the authored value, before display clamping.</summary>
    public double Value { get; }
    /// <summary>Gets the scale minimum.</summary>
    public double Minimum { get; }
    /// <summary>Gets the scale maximum.</summary>
    public double Maximum { get; }
    /// <summary>Gets a detached read-only snapshot of gauge references in authored order.</summary>
    /// <remarks>Reference values remain unchanged when their drawn marks clamp to the scale. Circular progress series require an empty collection.</remarks>
    public IReadOnlyList<ChartGaugeTarget> Targets => _targets ?? Array.Empty<ChartGaugeTarget>();

    /// <summary>Creates a measured value with a finite positive scale and optional gauge references.</summary>
    /// <param name="value">The authored measured value, before display clamping.</param>
    /// <param name="minimum">The scale minimum.</param>
    /// <param name="maximum">The scale maximum, above the minimum by a finite positive range.</param>
    /// <param name="targets">Optional numeric references, copied in authored order. Circular progress series require no references.</param>
    /// <remarks>Values and references outside the scale remain available as authored facts.</remarks>
    public ChartGaugeValue(double value, double minimum = 0, double maximum = 100, IEnumerable<ChartGaugeTarget>? targets = null) {
        ChartGuards.Finite(value, nameof(value)); ChartGuards.Finite(minimum, nameof(minimum)); ChartGuards.Finite(maximum, nameof(maximum));
        if (maximum <= minimum || double.IsInfinity(maximum - minimum))
            throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "Gauge maximum must define a finite positive range above minimum.");
        var references = targets?.ToArray() ?? Array.Empty<ChartGaugeTarget>();
        Value = value; Minimum = minimum; Maximum = maximum;
        _targets = Array.AsReadOnly(references);
    }
}
