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
        IEnumerable<string> names, IEnumerable<string?> summaries, bool hasSummary, bool now, IReadOnlyList<double> ticks, Func<double, string> format, bool hasFixedTicks = false) {
        var cache = new Dictionary<double, string>(); var originalFormat = format;
        string Format(double value) {
            if (!cache.TryGetValue(value, out var text)) cache.Add(value, text = originalFormat(value));
            return text;
        }
        format = Format;
        var style = VisualStateSceneTools.TickStyle(chart, context); var gap = context.Theme.Spacing;
        style = VisualAxisText.FitOversized(builder, style, new ChartRect(0, 0, viewport.Width * .25, Math.Max(0, viewport.Height * .25 - gap)), ticks.Select(format).ToArray(), chart.Options.XAxis.LabelAngle);
        var lineHeight = builder.MeasureText("Mg", style).Height;
        var nameWidth = names.Select(name => builder.MeasureText(name, style).Width).DefaultIfEmpty(0).Max();
        var summaryWidth = summaries.Select(summary => builder.MeasureText(summary ?? string.Empty, style).Width).DefaultIfEmpty(0).Max();
        summaryWidth = Math.Max(summaryWidth, builder.MeasureText(chart.Options.LaneSummaryHeader ?? string.Empty, style).Width);
        var angle = Math.Max(-80, Math.Min(80, chart.Options.XAxis.LabelAngle));
        var tickHeight = ticks.Select(tick => VisualAxisText.RotatedMetrics(builder.MeasureText(format(tick), style), angle).Height).DefaultIfEmpty(lineHeight).Max();
        var axisTitleStyle = chart.Options.AxisTitleStyle.Resolve(new TextStyle {
            Font = context.Font, FontSize = context.Theme.Typography.AxisTitleSize, Color = context.Theme.Resolve(context.ThemeMode).Foreground
        });
        var axisTitle = ChartTimeScale.DecorateTitle(chart.Options.XAxis, chart.XAxisTitle);
        axisTitleStyle = VisualAxisText.FitOversized(builder, axisTitleStyle, new ChartRect(0, 0, viewport.Width, viewport.Height * .15), new[] { axisTitle });
        var axisTitleHeight = builder.MeasureText(axisTitle, axisTitleStyle).Height;
        var top = now || chart.YAxisTitle.Length > 0 ? Math.Min(viewport.Height * .15, lineHeight + gap) : 0;
        // A rotated ordinary caption needs its complete footprint, including the text beyond a quarter-width estimate.
        // Leave the title and a nonempty plotting lane intact; uncontainable captions retain semantics without clipped paint.
        var titleReserve = axisTitle.Length > 0 ? axisTitleHeight + gap : 0;
        var axisReserve = Math.Min(tickHeight + gap, Math.Max(0, viewport.Height - top - titleReserve - 1));
        var plot = ChartStateTimelineModel.LanePlotArea(chart, viewport, hasSummary, chart.Options.LaneSummaryHeader,
            nameWidth, summaryWidth, lineHeight, 0, top, axisReserve, axisTitleHeight + gap);
        var plotLeft = Math.Min(viewport.Right, plot.Left); var plotTop = Math.Min(viewport.Bottom, plot.Top);
        plot = new ChartRect(plotLeft, plotTop, Math.Max(0, Math.Min(viewport.Right, plot.Right) - plotLeft), Math.Max(0, Math.Min(viewport.Bottom, plot.Bottom) - plotTop));
        var summaryLeft = hasSummary ? Math.Min(viewport.Right, plot.Right + ChartStateTimelineModel.ColumnGap) : viewport.Right;
        plot = ScheduleEndGutters(chart, builder, viewport, plot, ticks, format, style);
        return new ScheduleLayout(plot, hasSummary, summaryLeft, axisReserve, viewport, style, format, hasFixedTicks);
    }

    private static void Axis(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect viewport, ScheduleLayout layout,
        IReadOnlyList<double> ticks, Func<double, double> project, Func<double, string> format) {
        var colors = context.Theme.Resolve(context.ThemeMode); var plot = layout.Plot;
        var style = layout.TickStyle; var gap = context.Theme.Spacing;
        format = layout.FormatTick;
        ticks = ScheduleTicks(chart, context, builder, ticks, project, format, style, layout.HasFixedTicks);
        builder.DataProjection = builder.DataProjection?.WithDisplayTicks(ticks.Select(value => new ChartAxisLabel(value, format(value))).ToArray());
        var grid = chart.Options.ResolvePreparedGridLineStyle();
        var gridWidth = chart.Options.HasPreparedGridStrokeWidth ? grid.StrokeWidth : context.Theme.GridStrokeWidth;
        var dash = grid.Dash > 0 && grid.Gap > 0 ? new[] { grid.Dash, grid.Gap } : null;
        for (var index = 0; index < ticks.Count; index++) {
            var x = project(ticks[index]);
            if (chart.Options.ShowGrid && grid.ShowVerticalLines)
                builder.Line(x, plot.Top, x, plot.Bottom, colors.Border.WithOpacity(grid.VerticalOpacity), gridWidth, "schedule-grid", dash: dash,
                    paint: VisualChartPaint.Stroke(SvgPaint.Of(colors.Border, SvgColorRole.Grid).WithOpacity(colors.Border.WithOpacity(grid.VerticalOpacity), grid.VerticalOpacity)));
        }
        if (chart.Options.ShowAxes && chart.Options.XAxis.Visible) {
            TickLabels(chart, context, builder, viewport, layout, ticks, project, format, style);
            if (chart.Options.XAxis.ShowLine) builder.Line(plot.Left, plot.Bottom, plot.Right, plot.Bottom, colors.Border, context.Theme.AxisStrokeWidth, "schedule-axis", paint: VisualChartPaint.Stroke(colors.Border, SvgColorRole.Axis));
            var title = ChartTimeScale.DecorateTitle(chart.Options.XAxis, chart.XAxisTitle);
            var titleStyle = chart.Options.AxisTitleStyle.Resolve(new TextStyle { Font = context.Font, FontSize = context.Theme.Typography.AxisTitleSize, Color = colors.Foreground });
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
        var strokeStyle = chart.Options.GanttTodayStyle;
        var stroke = VisualReferenceStroke.Paint(strokeStyle, colors.Status.Critical.Fill, SvgColorRole.Status, out var strokeColor);
        using (builder.PushGroup("schedule-now", "gantt-now", new Dictionary<string, string> { ["data-cfx-value"] = VisualStateSceneTools.Number(now) }))
        using (builder.PushClip(plot))
            builder.Line(x, plot.Top, x, plot.Bottom, strokeColor, TodayWidth(chart, context), "gantt-now-line",
                dash: strokeStyle?.ResolveDash(new[] { 4d, 3d }) ?? new[] { 4d, 3d }, paint: stroke);
        var style = VisualStateSceneTools.TickStyle(chart, context);
        var availableHeight = Math.Max(0, plot.Top - layout.Viewport.Top - 4);
        style = VisualAxisText.FitOversized(builder, style, new ChartRect(0, 0, plot.Width, availableHeight), new[] { chart.Options.Labels.Now });
        var measured = builder.MeasureText(chart.Options.Labels.Now, style);
        var width = Math.Min(plot.Width, measured.Width + 4); var left = Math.Max(plot.Left, Math.Min(plot.Right - width, x - width / 2));
        VisualStateSceneTools.Text(builder, chart.Options.Labels.Now, new ChartRect(left, Math.Max(layout.Viewport.Top, plot.Top - measured.Height - 4), width, Math.Min(availableHeight, measured.Height)), style,
            "gantt-now-label", "schedule-now-label", TextAlignment.Center);
    }

    private static void DataLabel(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartSeries series, int index,
        string text, ChartRect bounds, ChartRect viewport, VisualThemeColors colors, ChartColor? fillOverride = null, ChartColorBlend? ink = null) {
        if (!(series.ShowDataLabels ?? chart.Options.ShowDataLabels) && series.Kind != ChartSeriesKind.GanttLane) return;
        var fill = fillOverride ?? VisualStateSceneTools.SeriesColor(series, chart.IndexOfSeries(series), colors);
        var style = VisualStateSceneTools.DataStyle(chart, context, series, index, ink?.Color ?? ChartColorMath.AccessibleTextOnBackground(fill));
        var placement = series.DataLabelPlacement ?? chart.Options.DataLabelPlacement;
        var measured = builder.MeasureText(text, style); var gap = context.Theme.Spacing / 2;
        if (placement is ChartDataLabelPlacement.Auto or ChartDataLabelPlacement.Center or ChartDataLabelPlacement.Inside) {
            var inner = new ChartRect(bounds.Left + 3, bounds.Top, Math.Max(0, bounds.Width - 6), bounds.Height);
            style = VisualTextFitting.FitOversized(builder, style, inner.Height, fitted => {
                var metrics = builder.MeasureText(text, fitted); return metrics.Width <= inner.Width && metrics.Height <= inner.Height;
            });
            measured = builder.MeasureText(text, style);
            if (measured.Width > inner.Width || measured.Height > inner.Height) return;
            VisualStateSceneTools.Text(builder, text, inner, style, "data-label", VisualStateSceneTools.SourceId(chart.IndexOfSeries(series), index) + "-label", TextAlignment.Center,
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
            VisualStateSceneTools.SourceId(chart.IndexOfSeries(series), index) + "-label", TextAlignment.Center, paint: VisualStateSceneTools.DataPaint(chart, series, index, style, ink?.Paint));
    }

    private sealed class ScheduleLayout {
        internal ScheduleLayout(ChartRect plot, bool hasSummary, double summaryLeft, double axisReserve, ChartRect viewport, TextStyle tickStyle, Func<double, string> formatTick, bool hasFixedTicks) {
            Plot = plot; HasSummary = hasSummary; SummaryLeft = summaryLeft; AxisReserve = axisReserve; Viewport = viewport;
            TickStyle = tickStyle; FormatTick = formatTick; HasFixedTicks = hasFixedTicks;
        }
        internal ChartRect Plot { get; } internal bool HasSummary { get; } internal double SummaryLeft { get; } internal double AxisReserve { get; }
        internal ChartRect Viewport { get; }
        internal TextStyle TickStyle { get; }
        internal Func<double, string> FormatTick { get; }
        internal bool HasFixedTicks { get; }
    }
}
