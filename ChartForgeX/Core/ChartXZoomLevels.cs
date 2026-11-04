using System;
using System.Collections;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>An immutable finite bank of increasing X magnifications, beginning with the full domain.</summary>
/// <remarks>Magnification changes the visible X span, not native stroke, marker or text sizes.
/// Each level is prepared by Core; intermediate magnifications are not synthesized by a host.</remarks>
public sealed class ChartXZoomLevels : IReadOnlyList<double> {
    private readonly double[] _values;

    /// <summary>Creates a bank whose first factor is one and whose remaining finite factors strictly increase.</summary>
    public ChartXZoomLevels(params double[] magnifications) {
        if (magnifications == null) throw new ArgumentNullException(nameof(magnifications));
        if (magnifications.Length == 0 || magnifications[0] != 1)
            throw new ArgumentException("X zoom levels must begin with magnification one.", nameof(magnifications));
        _values = (double[])magnifications.Clone();
        for (var i = 0; i < _values.Length; i++) {
            ChartPrimitiveGuards.Finite(_values[i], nameof(magnifications));
            if (_values[i] < 1 || i > 0 && _values[i] <= _values[i - 1])
                throw new ArgumentException("X zoom magnifications must strictly increase.", nameof(magnifications));
        }
    }

    /// <summary>Gets the number of authored levels.</summary>
    public int Count => _values.Length;
    /// <summary>Gets a level's magnification.</summary>
    public double this[int index] => _values[index];
    /// <summary>Enumerates magnifications in increasing order.</summary>
    public IEnumerator<double> GetEnumerator() => ((IEnumerable<double>)_values).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();
}
