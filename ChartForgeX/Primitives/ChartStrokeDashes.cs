using System;
using System.Collections.Generic;
using System.Globalization;

namespace ChartForgeX.Primitives;

/// <summary>Logical-pixel dash period and phase shared by scene serialization and native strokes.</summary>
internal static class ChartStrokeDashes {
    /// <summary>Preserves dash and phase values, retaining the compact spelling when it is exact.</summary>
    internal static string FormatNumber(double value) {
        var compact = value.ToString("0.###", CultureInfo.InvariantCulture);
        return double.TryParse(compact, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) && parsed == value
            ? compact : value.ToString("R", CultureInfo.InvariantCulture);
    }

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
