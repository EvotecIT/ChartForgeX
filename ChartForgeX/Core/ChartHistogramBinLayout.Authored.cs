using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

public sealed partial class ChartHistogramBinLayout {
    private ChartHistogramBinLayout(double[] lowerBounds, double[] upperBounds) {
        _lowerBounds = lowerBounds;
        _upperBounds = upperBounds;
        Minimum = lowerBounds[0];
        Maximum = upperBounds[upperBounds.Length - 1];
        Count = lowerBounds.Length;
    }

    /// <summary>Creates adjacent bins from strictly increasing finite boundaries, preserving their unequal widths.</summary>
    /// <param name="boundaries">At least two boundaries, in increasing order. Inputs are copied.</param>
    /// <returns>An immutable reusable layout. Each bin is half open except the final inclusive upper bound.</returns>
    public static ChartHistogramBinLayout FromBoundaries(IEnumerable<double> boundaries) {
        if (boundaries == null) throw new ArgumentNullException(nameof(boundaries));
        var values = boundaries.ToArray();
        if (values.Length < 2) throw new ArgumentException("Histogram boundaries must contain at least two values.", nameof(boundaries));
        return FromIntervals(values.Take(values.Length - 1).Select((lower, index) => (lower, values[index + 1])));
    }

    /// <summary>Creates finite, ordered, nonoverlapping bins with optional gaps between intervals.</summary>
    /// <param name="intervals">Positive-width intervals in increasing order. Inputs are copied; values in gaps are rejected.</param>
    /// <returns>An immutable reusable layout. Each interval is half open except the final inclusive upper bound.</returns>
    public static ChartHistogramBinLayout FromIntervals(IEnumerable<(double Lower, double Upper)> intervals) {
        if (intervals == null) throw new ArgumentNullException(nameof(intervals));
        var values = intervals.ToArray();
        if (values.Length == 0) throw new ArgumentException("Histogram intervals must contain at least one bin.", nameof(intervals));
        var lower = new double[values.Length];
        var upper = new double[values.Length];
        for (var index = 0; index < values.Length; index++) {
            ChartGuards.Finite(values[index].Lower, nameof(intervals));
            ChartGuards.Finite(values[index].Upper, nameof(intervals));
            if (values[index].Upper <= values[index].Lower || double.IsInfinity(values[index].Upper - values[index].Lower))
                throw new ArgumentException("Histogram intervals must have positive finite widths.", nameof(intervals));
            if (index > 0 && values[index].Lower < values[index - 1].Upper)
                throw new ArgumentException("Histogram intervals must be ordered and must not overlap.", nameof(intervals));
            lower[index] = values[index].Lower;
            upper[index] = values[index].Upper;
        }
        ValidateRange(lower[0], upper[upper.Length - 1]);
        return new ChartHistogramBinLayout(lower, upper);
    }

    private int GetAuthoredIndex(double value) {
        var low = 0;
        var high = Count - 1;
        while (low <= high) {
            var middle = low + (high - low) / 2;
            if (value < _lowerBounds![middle]) high = middle - 1;
            else if (value < _upperBounds![middle] || middle == Count - 1 && value == Maximum) return middle;
            else low = middle + 1;
        }
        throw new ArgumentOutOfRangeException(nameof(value), value, "Histogram values must fall within an authored interval; gaps and excluded upper bounds are not observations.");
    }

    internal bool HasEquivalentBins(ChartHistogramBinLayout other) {
        if (ReferenceEquals(this, other)) return true;
        if (Count != other.Count || !ChartMath.SameCoordinate(Minimum, other.Minimum) || !ChartMath.SameCoordinate(Maximum, other.Maximum)) return false;
        if (_lowerBounds == null && other._lowerBounds == null) return ChartMath.SameCoordinate(_stepWidth, other._stepWidth);
        for (var index = 0; index < Count; index++) {
            if (!ChartMath.SameCoordinate(GetLowerBound(index), other.GetLowerBound(index)) ||
                !ChartMath.SameCoordinate(GetUpperBound(index), other.GetUpperBound(index))) return false;
        }
        return true;
    }
}
