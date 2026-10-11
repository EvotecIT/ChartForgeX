using System;
using System.Collections.Generic;

namespace ChartForgeX.Primitives;

/// <summary>Logical-pixel dash period and phase shared by scene serialization and native strokes.</summary>
internal static class ChartStrokeDashes {
    internal static double Period(IReadOnlyList<double> pattern) {
        var period = 0d;
        for (var i = 0; i < pattern.Count; i++) period += pattern[i];
        return pattern.Count % 2 == 0 ? period : period * 2;
    }

    internal static double NormalizeOffset(IReadOnlyList<double>? pattern, double offset) {
        if (offset == 0 || pattern == null || pattern.Count == 0) return 0;
        var period = Period(pattern);
        return period > 0 && !double.IsInfinity(period) ? offset % period : 0;
    }
}
