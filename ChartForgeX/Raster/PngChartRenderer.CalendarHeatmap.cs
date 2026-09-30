using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawCalendarHeatmap(RgbaCanvas c, Chart chart, ChartRect basePlot) {
        var model = ChartCalendarHeatmapModel.Build(chart);
        if (model == null) return;

        var t = chart.Options.Theme;
        var (leftReserve, topReserve, bottomReserve) = PngCalendarReserves(chart, model);
        var layout = model.Layout(basePlot, leftReserve, topReserve, bottomReserve);

        DrawCalendarHeatmapPngAxes(c, chart, model, layout);
        var hasZero = false;
        for (var day = model.Start; day <= model.End; day = day.AddDays(1)) {
            var hasValue = model.TryGetDay(day, out var entry);
            hasZero |= hasValue && model.IsZero(entry.Value) && !entry.Color.HasValue;
            var color = hasValue ? model.Color(entry.Value, entry.Color) : ChartHeatmapSurface.CalendarEmptyColor(chart);
            var x = layout.X(model.Column(day));
            var y = layout.Y(model.Row(day));
            c.FillRoundedRect(x, y, layout.Cell, layout.Cell, layout.Radius, color);
            c.StrokeRoundedRect(x, y, layout.Cell, layout.Cell, layout.Radius, ApplyOpacity(t.CardBackground, ChartVisualPrimitives.HeatmapCellBorderOpacity), ChartVisualPrimitives.HeatmapCellBorderStrokeWidth);
        }

        if (chart.Options.ShowHeatmapScale) DrawCalendarHeatmapPngScale(c, chart, model, layout.X0 + layout.GridWidth, layout.Y0 + layout.GridHeight + 20, layout.Cell, model.EmptyDays > 0, hasZero);
    }

    /// <summary>Returns the space a calendar keeps for weekday labels, month labels, and its scale.</summary>
    private static (double Left, double Top, double Bottom) PngCalendarReserves(Chart chart, ChartCalendarHeatmapModel model) {
        var tickStyle = chart.Options.TickLabelStyle;
        var tickFontSize = PngTickFontSize(chart);
        var tickHeight = EstimatePngStyledTextBoundsHeight(tickFontSize, tickStyle);
        var widestDay = 0.0;
        for (var row = 0; row < 7; row++) widestDay = Math.Max(widestDay, EstimatePngStyledTextWidth(model.DayName(row), tickFontSize, tickStyle, emphasized: false));
        return (chart.Options.ShowAxes ? Math.Max(34, widestDay + 12) : 6,
            chart.Options.ShowAxes ? Math.Max(24, tickHeight + 10) : 6,
            chart.Options.ShowHeatmapScale ? Math.Max(38, tickHeight + 22) : 8);
    }

    private static void DrawCalendarHeatmapPngAxes(RgbaCanvas c, Chart chart, ChartCalendarHeatmapModel model, ChartCalendarLayout layout) {
        if (!chart.Options.ShowAxes) return;
        var style = chart.Options.TickLabelStyle;
        var fontSize = PngTickFontSize(chart);
        foreach (var row in model.LabelledRows(layout)) {
            DrawCalendarHeatmapPngTick(c, chart, layout.X0 - 8, layout.Y(row) + layout.Cell / 2, model.DayName(row), rightAligned: true, emphasized: false);
        }

        foreach (var (month, x) in model.MonthLabels(layout)) {
            DrawPngTextStyled(c, x, layout.Y0 - EstimatePngStyledTextBoundsHeight(fontSize, style) - 4 - PngStyledTextTopExtent(fontSize, style), model.MonthName(month), style, chart.Options.Theme.MutedText, fontSize, emphasized: true);
        }
    }

    private static void DrawCalendarHeatmapPngScale(RgbaCanvas c, Chart chart, ChartCalendarHeatmapModel model, double right, double y, double cell, bool showNoData, bool showZero) {
        var labels = chart.Options.Labels;
        var size = Math.Max(7, Math.Min(12, cell));
        var gap = Math.Max(2, size * 0.28);
        var width = 5 * size + 4 * gap;
        var fontSize = PngTickFontSize(chart);
        var extraWidth = (showNoData ? size + gap : 0) + (showZero ? size + gap : 0);
        var style = chart.Options.TickLabelStyle;
        var x = right - extraWidth - width - EstimatePngStyledTextWidth(labels.More, fontSize, style, emphasized: false) - 10;
        var lessLabelX = x - EstimatePngStyledTextWidth(labels.Less, fontSize, style, emphasized: false) - 8;
        if (showNoData) {
            c.FillRoundedRect(x, y, size, size, Math.Min(3, size * 0.22), ChartHeatmapSurface.CalendarEmptyColor(chart));
            x += size + gap;
        }

        if (showZero) {
            c.FillRoundedRect(x, y, size, size, Math.Min(3, size * 0.22), ChartHeatmapSurface.ZeroColor(chart));
            x += size + gap;
        }

        DrawCalendarHeatmapPngTick(c, chart, lessLabelX, y + size / 2, labels.Less, rightAligned: false, emphasized: false);
        for (var i = 0; i < 5; i++) {
            var value = model.ScaleValue(i);
            var color = ChartHeatmapSurface.CalendarColor(chart, model.Series, null, value, model.RampMin, model.Max);
            c.FillRoundedRect(x + i * (size + gap), y, size, size, Math.Min(3, size * 0.22), color);
        }

        DrawCalendarHeatmapPngTick(c, chart, x + width + 8, y + size / 2, labels.More, rightAligned: false, emphasized: false);
    }

    private static void DrawCalendarHeatmapPngTick(RgbaCanvas c, Chart chart, double x, double middle, string text, bool rightAligned, bool emphasized) {
        var style = chart.Options.TickLabelStyle;
        var fontSize = PngTickFontSize(chart);
        var width = EstimatePngStyledTextWidth(text, fontSize, style, emphasized);
        var drawX = rightAligned && text.Length > 0 ? x - width : x;
        var drawY = middle - EstimatePngStyledTextBoundsHeight(fontSize, style) / 2 - PngStyledTextTopExtent(fontSize, style);
        DrawPngTextStyled(c, drawX, drawY, text, style, chart.Options.Theme.MutedText, fontSize, emphasized);
    }

    private static bool IsCalendarHeatmapChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.CalendarHeatmap);
}
