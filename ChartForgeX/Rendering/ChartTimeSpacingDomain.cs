using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>One captured, bounded time-coordinate map shared by marks, ticks and detached projections.</summary>
internal sealed class ChartTimeSpacingDomain {
    internal const int MaximumEntries = 100_000;
    private readonly double[] _instants;
    internal ChartTimeSpacingDomain(ChartTimeSpacing spacing, double[] instants, double fullMinimum, double fullMaximum) {
        Spacing = spacing; _instants = (double[])instants.Clone(); Instants = Array.AsReadOnly(_instants);
        FullMinimum = Unit ? Minimum : fullMinimum; FullMaximum = Unit ? Maximum : fullMaximum;
    }
    internal ChartTimeSpacing Spacing { get; }
    internal IReadOnlyList<double> Instants { get; }
    internal bool Unit => Spacing.CalendarUnit != null;
    internal double Minimum => _instants[0];
    internal double Maximum => _instants[_instants.Length - 1];
    internal double FullMinimum { get; }
    internal double FullMaximum { get; }

    /// <summary>Exclusive unit maxima name a captured calendar edge; partial-band geometry is not supported.</summary>
    internal void ValidateMaximum(ChartAxisWindow window, string parameterName) {
        if (Unit && !window.MaximumInclusive && !IsBoundary(window.Maximum))
            throw new ArgumentOutOfRangeException(parameterName, window.Maximum, "An exclusive calendar-unit maximum must equal a captured canonical calendar boundary.");
    }

    internal bool IsBoundary(double value) => Array.BinarySearch(_instants, value) >= 0;

    internal double Forward(double value) {
        var index = Find(value);
        if (Unit) return index < 0 ? (value - Minimum) / (_instants[1] - Minimum)
            : index >= _instants.Length - 1 ? _instants.Length - 1 + (value - Maximum) / (Maximum - _instants[_instants.Length - 2])
            : index + .5;
        if (_instants.Length == 1) return value - Minimum;
        index = Math.Max(0, Math.Min(_instants.Length - 2, index));
        return index + (value - _instants[index]) / (_instants[index + 1] - _instants[index]);
    }

    /// <summary>Discrete units select a canonical lower calendar boundary; ordinal gaps interpolate actual instants.</summary>
    internal double Inverse(double coordinate) {
        if (Unit) {
            if (coordinate <= 0) return Minimum + coordinate * (_instants[1] - Minimum);
            if (coordinate >= _instants.Length - 1) return Maximum + (coordinate - (_instants.Length - 1)) * (Maximum - _instants[_instants.Length - 2]);
            return _instants[(int)Math.Floor(coordinate)];
        }
        if (_instants.Length == 1) return Minimum + coordinate;
        var index = (int)Math.Max(0, Math.Min(_instants.Length - 2, Math.Floor(coordinate)));
        return _instants[index] + (coordinate - index) * (_instants[index + 1] - _instants[index]);
    }

    /// <summary>Source windows select whole calendar bands, independently of a band's centered mark position.</summary>
    internal double Bound(double value, bool maximum, bool maximumInclusive = true) {
        if (!Unit) return Forward(value);
        var index = Find(value);
        if (index < 0 || index >= _instants.Length - 1) return Forward(value);
        return maximum && (maximumInclusive || value != _instants[index]) ? index + 1 : index;
    }
    internal double InverseBound(double coordinate, bool maximum) => Unit
        ? Inverse(maximum ? Math.Ceiling(coordinate) : Math.Floor(coordinate)) : Inverse(coordinate);

    internal IReadOnlyList<double> Ticks(double minimum, double maximum, int desiredCount, bool maximumInclusive = true) {
        var candidates = _instants.Take(Unit ? _instants.Length - 1 : _instants.Length)
            .Where(value => Unit ? Forward(value) >= Bound(minimum, false) && Forward(value) < Bound(maximum, true, maximumInclusive)
                : value >= minimum && (maximumInclusive ? value <= maximum : value < maximum)).ToArray();
        var limit = Math.Min(2_000, Math.Max(1, desiredCount));
        if (candidates.Length <= limit) return candidates;
        var stride = (int)Math.Ceiling((double)candidates.Length / limit);
        return candidates.Where((_, index) => index % stride == 0).ToArray();
    }
    internal IReadOnlyList<double> FitTicks(double minimum, double maximum, int count, Func<IReadOnlyList<double>, bool> fits, bool maximumInclusive = true) {
        var ticks = Ticks(minimum, maximum, count, maximumInclusive);
        for (var stride = 1; stride <= ticks.Count; stride++) {
            var selected = ticks.Where((_, index) => index % stride == 0).ToArray();
            if (fits(selected)) return selected;
        }
        return Array.Empty<double>();
    }
    private int Find(double value) {
        var low = 0; var high = _instants.Length - 1;
        while (low <= high) { var middle = low + (high - low) / 2; if (_instants[middle] <= value) low = middle + 1; else high = middle - 1; }
        return high;
    }
}
