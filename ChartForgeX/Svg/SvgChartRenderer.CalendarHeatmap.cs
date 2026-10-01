using System;
using System.Globalization;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawCalendarHeatmap(StringBuilder sb, Chart chart, ChartRect basePlot) {
        var model = ChartCalendarHeatmapModel.Build(chart);
        if (model == null) return;

        var t = chart.Options.Theme;
        var labels = chart.Options.Labels;
        var (leftReserve, topReserve, bottomReserve) = CalendarReserves(chart, model);
        var layout = model.Layout(basePlot, leftReserve, topReserve, bottomReserve);
        var startText = model.DateText(model.Start);
        var endText = model.DateText(model.End);

        var writer = new SvgMarkupWriter(4096);
        writer
            .StartElement("g")
            .Attribute("data-cfx-role", "calendar-heatmap")
            .Attribute("data-cfx-label-level", labels.LevelOverride)
            .Attribute("data-cfx-label", model.Series.Name)
            .Attribute("data-cfx-start-date", startText)
            .Attribute("data-cfx-end-date", endText)
            .Attribute("data-cfx-day-count", model.TotalDays)
            .Attribute("data-cfx-filled-day-count", model.FilledDays)
            .Attribute("data-cfx-empty-day-count", model.EmptyDays)
            .Attribute("data-cfx-first-day", model.FirstDay.ToString())
            .Attribute("data-cfx-min-value", model.Min)
            .Attribute("data-cfx-max-value", model.Max)
            .Attribute("data-cfx-cell-size", layout.Cell)
            .Attribute("role", "group")
            .Attribute("aria-label", model.Summary())
            .EndStartElement()
            .Line();
        DrawCalendarHeatmapSvgAxes(writer, chart, model, layout);
        var hasZero = false;
        for (var day = model.Start; day <= model.End; day = day.AddDays(1)) {
            var column = model.Column(day);
            var row = model.Row(day);
            var hasValue = model.TryGetDay(day, out var entry);
            var value = hasValue ? entry.Value : 0;
            hasZero |= hasValue && model.IsZero(value) && !entry.Color.HasValue;
            int? level = hasValue ? model.Level(value) : null;
            var fill = hasValue ? model.Blend(value, entry.Color) : ChartHeatmapSurface.CalendarEmptyBlend(chart);
            var dateText = model.DateText(day);
            var summary = model.Series.Name + ", " + labels.FormatDate(day) + ": " + (hasValue ? FormatValue(chart, value) : labels.NoData);
            // A static cell carries an accessible name but is not a tab stop; the interactive HTML adapter adds focus.
            writer
                .StartElement("rect")
                .Attribute("class", "cfx-interactive-region")
                .Attribute("data-cfx-role", "calendar-heatmap-cell")
                .Attribute("data-cfx-date", dateText)
                .Attribute("data-cfx-week-index", column)
                .Attribute("data-cfx-weekday-index", (int)day.DayOfWeek)
                .Attribute("data-cfx-value", value)
                .OptionalAttribute("data-cfx-level", level)
                .Attribute("data-cfx-empty", !hasValue)
                .Attribute("data-cfx-status", hasValue ? null : "empty")
                .Attribute("role", "img")
                .Attribute("aria-label", summary)
                .Attribute("data-cfx-row", row)
                .Attribute("x", layout.X(column))
                .Attribute("y", layout.Y(row))
                .Attribute("width", layout.Cell)
                .Attribute("height", layout.Cell)
                .Attribute("rx", layout.Radius)
                .Paint("fill", fill.Paint)
                .Paint("stroke", SvgPaint.Of(t.CardBackground, SvgColorRole.Surface))
                .Attribute("stroke-opacity", ChartVisualPrimitives.HeatmapCellBorderOpacity)
                .Attribute("stroke-width", ChartVisualPrimitives.HeatmapCellBorderStrokeWidth)
                .EndStartElement()
                .StartElement("title")
                .Text(summary)
                .EndElement()
                .EndElement()
                .Line();
        }

        if (chart.Options.ShowHeatmapScale) DrawCalendarHeatmapSvgScale(writer, chart, model, layout.X0 + layout.GridWidth, layout.Y0 + layout.GridHeight + 20, layout.Cell, model.EmptyDays > 0, hasZero);
        writer.EndElement().Line();
        sb.Append(writer.Build());
    }

    /// <summary>Returns the space a calendar keeps for weekday labels, month labels, and its scale.</summary>
    private static (double Left, double Top, double Bottom) CalendarReserves(Chart chart, ChartCalendarHeatmapModel model) {
        var tickStyle = chart.Options.TickLabelStyle;
        var tickFontSize = StyleFontSize(tickStyle, chart.Options.Theme.TickLabelFontSize);
        var tickHeight = EstimateSvgStyledTextHeight(tickFontSize, tickStyle);
        var widestDay = 0.0;
        for (var row = 0; row < 7; row++) widestDay = Math.Max(widestDay, EstimateSvgStyledTextWidth(chart, model.DayName(row), tickFontSize, tickStyle));
        return (chart.Options.ShowAxes ? Math.Max(34, widestDay + 12) : 6,
            chart.Options.ShowAxes ? Math.Max(24, tickHeight + 10) : 6,
            chart.Options.ShowHeatmapScale ? Math.Max(38, tickHeight + 22) : 8);
    }

    /// <summary>Returns the frame of a calendar chart (see <see cref="ChartCalendarHeatmapModel.Frame"/>), or the plot of other charts.</summary>
    private static ChartRect CalendarFrame(Chart chart, ChartRect plot) =>
        IsCalendarHeatmapChart(chart) ? ChartCalendarHeatmapModel.Frame(chart, plot) : plot;

    private static void DrawCalendarHeatmapSvgAxes(SvgMarkupWriter writer, Chart chart, ChartCalendarHeatmapModel model, ChartCalendarLayout layout) {
        if (!chart.Options.ShowAxes) return;
        foreach (var row in model.LabelledRows(layout)) {
            WriteCalendarHeatmapSvgTick(writer, chart, "calendar-heatmap-weekday-label", model.DayName(row), layout.X0 - 8, layout.Y(row) + layout.Cell / 2, "end", emphasized: false, middleBaseline: true);
        }

        foreach (var (month, x) in model.MonthLabels(layout)) {
            WriteCalendarHeatmapSvgTick(writer, chart, "calendar-heatmap-month-label", model.MonthName(month), x, layout.Y0 - 8, "start", emphasized: true, middleBaseline: false);
        }
    }

    private static void DrawCalendarHeatmapSvgScale(SvgMarkupWriter writer, Chart chart, ChartCalendarHeatmapModel model, double right, double y, double cell, bool showNoData, bool showZero) {
        var t = chart.Options.Theme;
        var labels = chart.Options.Labels;
        var size = Math.Max(7, Math.Min(12, cell));
        var gap = Math.Max(2, size * 0.28);
        var width = 5 * size + 4 * gap;
        var extraWidth = (showNoData ? size + gap : 0) + (showZero ? size + gap : 0);
        var tickStyle = chart.Options.TickLabelStyle;
        var tickFontSize = StyleFontSize(tickStyle, t.TickLabelFontSize);
        var x = right - extraWidth - width - EstimateSvgStyledTextWidth(chart, labels.More, tickFontSize, tickStyle) - 10;
        var lessLabelX = x - 8;
        if (showNoData) {
            WriteCalendarScaleSwatch(writer, "calendar-heatmap-scale-no-data", "empty", x, y, size, ChartHeatmapSurface.CalendarEmptyBlend(chart).Paint, labels.NoData);
            x += size + gap;
        }

        if (showZero) {
            WriteCalendarScaleSwatch(writer, "calendar-heatmap-scale-zero", null, x, y, size, ChartHeatmapSurface.ZeroBlend(chart).Paint, FormatValue(chart, 0));
            x += size + gap;
        }

        WriteCalendarHeatmapSvgTick(writer, chart, "calendar-heatmap-scale-label", labels.Less, lessLabelX, y + size / 2, "end", emphasized: false, middleBaseline: true);
        for (var i = 0; i < 5; i++) {
            var value = model.ScaleValue(i);
            var color = ChartHeatmapSurface.CalendarBlend(chart, model.Series, null, value, model.RampMin, model.Max).Paint;
            writer
                .StartElement("rect")
                .Attribute("data-cfx-role", "calendar-heatmap-scale-step")
                .Attribute("data-cfx-level", model.ScaleLevel(i))
                .Attribute("data-cfx-value", value)
                .Attribute("x", x + i * (size + gap))
                .Attribute("y", y)
                .Attribute("width", size)
                .Attribute("height", size)
                .Attribute("rx", Math.Min(3, size * 0.22))
                .Paint("fill", color)
                .EndEmptyElement()
                .Line();
        }

        WriteCalendarHeatmapSvgTick(writer, chart, "calendar-heatmap-scale-label", labels.More, x + width + 8, y + size / 2, "start", emphasized: false, middleBaseline: true);
    }

    private static void WriteCalendarScaleSwatch(SvgMarkupWriter writer, string role, string? status, double x, double y, double size, SvgPaint color, string title) {
        writer
            .StartElement("rect")
            .Attribute("data-cfx-role", role)
            .Attribute("data-cfx-status", status)
            .Attribute("x", x)
            .Attribute("y", y)
            .Attribute("width", size)
            .Attribute("height", size)
            .Attribute("rx", Math.Min(3, size * 0.22))
            .Paint("fill", color)
            .EndStartElement()
            .StartElement("title")
            .Text(title)
            .EndElement()
            .EndElement()
            .Line();
    }

    private static void WriteCalendarHeatmapSvgTick(SvgMarkupWriter writer, Chart chart, string role, string text, double x, double y, string anchor, bool emphasized, bool middleBaseline) {
        var style = chart.Options.TickLabelStyle;
        var fontSize = StyleFontSize(style, chart.Options.Theme.TickLabelFontSize);
        writer.StartElement("text").Attribute("data-cfx-role", role).Attribute("x", x).Attribute("y", y).Attribute("text-anchor", anchor);
        if (middleBaseline) writer.Attribute("dominant-baseline", "middle");
        writer.Attribute("fill", StyleColor(style, chart.Options.Theme.MutedText).ToCss()).Attribute("font-family", SvgFontFamilyAttributeValue(StyleFontFamily(chart, style))).Attribute("font-size", fontSize).Attribute("font-weight", StyleWeight(style, emphasized ? "650" : "400"));
        WriteSvgTextStyleAttributes(writer, style);
        WriteSvgStyledTextContent(writer, style, StyleText(style, text)).EndElement().Line();
    }

    private static bool IsCalendarHeatmapChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.CalendarHeatmap);
}
