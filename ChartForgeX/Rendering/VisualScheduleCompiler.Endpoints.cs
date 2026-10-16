using System;
using System.Collections.Generic;

namespace ChartForgeX.Rendering;

internal static partial class VisualScheduleCompiler {
    /// <summary>Measures the actual project endpoints when reserving automatic schedule caption gutters.</summary>
    private static IReadOnlyList<double> ScheduleProjectWindowTicks(IReadOnlyList<double> ticks, double min, double max) {
        var result = new List<double>(ticks.Count + 2) { min };
        foreach (var tick in ticks) if (tick > min && tick < max) result.Add(tick);
        if (max > min) result.Add(max);
        return result;
    }

    /// <summary>Keeps project ends on their actual ticks, leaving interval choice to the shared calendar fitter.</summary>
    private static IReadOnlyList<double> ScheduleProjectEndpoints(IReadOnlyList<double> ticks, double min, double max,
        Func<IReadOnlyList<double>, bool> fits) {
        if (max <= min || !fits(new[] { min, max })) return new[] { min };
        var result = new List<double>(ticks.Count + 2) { min };
        foreach (var tick in ticks) {
            if (tick <= min || tick >= max) continue;
            // Reserve endpoint geometry only. The shared calendar fitter validates
            // adjacent caption precision and density for the complete sequence.
            if (fits(new[] { min, tick }) && fits(new[] { tick, max })) result.Add(tick);
        }
        result.Add(max);
        return result;
    }
}
