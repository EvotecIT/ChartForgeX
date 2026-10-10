using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Adds a vertical Gantt reference line without reserving a task row.</summary>
    /// <param name="name">The marker caption.</param>
    /// <param name="when">The date at which to paint the marker.</param>
    /// <param name="rangeEnd">An optional scheduling endpoint included in the automatic time window. The line stays at <paramref name="when"/>.</param>
    /// <param name="color">Optional marker ink.</param>
    /// <returns>The current chart.</returns>
    public Chart AddGanttMarker(string name, DateTime when, DateTime? rangeEnd = null, ChartColor? color = null) {
        AddGanttRange(name, when.ToOADate(), (rangeEnd ?? when).ToOADate(), 0, -1, false, color, verticalMarker: true);
        Options.XAxis.UseTimeScaleByDefault();
        return this;
    }

    /// <summary>Adds a vertical Gantt reference line at a numeric schedule value without reserving a task row.</summary>
    /// <param name="name">The marker caption.</param>
    /// <param name="when">The schedule value at which to paint the marker.</param>
    /// <param name="rangeEnd">An optional scheduling endpoint included in the automatic window. The line stays at <paramref name="when"/>.</param>
    /// <param name="color">Optional marker ink.</param>
    /// <returns>The current chart.</returns>
    public Chart AddGanttMarker(string name, double when, double? rangeEnd = null, ChartColor? color = null) =>
        AddGanttRange(name, when, rangeEnd ?? when, 0, -1, false, color, verticalMarker: true);
}
