using System;
using System.Collections.Generic;

namespace ChartForgeX.Rendering;

internal static partial class VisualScheduleCompiler {
    /// <summary>Preserves exact authored clock endpoints for caption measurement.</summary>
    private static IReadOnlyList<double> ScheduleProjectWindowTicks(IReadOnlyList<double> ticks, double min, double max) {
        var result = new List<double>(ticks.Count + 2) { min };
        foreach (var tick in ticks) if (tick > min && tick < max) result.Add(tick);
        if (max > min) result.Add(max);
        return result;
    }

    /// <summary>Retains exact instant captions for clock policies; date-only axes use calendar boundaries instead.</summary>
    private static IReadOnlyList<double> ScheduleClockEndpoints(IReadOnlyList<double> ticks, double min, double max,
        Func<IReadOnlyList<double>, bool> fits) {
        if (max <= min || !fits(new[] { min, max })) return new[] { min };
        var result = new List<double>(ticks.Count + 2) { min };
        foreach (var tick in ticks) {
            if (tick > min && tick < max && fits(new[] { min, tick }) && fits(new[] { tick, max })) result.Add(tick);
        }
        result.Add(max);
        return result;
    }
}
