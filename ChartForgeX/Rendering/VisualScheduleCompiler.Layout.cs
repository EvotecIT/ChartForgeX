using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualScheduleCompiler {
    private static ScheduleLayout LaneLayout(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect viewport,
        IEnumerable<string> names, IEnumerable<string?> summaries, bool hasSummary, bool now, IReadOnlyList<double> ticks, Func<double, string> format) {
        var style = VisualStateSceneTools.TickStyle(chart, context); var gap = context.Theme.Spacing;
        var lineHeight = builder.MeasureText("Mg", style).Height;
        var nameWidth = names.Select(name => builder.MeasureText(name, style).Width).DefaultIfEmpty(0).Max();
        var summaryWidth = summaries.Select(summary => builder.MeasureText(summary ?? string.Empty, style).Width).DefaultIfEmpty(0).Max();
        summaryWidth = Math.Max(summaryWidth, builder.MeasureText(chart.Options.LaneSummaryHeader ?? string.Empty, style).Width);
        var angle = Math.Min(80, Math.Abs(chart.Options.XAxis.LabelAngle)) * Math.PI / 180;
        var widestTick = ticks.Select(tick => builder.MeasureText(format(tick), style).Width).DefaultIfEmpty(0).Max();
        var axisReserve = Math.Min(viewport.Height * .25, Math.Sin(angle) * Math.Min(widestTick, viewport.Width * .25) + Math.Cos(angle) * lineHeight + gap);
        var axisTitleStyle = chart.Options.AxisTitleStyle.Resolve(new TextStyle {
            Font = context.Font, FontSize = context.Theme.Typography.AxisSize, Color = context.Theme.Resolve(context.ThemeMode).Foreground
        });
        var axisTitleHeight = builder.MeasureText(ChartTimeScale.DecorateTitle(chart.Options.XAxis, chart.XAxisTitle), axisTitleStyle).Height;
        var top = now || chart.YAxisTitle.Length > 0 ? Math.Min(viewport.Height * .15, lineHeight + gap) : 0;
        var plot = ChartStateTimelineModel.LanePlotArea(chart, viewport, hasSummary, chart.Options.LaneSummaryHeader,
            nameWidth, summaryWidth, lineHeight, 0, top, axisReserve, axisTitleHeight + gap);
        var plotLeft = Math.Min(viewport.Right, plot.Left); var plotTop = Math.Min(viewport.Bottom, plot.Top);
        plot = new ChartRect(plotLeft, plotTop, Math.Max(0, Math.Min(viewport.Right, plot.Right) - plotLeft), Math.Max(0, Math.Min(viewport.Bottom, plot.Bottom) - plotTop));
        var summaryLeft = hasSummary ? Math.Min(viewport.Right, plot.Right + ChartStateTimelineModel.ColumnGap) : viewport.Right;
        return new ScheduleLayout(plot, hasSummary, summaryLeft, axisReserve);
    }

    private static void Axis(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect viewport, ScheduleLayout layout,
        IReadOnlyList<double> ticks, Func<double, double> project, Func<double, string> format) {
        var colors = context.Theme.Resolve(context.ThemeMode); var plot = layout.Plot;
        var style = VisualStateSceneTools.TickStyle(chart, context); var gap = context.Theme.Spacing;
        var grid = chart.Options.ResolvePreparedGridLineStyle();
        var gridWidth = chart.Options.HasPreparedGridStrokeWidth ? grid.StrokeWidth : context.Theme.GridStrokeWidth;
        var dash = grid.Dash > 0 && grid.Gap > 0 ? new[] { grid.Dash, grid.Gap } : null;
        var previousRight = double.NegativeInfinity;
        for (var index = 0; index < ticks.Count; index++) {
            var x = project(ticks[index]);
            if (chart.Options.ShowGrid && grid.ShowVerticalLines)
                builder.Line(x, plot.Top, x, plot.Bottom, colors.Border.WithOpacity(grid.VerticalOpacity), gridWidth, "schedule-grid", dash: dash,
                    paint: VisualChartPaint.Stroke(SvgPaint.Of(colors.Border, SvgColorRole.Grid).WithOpacity(colors.Border.WithOpacity(grid.VerticalOpacity), grid.VerticalOpacity)));
            if (!chart.Options.ShowAxes || !chart.Options.XAxis.Visible) continue;
            var text = format(ticks[index]);
            var width = Math.Min(builder.MeasureText(text, style).Width, Math.Max(0, plot.Width / Math.Max(1, ticks.Count - 1) - gap / 2));
            var left = Math.Max(plot.Left, Math.Min(plot.Right - width, x - width / 2));
            var density = chart.Options.XAxis.LabelDensity;
            if (density != ChartLabelDensity.All && left < previousRight + gap / 2 && index != ticks.Count - 1) continue;
            var tickTop = Math.Min(viewport.Bottom, plot.Bottom + gap / 2);
            var bounds = new ChartRect(left, tickTop, width, Math.Max(0, Math.Min(layout.AxisReserve - gap / 2, viewport.Bottom - tickTop)));
            using (builder.PushClip(viewport))
            using (builder.PushRotation(Math.Max(-80, Math.Min(80, chart.Options.XAxis.LabelAngle)), x, bounds.Top))
                VisualStateSceneTools.Text(builder, text, bounds, style, "schedule-tick-label", "schedule-tick-" + index, TextAlignment.Center);
            previousRight = left + width;
        }
        if (chart.Options.ShowAxes && chart.Options.XAxis.Visible) {
            if (chart.Options.XAxis.ShowLine) builder.Line(plot.Left, plot.Bottom, plot.Right, plot.Bottom, colors.Border, context.Theme.AxisStrokeWidth, "schedule-axis", paint: VisualChartPaint.Stroke(colors.Border, SvgColorRole.Axis));
            var title = ChartTimeScale.DecorateTitle(chart.Options.XAxis, chart.XAxisTitle);
            var titleStyle = chart.Options.AxisTitleStyle.Resolve(new TextStyle { Font = context.Font, FontSize = context.Theme.Typography.AxisSize, Color = colors.Foreground });
            VisualStateSceneTools.Text(builder, title, new ChartRect(plot.Left, plot.Bottom + layout.AxisReserve, plot.Width, Math.Max(0, viewport.Bottom - plot.Bottom - layout.AxisReserve)),
                titleStyle, "schedule-x-axis-title", "schedule-x-title", TextAlignment.Center);
        }
        if (chart.Options.ShowAxes && chart.Options.YAxis.Visible && chart.YAxisTitle.Length > 0)
            VisualStateSceneTools.Text(builder, chart.YAxisTitle, new ChartRect(viewport.Left, viewport.Top, Math.Max(0, plot.Left - viewport.Left), Math.Max(0, plot.Top - viewport.Top)),
                chart.Options.AxisTitleStyle.Resolve(style), "schedule-y-axis-title", "schedule-y-title");
        if (layout.HasSummary && !string.IsNullOrWhiteSpace(chart.Options.LaneSummaryHeader))
            VisualStateSceneTools.Text(builder, chart.Options.LaneSummaryHeader!, new ChartRect(layout.SummaryLeft, viewport.Top,
                Math.Max(0, viewport.Right - layout.SummaryLeft), Math.Max(0, plot.Top - viewport.Top)), style, "lane-summary-header", "schedule-summary-header", TextAlignment.Right);
    }

    private static void LaneText(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect viewport, ScheduleLayout layout,
        string name, string? summary, double top, double height, int index) {
        var style = VisualStateSceneTools.TickStyle(chart, context); var plot = layout.Plot; var gap = context.Theme.Spacing;
        if (chart.Options.ShowGrid) {
            var grid = chart.Options.ResolvePreparedGridLineStyle();
            if (grid.ShowHorizontalLines) builder.Line(plot.Left, top + height / 2, plot.Right, top + height / 2,
                context.Theme.Resolve(context.ThemeMode).Border.WithOpacity(grid.HorizontalOpacity), chart.Options.HasPreparedGridStrokeWidth ? grid.StrokeWidth : context.Theme.GridStrokeWidth,
                "schedule-row-grid", dash: grid.Dash > 0 && grid.Gap > 0 ? new[] { grid.Dash, grid.Gap } : null,
                paint: VisualChartPaint.Stroke(SvgPaint.Of(context.Theme.Resolve(context.ThemeMode).Border, SvgColorRole.Grid)
                    .WithOpacity(context.Theme.Resolve(context.ThemeMode).Border.WithOpacity(grid.HorizontalOpacity), grid.HorizontalOpacity)));
        }
        if (chart.Options.ShowAxes && chart.Options.YAxis.Visible)
            VisualStateSceneTools.Text(builder, name, new ChartRect(viewport.Left, top, Math.Max(0, plot.Left - viewport.Left - gap), height), style,
                "schedule-row-label", "schedule-row-" + index, TextAlignment.Right);
        if (layout.HasSummary && summary != null)
            VisualStateSceneTools.Text(builder, summary, new ChartRect(layout.SummaryLeft, top, Math.Max(0, viewport.Right - layout.SummaryLeft), height), style,
                "lane-summary", "schedule-summary-" + index, TextAlignment.Right);
    }

    private static void Now(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ScheduleLayout layout, double x, double now) {
        var colors = context.Theme.Resolve(context.ThemeMode); var plot = layout.Plot;
        using (builder.PushGroup("schedule-now", "gantt-now", new Dictionary<string, string> { ["data-cfx-value"] = VisualStateSceneTools.Number(now) }))
            builder.Line(x, plot.Top, x, plot.Bottom, colors.Status.Critical.Fill, context.Theme.AxisStrokeWidth, "gantt-now-line", dash: new[] { 4d, 3d }, paint: VisualChartPaint.Stroke(colors.Status.Critical.Fill, SvgColorRole.Status));
        var style = VisualStateSceneTools.TickStyle(chart, context); var measured = builder.MeasureText(chart.Options.Labels.Now, style);
        var width = Math.Min(plot.Width, measured.Width + 4); var left = Math.Max(plot.Left, Math.Min(plot.Right - width, x - width / 2));
        VisualStateSceneTools.Text(builder, chart.Options.Labels.Now, new ChartRect(left, Math.Max(0, plot.Top - measured.Height - 4), width, measured.Height), style,
            "gantt-now-label", "schedule-now-label", TextAlignment.Center);
    }

    private static void DataLabel(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartSeries series, int index,
        string text, ChartRect bounds, ChartRect viewport, VisualThemeColors colors, ChartColor? fillOverride = null, ChartColorBlend? ink = null) {
        if (!(series.ShowDataLabels ?? chart.Options.ShowDataLabels) && series.Kind != ChartSeriesKind.GanttLane) return;
        var fill = fillOverride ?? VisualStateSceneTools.SeriesColor(series, chart.Series.IndexOf(series), colors);
        var style = VisualStateSceneTools.DataStyle(chart, context, series, index, ink?.Color ?? ChartColorMath.AccessibleTextOnBackground(fill));
        var placement = series.DataLabelPlacement ?? chart.Options.DataLabelPlacement;
        var measured = builder.MeasureText(text, style); var gap = context.Theme.Spacing / 2;
        if (placement is ChartDataLabelPlacement.Auto or ChartDataLabelPlacement.Center or ChartDataLabelPlacement.Inside) {
            var inner = new ChartRect(bounds.Left + 3, bounds.Top, Math.Max(0, bounds.Width - 6), bounds.Height);
            if (measured.Width > inner.Width || measured.Height > inner.Height) return;
            VisualStateSceneTools.Text(builder, text, inner, style, "data-label", VisualStateSceneTools.SourceId(chart.Series.IndexOf(series), index) + "-label", TextAlignment.Center,
                paint: VisualStateSceneTools.DataPaint(chart, series, index, style, ink?.Paint));
            return;
        }
        var width = Math.Min(viewport.Width, measured.Width); var height = Math.Min(viewport.Height, measured.Height);
        var left = bounds.Left + (bounds.Width - width) / 2; var top = bounds.Top - gap - height;
        if (placement == ChartDataLabelPlacement.Below) top = bounds.Bottom + gap;
        if (placement == ChartDataLabelPlacement.Left) { left = bounds.Left - width - gap; top = bounds.Top + (bounds.Height - height) / 2; }
        if (placement is ChartDataLabelPlacement.Right or ChartDataLabelPlacement.Outside) { left = bounds.Right + gap; top = bounds.Top + (bounds.Height - height) / 2; }
        left = Math.Max(viewport.Left, Math.Min(viewport.Right - width, left)); top = Math.Max(viewport.Top, Math.Min(viewport.Bottom - height, top));
        if (placement is ChartDataLabelPlacement.Left or ChartDataLabelPlacement.Right or ChartDataLabelPlacement.Outside)
            VisualStateSceneTools.Connector(builder, chart.Options, new ChartPoint(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2),
                new ChartPoint(left + width / 2, top + height / 2), colors.MutedForeground);
        VisualStateSceneTools.Text(builder, text, new ChartRect(left, top, width, height), style, "data-label",
            VisualStateSceneTools.SourceId(chart.Series.IndexOf(series), index) + "-label", TextAlignment.Center, paint: VisualStateSceneTools.DataPaint(chart, series, index, style, ink?.Paint));
    }

    private sealed class ScheduleLayout {
        internal ScheduleLayout(ChartRect plot, bool hasSummary, double summaryLeft, double axisReserve) { Plot = plot; HasSummary = hasSummary; SummaryLeft = summaryLeft; AxisReserve = axisReserve; }
        internal ChartRect Plot { get; } internal bool HasSummary { get; } internal double SummaryLeft { get; } internal double AxisReserve { get; }
    }
}
