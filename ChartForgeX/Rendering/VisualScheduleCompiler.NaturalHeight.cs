using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class VisualScheduleCompiler {
    /// <summary>Fits a native range schedule to the same row density and measured common frame used by both exporters.</summary>
    internal static void FitNaturalHeight(Chart chart) {
        var context = VisualExportRequest.ForChart(chart).Context;
        var builder = new VisualSceneBuilder(context.Layout.Size, context.Font);
        var plot = VisualFrameLayout.Build(builder, context, VisualChartCompiler.LegendEntries(VisualChartFamily.Schedule, chart, context.Theme.Resolve(context.ThemeMode)));
        var rows = chart.Series.Count(series => series.GanttItems.Count > 0 && !IsMarker(series));
        var frame = context.Layout.Size.Height - plot.Height;
        var style = VisualStateSceneTools.TickStyle(chart, context);
        var axis = builder.MeasureText("Ag", style).LineHeight + context.Theme.Spacing;
        if (!string.IsNullOrWhiteSpace(chart.XAxisTitle)) axis += context.Theme.Typography.AxisTitleSize * 1.4 + context.Theme.Spacing;
        var top = chart.Options.GanttToday.HasValue || chart.YAxisTitle.Length > 0 ? axis : 0;
        var height = frame + top + Math.Max(rows, ChartRowDensity.MinimumScheduleRows) * ChartRowDensity.MaximumRow + axis;
        chart.WithSize(chart.Options.Size.Width, Math.Max(1, (int)Math.Ceiling(height)));
    }
}
