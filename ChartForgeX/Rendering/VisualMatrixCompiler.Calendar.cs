using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualMatrixCompiler {
    private static void Calendar(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect viewport) {
        if (chart.Series.Count != 1) throw new InvalidOperationException("A calendar heatmap requires exactly one series.");
        var model = ChartCalendarHeatmapModel.Build(chart);
        if (model == null) { NoData(chart, context, builder, viewport); return; }
        var colors = context.Theme.Resolve(context.ThemeMode); var style = VisualStateSceneTools.TickStyle(chart, context);
        var lineHeight = builder.MeasureText("Mg", style).Height; var gap = context.Theme.Spacing;
        var axes = chart.Options.ShowAxes;
        var left = axes && chart.Options.YAxis.Visible ? Math.Min(viewport.Width * .22, Enumerable.Range(0, 7).Select(row => builder.MeasureText(model.DayName(row), style).Width).Max() + gap) : 0;
        var top = axes && chart.Options.XAxis.Visible ? Math.Min(viewport.Height * .15, lineHeight + gap) : 0;
        var scaleHeight = ScaleVisible(chart, context) ? Math.Min(viewport.Height * .25, lineHeight * 2 + gap + 12) : 0;
        var area = new ChartRect(viewport.Left + left, viewport.Top + top, Math.Max(0, viewport.Width - left), Math.Max(0, viewport.Height - top - scaleHeight));
        var grid = model.Layout(area);
        var sourceIndices = model.Series.Points.Select((point, index) => new { Date = DateTime.FromOADate(point.X).Date, Index = index })
            .GroupBy(item => item.Date).ToDictionary(group => group.Key, group => group.Select(item => item.Index).ToArray());
        using (builder.PushGroup("calendar", "calendar-heatmap", new Dictionary<string, string> {
            ["aria-label"] = model.Summary(), ["role"] = "group", ["data-cfx-min"] = VisualStateSceneTools.Number(model.Min),
            ["data-cfx-max"] = VisualStateSceneTools.Number(model.Max), ["data-cfx-start"] = model.DateText(model.Start),
            ["data-cfx-end"] = model.DateText(model.End), ["data-cfx-value-count"] = model.ValueDays.ToString(),
            ["data-cfx-empty-count"] = model.EmptyDays.ToString(), ["data-cfx-zero-count"] = model.ZeroDays.ToString()
        })) {
            for (var offset = 0; offset < model.TotalDays; offset++) {
                var date = model.Start.AddDays(offset); var present = model.TryGetDay(date, out var day);
                var valueText = present ? ChartNumericFormatter.FormatValue(chart.Options, day.Value) : chart.Options.Labels.NoData;
                var label = chart.Options.Labels.FormatDate(date) + ": " + valueText;
                var id = "calendar-day-" + model.DateText(date);
                var bounds = new ChartRect(grid.X(model.Column(date)), grid.Y(model.Row(date)), grid.Cell, grid.Cell);
                var fill = present ? day.Color ?? (model.IsZero(day.Value) ? ChartHeatmapSurface.ZeroColor(colors)
                    : ChartHeatmapSurface.CalendarColor(colors, model.Series.Color, day.Value, model.RampMin, model.Max)) : ChartHeatmapSurface.CalendarEmptyColor(colors);
                var metadata = new Dictionary<string, string> { ["data-cfx-date"] = model.DateText(date), ["data-cfx-empty"] = present ? "false" : "true" };
                if (present) {
                    metadata["data-cfx-value"] = VisualStateSceneTools.Number(day.Value);
                    metadata["data-cfx-level"] = model.Level(day.Value).ToString();
                    metadata["data-cfx-source-points"] = string.Join(",", sourceIndices[date]);
                }
                using (VisualStateSceneTools.Mark(builder, id, "calendar-cell", bounds, label, metadata))
                using (builder.PushClip(area)) builder.Rect(bounds, fill, radius: Math.Min(grid.Radius, context.Theme.BarRadius), role: "calendar-cell-shape");
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
            if (ScaleVisible(chart, context)) CalendarScale(chart, context, builder, model, new ChartRect(area.Left, viewport.Bottom - scaleHeight, area.Width, scaleHeight));
        }
    }

    private static void CalendarScale(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartCalendarHeatmapModel model, ChartRect bounds) {
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        var style = VisualStateSceneTools.TickStyle(chart, context); var colors = context.Theme.Resolve(context.ThemeMode);
        var special = (model.EmptyDays > 0 ? 1 : 0) + (model.ZeroDays > 0 ? 1 : 0); var count = special + 5;
        var height = Math.Min(12, bounds.Height / 3); var pitch = Math.Min(24, bounds.Width / count); var left = bounds.Left + (bounds.Width - pitch * count) / 2;
        for (var index = 0; index < count; index++) {
            var empty = model.EmptyDays > 0 && index == 0; var zero = !empty && index < special;
            var value = index < special ? 0 : model.ScaleValue(index - special);
            var fill = empty ? ChartHeatmapSurface.CalendarEmptyColor(colors) : zero ? ChartHeatmapSurface.ZeroColor(colors)
                : ChartHeatmapSurface.CalendarColor(colors, model.Series.Color, value, model.RampMin, model.Max);
            var label = empty ? chart.Options.Labels.NoData : ChartNumericFormatter.FormatValue(chart.Options, value);
            var box = new ChartRect(left + index * pitch, bounds.Top + 4, Math.Max(0, pitch - 3), height);
            var metadata = new Dictionary<string, string> { ["data-cfx-value"] = VisualStateSceneTools.Number(value), ["data-cfx-empty"] = empty ? "true" : "false" };
            if (empty && chart.Options.PinStateColorsInForcedColors) metadata["data-cfx-pin-state-colors"] = "true";
            using (VisualStateSceneTools.Mark(builder, "calendar-scale-" + index, "calendar-scale-step", box, label, metadata)) builder.Rect(box, fill, radius: 1);
        }
        var textTop = bounds.Top + height + 6;
        VisualStateSceneTools.Text(builder, chart.Options.Labels.NoData, new ChartRect(bounds.Left, textTop, bounds.Width * .35, Math.Max(0, bounds.Bottom - textTop)),
            style, "calendar-scale-label", "calendar-scale-empty");
        VisualStateSceneTools.Text(builder, chart.Options.Labels.Less + " – " + chart.Options.Labels.More,
            new ChartRect(bounds.Left + bounds.Width * .35, textTop, bounds.Width * .65, Math.Max(0, bounds.Bottom - textTop)), style,
            "calendar-scale-label", "calendar-scale-range", TextAlignment.Right);
    }
}
