using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawCalendarHeatmap(RgbaCanvas c, Chart chart, ChartRect basePlot) {
        ChartSeries? series = null;
        foreach (var item in chart.Series) if (item.Kind == ChartSeriesKind.CalendarHeatmap) { series = item; break; }
        if (series == null || series.Points.Count == 0) return;
        var cells = CalendarHeatmapCells(series);
        if (cells.Count == 0) return;

        var t = chart.Options.Theme;
        var minDate = cells[0].Date;
        var maxDate = cells[0].Date;
        var min = cells[0].Value;
        var max = cells[0].Value;
        foreach (var item in cells) {
            if (item.Date < minDate) minDate = item.Date;
            if (item.Date > maxDate) maxDate = item.Date;
            if (item.Value < min) min = item.Value;
            if (item.Value > max) max = item.Value;
        }
        if (Math.Abs(max - min) < 0.000001) max = min + 1;

        var start = CalendarWeekStart(minDate);
        var end = CalendarWeekEnd(maxDate);
        var columns = Math.Max(1, ((end - start).Days / 7) + 1);
        var leftReserve = chart.Options.ShowAxes ? 34 : 6;
        var topReserve = chart.Options.ShowAxes ? 24 : 6;
        var bottomReserve = chart.Options.ShowHeatmapScale ? 38 : 8;
        var plot = new ChartRect(basePlot.Left + leftReserve, basePlot.Top + topReserve, Math.Max(1, basePlot.Width - leftReserve - 8), Math.Max(1, basePlot.Height - topReserve - bottomReserve));
        var gap = columns > 32 ? 2.5 : 3.5;
        var cell = Math.Max(1, Math.Min((plot.Width - gap * (columns - 1)) / columns, (plot.Height - gap * 6) / 7));
        var gridWidth = columns * cell + (columns - 1) * gap;
        var gridHeight = 7 * cell + 6 * gap;
        var x0 = plot.Left + Math.Max(0, (plot.Width - gridWidth) / 2);
        var y0 = plot.Top + Math.Max(0, (plot.Height - gridHeight) / 2);
        var radius = Math.Min(4, cell * 0.22);
        var byDate = new Dictionary<DateTime, CalendarHeatmapCell>();
        foreach (var item in cells) byDate[item.Date] = item;
        var hasEmptyCells = false;

        DrawCalendarHeatmapPngAxes(c, chart, start, maxDate, x0, y0, cell, gap);
        for (var day = start; day <= end; day = day.AddDays(1)) {
            var column = (day - start).Days / 7;
            var row = (int)day.DayOfWeek;
            var hasValue = byDate.TryGetValue(day, out var entry);
            hasEmptyCells |= !hasValue;
            var value = hasValue ? entry.Value : 0;
            var color = hasValue ? ChartHeatmapSurface.CalendarColor(chart, series, entry.Color, value, min, max) : ChartHeatmapSurface.CalendarEmptyColor(chart);
            var x = x0 + column * (cell + gap);
            var y = y0 + row * (cell + gap);
            c.FillRoundedRect(x, y, cell, cell, radius, color);
            c.StrokeRoundedRect(x, y, cell, cell, radius, ApplyOpacity(t.CardBackground, ChartVisualPrimitives.HeatmapCellBorderOpacity), ChartVisualPrimitives.HeatmapCellBorderStrokeWidth);
        }

        if (chart.Options.ShowHeatmapScale) DrawCalendarHeatmapPngScale(c, chart, series, min, max, x0 + gridWidth, y0 + gridHeight + 20, cell, hasEmptyCells);
    }

    private static void DrawCalendarHeatmapPngAxes(RgbaCanvas c, Chart chart, DateTime start, DateTime end, double x0, double y0, double cell, double gap) {
        if (!chart.Options.ShowAxes) return;
        var t = chart.Options.Theme;
        var fontSize = PngTickFontSize(chart);
        DrawPngRightAlignedText(c, x0 - 8, y0 + 1 * (cell + gap) + cell / 2, "Mon", t.MutedText, fontSize);
        DrawPngRightAlignedText(c, x0 - 8, y0 + 3 * (cell + gap) + cell / 2, "Wed", t.MutedText, fontSize);
        DrawPngRightAlignedText(c, x0 - 8, y0 + 5 * (cell + gap) + cell / 2, "Fri", t.MutedText, fontSize);

        var month = new DateTime(start.Year, start.Month, 1);
        while (month < start) month = month.AddMonths(1);
        var lastX = x0 - 40;
        while (month <= end) {
            var column = Math.Max(0, (month - start).Days / 7);
            var x = x0 + column * (cell + gap);
            if (x - lastX >= 28) {
                c.DrawTextEmphasized(x, y0 - fontSize - 4, month.ToString("MMM", System.Globalization.CultureInfo.InvariantCulture), t.MutedText, fontSize);
                lastX = x;
            }

            month = month.AddMonths(1);
        }
    }

    private static void DrawCalendarHeatmapPngScale(RgbaCanvas c, Chart chart, ChartSeries series, double min, double max, double right, double y, double cell, bool showNoData) {
        var t = chart.Options.Theme;
        var size = Math.Max(7, Math.Min(12, cell));
        var gap = Math.Max(2, size * 0.28);
        var width = 5 * size + 4 * gap;
        var fontSize = PngTickFontSize(chart);
        var noDataWidth = showNoData ? size + gap : 0;
        var x = right - noDataWidth - width - EstimatePngTextWidth("More", fontSize) - 10;
        var lessLabelX = x - EstimatePngTextWidth("Less", fontSize) - 8;
        if (showNoData) {
            c.FillRoundedRect(x, y, size, size, Math.Min(3, size * 0.22), ChartHeatmapSurface.CalendarEmptyColor(chart));
            x += size + gap;
        }

        c.DrawText(lessLabelX, y + size / 2 - fontSize / 2, "Less", t.MutedText, fontSize);
        for (var i = 0; i < 5; i++) {
            var value = min + (max - min) * (i / 4.0);
            var color = ChartHeatmapSurface.CalendarColor(chart, series, null, value, min, max);
            c.FillRoundedRect(x + i * (size + gap), y, size, size, Math.Min(3, size * 0.22), color);
        }

        c.DrawText(x + width + 8, y + size / 2 - fontSize / 2, "More", t.MutedText, fontSize);
    }

    private static void DrawPngRightAlignedText(RgbaCanvas c, double right, double middle, string text, ChartColor color, double fontSize) {
        c.DrawText(text.Length == 0 ? right : right - EstimatePngTextWidth(text, fontSize), middle - fontSize / 2, text, color, fontSize);
    }

    private static List<CalendarHeatmapCell> CalendarHeatmapCells(ChartSeries series) {
        var cells = new List<CalendarHeatmapCell>();
        for (var i = 0; i < series.Points.Count; i++) {
            var color = i < series.PointColors.Count ? series.PointColors[i] : null;
            cells.Add(new CalendarHeatmapCell(DateTime.FromOADate(series.Points[i].X).Date, series.Points[i].Y, color));
        }

        return cells;
    }

    private static DateTime CalendarWeekStart(DateTime date) => date.Date.AddDays(-(int)date.DayOfWeek);

    private static DateTime CalendarWeekEnd(DateTime date) => date.Date.AddDays(6 - (int)date.DayOfWeek);

    private static bool IsCalendarHeatmapChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.CalendarHeatmap);

    private readonly struct CalendarHeatmapCell {
        public readonly DateTime Date;
        public readonly double Value;
        public readonly ChartColor? Color;

        public CalendarHeatmapCell(DateTime date, double value, ChartColor? color) {
            Date = date;
            Value = value;
            Color = color;
        }
    }
}
