using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualMatrixCompiler {
    private static void Calendar(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect viewport, ChartColorPresentation presentation) {
        if (chart.Series.Count != 1) throw new InvalidOperationException("A calendar heatmap requires exactly one series.");
        var model = ChartCalendarHeatmapModel.Build(chart);
        if (model == null) { NoData(chart, context, builder, viewport); return; }
        var colors = context.Theme.Resolve(context.ThemeMode); var style = VisualStateSceneTools.TickStyle(chart, context);
        var lineHeight = builder.MeasureText("Mg", style).Height; var gap = context.Theme.Spacing;
        var axes = chart.Options.ShowAxes;
        var left = axes && chart.Options.YAxis.Visible ? Math.Min(viewport.Width * .22, Enumerable.Range(0, 7).Select(row => builder.MeasureText(model.DayName(row), style).Width).Max() + gap) : 0;
        var top = axes && chart.Options.XAxis.Visible ? Math.Min(viewport.Height * .15, lineHeight + gap) : 0;
        var scale = chart.Options.HeatmapColorScale;
        var scaleHeight = ScaleVisible(context, presentation) ? scale != null
            ? ColorScaleHeight(chart, context, builder, Math.Max(0, viewport.Width - left), Math.Max(0, viewport.Height * .35 - gap), presentation, model.EmptyDays > 0) + gap
            : Math.Min(viewport.Height * .25, CalendarScaleCaptionHeight(chart, builder, model, style, presentation) + 12 + gap + gap / 3 + VisualColorScaleLegendScene.HeadingHeight(builder, style, gap / 2, presentation.HasName ? presentation.Name : null)) : 0;
        var scaleGap = Math.Min(gap, scaleHeight / 3);
        var area = new ChartRect(viewport.Left + left, viewport.Top + top, Math.Max(0, viewport.Width - left), Math.Max(0, viewport.Height - top - scaleHeight));
        var grid = model.Layout(area);
        var sourceIndices = model.Series.Points.Select((point, index) => new { Date = DateTime.FromOADate(point.X).Date, Index = index })
            .GroupBy(item => item.Date).ToDictionary(group => group.Key, group => group.Select(item => item.Index).ToArray());
        using (builder.PushGroup("calendar", "calendar-heatmap", new Dictionary<string, string> {
            ["aria-label"] = model.Summary(), ["role"] = "group", ["data-cfx-label-level"] = chart.Options.Labels.Level,
            ["data-cfx-min"] = VisualStateSceneTools.Number(model.Min),
            ["data-cfx-max"] = VisualStateSceneTools.Number(model.Max), ["data-cfx-start"] = model.DateText(model.Start),
            ["data-cfx-end"] = model.DateText(model.End), ["data-cfx-first-day"] = model.FirstDay.ToString(), ["data-cfx-value-count"] = model.ValueDays.ToString(),
            ["data-cfx-empty-count"] = model.EmptyDays.ToString(), ["data-cfx-zero-count"] = model.ZeroDays.ToString()
        })) {
            for (var offset = 0; offset < model.TotalDays; offset++) {
                var date = model.Start.AddDays(offset); var present = model.TryGetDay(date, out var day);
                var valueText = present ? presentation.Value(day.Value) : presentation.NoData;
                var label = model.Series.Name + ", " + presentation.Labels.FormatDate(date) + ": " + valueText;
                var id = "calendar-day-" + model.DateText(date);
                var bounds = new ChartRect(grid.X(model.Column(date)), grid.Y(model.Row(date)), grid.Cell, grid.Cell);
                var blend = present ? day.Color.HasValue ? ChartColorBlend.Solid(day.Color.Value, SvgColorRole.Series)
                    : scale != null ? scale.BlendFor(day.Value, model.Min, model.Max)
                    : model.IsZero(day.Value) ? ChartHeatmapSurface.ZeroBlend(colors)
                    : ChartHeatmapSurface.CalendarBlend(colors, model.Series.Color, day.Value, model.RampMin, model.Max, VisualChartPaint.SeriesRole(model.Series))
                    : CalendarMissingBlend(chart, colors);
                var metadata = new Dictionary<string, string> {
                    ["data-cfx-series"] = "0", ["data-cfx-region"] = model.DateText(date), ["data-cfx-label"] = label,
                    ["data-cfx-date"] = model.DateText(date), ["data-cfx-empty"] = present ? "false" : "true",
                    ["data-cfx-row"] = model.Row(date).ToString(CultureInfo.InvariantCulture),
                    ["data-cfx-week-index"] = model.Column(date).ToString(CultureInfo.InvariantCulture),
                    ["data-cfx-weekday-index"] = ((int)date.DayOfWeek).ToString(CultureInfo.InvariantCulture)
                };
                if (present) {
                    metadata["data-cfx-value"] = VisualStateSceneTools.Number(day.Value);
                    if (presentation.HasOverrides) {
                        metadata["data-cfx-formatted-value"] = valueText;
                        metadata["data-cfx-label-value"] = presentation.Name;
                    }
                    if (scale == null) metadata["data-cfx-level"] = model.Level(day.Value).ToString();
                    var sources = sourceIndices[date];
                    metadata["data-cfx-source-points"] = string.Join(",", sources);
                    // Ingestion normalizes dates to one authored observation. Later numeric edits can still create aggregates.
                    if (sources.Length == 1) metadata["data-cfx-observation-index"] = sources[0].ToString(CultureInfo.InvariantCulture);
                }
                using (VisualStateSceneTools.Mark(builder, id, "calendar-cell", bounds, label, metadata))
                using (builder.PushClip(area)) builder.Rect(bounds, blend.Color, radius: Math.Min(grid.Radius, context.Theme.BarRadius), role: "calendar-cell-shape", paint: VisualChartPaint.Fill(blend.Paint));
            }
            if (axes && chart.Options.YAxis.Visible) {
                for (var row = 0; row < 7; row++) {
                    var weekday = (DayOfWeek)(((int)model.FirstDay + row) % 7);
                    if (grid.Cell + grid.Gap < lineHeight && weekday != DayOfWeek.Monday && weekday != DayOfWeek.Wednesday && weekday != DayOfWeek.Friday) continue;
                    VisualStateSceneTools.Text(builder, model.DayName(row), new ChartRect(viewport.Left, grid.Y(row) - Math.Max(0, (lineHeight - grid.Cell) / 2),
                        Math.Max(0, area.Left - viewport.Left - gap), Math.Max(lineHeight, grid.Cell)), style, "calendar-weekday", "calendar-weekday-" + row, TextAlignment.Right);
                }
            }
            if (axes && chart.Options.XAxis.Visible)
                foreach (var month in model.MonthLabels(grid, text => builder.MeasureText(text, style).Width))
                    VisualStateSceneTools.Text(builder, model.MonthName(month.Month), new ChartRect(month.X, viewport.Top, Math.Max(0, area.Right - month.X), top),
                        style, "calendar-month", "calendar-month-" + month.Month.ToString("yyyy-MM", CultureInfo.InvariantCulture));
            if (ScaleVisible(context, presentation)) {
                var scaleBounds = new ChartRect(area.Left, viewport.Bottom - scaleHeight + scaleGap, area.Width, Math.Max(0, scaleHeight - scaleGap));
                if (scale != null) ColorScale(chart, context, builder, scaleBounds, model.Min, model.Max, "calendar-scale", presentation, model.EmptyDays > 0,
                    CalendarMissingBlend(chart, colors));
                else CalendarScale(chart, context, builder, model, scaleBounds, presentation);
            }
        }
    }
}
