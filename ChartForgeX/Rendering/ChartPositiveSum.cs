using System;
using System.Collections.Generic;

namespace ChartForgeX.Rendering;

/// <summary>Retains positive part-to-whole fractions when a finite list's sum exceeds a double.</summary>
internal readonly struct ChartPositiveSum {
    private readonly double _reference;
    private readonly double _scaledTotal;
    private readonly double _total;

    internal ChartPositiveSum(IEnumerable<double> values) {
        if (values == null) throw new ArgumentNullException(nameof(values));
        _reference = _scaledTotal = _total = 0;
        PositiveCount = 0;
        LastPositiveIndex = -1;
        var index = 0;
        foreach (var value in values) {
            if (!ChartMath.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(values), "Weights must be finite and non-negative.");
            _total += value;
            if (value > 0) {
                if (value > _reference) {
                    _scaledTotal = _scaledTotal * (_reference / value) + 1;
                    _reference = value;
                } else {
                    _scaledTotal += value / _reference;
                }
                PositiveCount++;
                LastPositiveIndex = index;
            }
            index++;
        }
    }

    internal bool HasPositiveValues => PositiveCount > 0;
    internal int PositiveCount { get; }
    internal int LastPositiveIndex { get; }
    /// <summary>Gets the ordinary authored sum when representable; overflow is distinct from a zero total.</summary>
    internal double? FiniteTotal => ChartMath.IsFinite(_total) ? _total : null;
    /// <summary>Uses ordinary division when possible, retaining existing finite-data output.</summary>
    internal double Fraction(double value) => !HasPositiveValues || value <= 0 ? 0
        : ChartMath.IsFinite(_total) ? value / _total : (value / _reference) / _scaledTotal;
}
