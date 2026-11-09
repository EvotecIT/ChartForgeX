using System;
using System.Collections.Generic;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>
/// Design rule 1, shared by every value axis: an automatic linear value axis ends on the next clean tick
/// (1/2/2.5/5 × 10ⁿ, or whole numbers for count data) beyond its data, so no mark passes the outer gridline
/// and every family reads its scale the same way. Authored bounds, authored labels and time scales are kept. A fixed tick step,
/// including a logarithmic one, extends each end to its next stepped tick; explicit tick values extend each end to the nearest
/// authored value beyond the data, when there is one. Logarithmic axes without a fixed step keep their data ends.
/// </summary>
internal static class ChartValueAxisRange {
    /// <summary>Whether the axis takes its automatic ends from its tick step.</summary>
    internal static bool Applies(ChartAxis axis) => (axis.Scale == ChartScaleKind.Linear || axis.Scale == ChartScaleKind.Logarithmic && axis.HasFixedTicks)
        && axis.Labels.Count == 0
        && axis.PreparedTimeDomain == null && axis.TimeSessions == null;

    /// <summary>
    /// Extends each automatic end to the next tick of the selected step and returns the ticks including those ends.
    /// Ticks already inside the domain stay; only missing outer ticks are added.
    /// </summary>
    internal static IReadOnlyList<double> Snap(ChartAxis axis, ref double minimum, ref double maximum, IReadOnlyList<double> ticks) {
        if (Applies(axis) && (axis.TickValues.Count > 0 || axis.Scale == ChartScaleKind.Logarithmic)) return SnapAuthored(axis, ref minimum, ref maximum, ticks);
        if (!Applies(axis) || ticks.Count < 2 || !ChartMath.IsFinite(minimum) || !ChartMath.IsFinite(maximum) || maximum <= minimum) return ticks;
        var step = ticks[1] - ticks[0];
        if (!(step > 0) || !ChartMath.IsFinite(step)) return ticks;
        for (var index = 2; index < ticks.Count; index++)
            if (Math.Abs(ticks[index] - ticks[index - 1] - step) > step * 1e-6) return ticks; // irregular (fitted calendar or authored) scale
        var tolerance = step * 1e-6;
        var result = new List<double>(ticks);
        if (!axis.Maximum.HasValue) {
            for (var guard = 0; maximum > result[result.Count - 1] + tolerance && guard < 4; guard++) result.Add(Clean(result[result.Count - 1] + step, step));
            if (maximum < result[result.Count - 1] + tolerance) maximum = result[result.Count - 1];
        }
        if (!axis.Minimum.HasValue) {
            for (var guard = 0; minimum < result[0] - tolerance && guard < 4; guard++) result.Insert(0, Clean(result[0] - step, step));
            if (minimum > result[0] - tolerance) minimum = result[0];
        }
        return result;
    }

    /// <summary>Moves each automatic end out to the authored policy's nearest tick beyond it, when one exists.</summary>
    private static IReadOnlyList<double> SnapAuthored(ChartAxis axis, ref double minimum, ref double maximum, IReadOnlyList<double> ticks) {
        if (!ChartMath.IsFinite(minimum) || !ChartMath.IsFinite(maximum) || maximum <= minimum) return ticks;
        var outer = ChartTicks.Generate(axis, minimum, maximum);
        if (outer.Count == 0) return ticks;
        if (!axis.Minimum.HasValue && outer[0] < minimum) minimum = outer[0];
        if (!axis.Maximum.HasValue && outer[outer.Count - 1] > maximum) maximum = outer[outer.Count - 1];
        return ChartTicks.GenerateInside(axis, minimum, maximum);
    }

    /// <summary>Whether every observation on a value axis is a whole number, so its ticks should be whole numbers too.</summary>
    internal static bool IsCountData(IEnumerable<double> values) {
        var any = false;
        foreach (var value in values) {
            if (!ChartMath.IsFinite(value)) continue;
            if (Math.Abs(value - Math.Round(value)) > 1e-9) return false;
            any = true;
        }
        return any;
    }

    private static double Clean(double value, double step) {
        var rounded = Math.Round(value / step) * step;
        return Math.Abs(rounded) < step * 1e-9 ? 0 : rounded;
    }
}
