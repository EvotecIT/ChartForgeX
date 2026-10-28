using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal sealed partial class ChartGanttLaneModel {
    private static void IncludeSourceWindow(ChartSeries series, ref double minimum, ref double maximum) {
        var min = minimum; var max = maximum;
        void Include(double start, double end) { min = Math.Min(min, start); max = Math.Max(max, end); }
        if (series.Kind == ChartSeriesKind.StateTimeline) foreach (var source in series.StateTimelineSegments) Include(source.Start, source.End);
        else if (series.Kind == ChartSeriesKind.Gantt) foreach (var source in series.GanttItems) Include(source.Start, source.End);
        else if (series.Kind == ChartSeriesKind.Timeline && series.RenderPoints.Count > 0) Include(series.RenderPoints[0].X, series.RenderPoints[0].Y);
        minimum = min; maximum = max;
    }

    internal double RowOffset(int seriesIndex) {
        var offset = 0d;
        foreach (var row in Rows) { if (!row.IsGroup && row.SeriesIndex == seriesIndex) return offset; offset += RowUnits(row); }
        throw new InvalidOperationException("The schedule source has no allocated lane.");
    }

    internal ChartRect RowBounds(ChartRect plot, int rowIndex, double[] tops) {
        var height = Math.Max(0, tops[rowIndex + 1] - tops[rowIndex]);
        return new ChartRect(plot.Left, VisualScheduleCompiler.ScheduleRowTop(Chart, plot, tops[rowIndex], height), plot.Width, height);
    }
}
