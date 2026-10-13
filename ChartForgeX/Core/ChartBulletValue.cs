using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Core;

/// <summary>Describes one measured value, target and scale with its authored qualitative range boundaries.</summary>
public readonly struct ChartBulletValue {
    private readonly IReadOnlyList<double>? _rangeEnds;
    /// <summary>Gets the authored measured value.</summary>
    public double Value { get; }
    /// <summary>Gets the authored target marker value.</summary>
    public double Target { get; }
    /// <summary>Gets the scale minimum.</summary>
    public double Minimum { get; }
    /// <summary>Gets the scale maximum.</summary>
    public double Maximum { get; }
    /// <summary>Gets a detached read-only snapshot of authored qualitative boundaries in input order.</summary>
    /// <remarks>Rendering orders and clamps these boundaries to the visible scale without changing this snapshot.</remarks>
    public IReadOnlyList<double> RangeEnds => _rangeEnds ?? Array.Empty<double>();

    /// <summary>Creates one bullet observation. Values and targets outside the scale remain available as authored facts.</summary>
    public ChartBulletValue(double value, double target, double minimum = 0, double maximum = 100, IEnumerable<double>? rangeEnds = null) {
        var scale = new ChartGaugeValue(value, minimum, maximum);
        ChartGuards.Finite(target, nameof(target));
        var boundaries = rangeEnds?.ToArray() ?? Array.Empty<double>();
        foreach (var boundary in boundaries) ChartGuards.Finite(boundary, nameof(rangeEnds));
        Value = value; Target = target; Minimum = scale.Minimum; Maximum = scale.Maximum;
        _rangeEnds = Array.AsReadOnly(boundaries);
    }
}
