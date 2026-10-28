using System;
using System.Linq;

namespace ChartForgeX.Core;

internal static partial class ChartGuards {
    private static bool ValidateMixedSchedules(Chart chart, bool preparing) {
        if (!ChartSeriesKindTraits.IsMixedSchedule(chart)) return false;
        if (chart.Options.XAxis.Scale is not (ChartScaleKind.Linear or ChartScaleKind.Time))
            throw new InvalidOperationException("Mixed schedules require one shared linear or time x-axis.");
        if (chart.Series.Any(series => series.ValueAxis == ChartAxisSide.Secondary)
            || chart.Options.SecondaryYAxis.Reversed || chart.Options.SecondaryXAxis.Reversed)
            throw new InvalidOperationException("Mixed schedules do not own secondary axes; use the primary time axis and primary lane order.");
        if (chart.Series.Any(series => series.Kind is ChartSeriesKind.StateTimeline or ChartSeriesKind.GanttLane)) ValidateStateCategories(chart);
        foreach (var series in chart.Series) {
            if (series.Kind == ChartSeriesKind.Gantt && series.HasSourceData) ValidateGantt(new[] { series });
            if (series.Kind != ChartSeriesKind.Timeline) continue;
            if (series.RenderPoints.Count > 1) throw new InvalidOperationException("Each Timeline source in a mixed schedule holds one start/end observation; add a source series for another row.");
            foreach (var source in series.RenderPoints)
                if (!double.IsNaN(source.X) && !double.IsInfinity(source.X) && !double.IsNaN(source.Y) && !double.IsInfinity(source.Y) && source.Y >= source.X) continue;
                else throw new InvalidOperationException("Schedule endpoints must be finite and ordered.");
        }
        if (!preparing && chart.Series.All(series => !series.HasSourceData))
            throw new InvalidOperationException("Mixed schedules require at least one source observation.");
        return true;
    }
}
