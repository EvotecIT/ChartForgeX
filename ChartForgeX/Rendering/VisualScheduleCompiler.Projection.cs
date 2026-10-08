using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class VisualScheduleCompiler {
    private const double GanttLaneOpenEndStrokeWidth = 1.5;

    /// <summary>Reserves painted mark extents at automatic axis sides without changing dates or authored clipping windows.</summary>
    private static ChartRect MarkProjection(ChartAxis axis, ChartRect plot, double minimumExtent, double maximumExtent) {
        var left = axis.Minimum.HasValue ? 0 : Math.Min(plot.Width / 2, minimumExtent);
        var right = axis.Maximum.HasValue ? 0 : Math.Min(plot.Width / 2, maximumExtent);
        return new ChartRect(plot.Left + left, plot.Top, Math.Max(0, plot.Width - left - right), plot.Height);
    }
}
