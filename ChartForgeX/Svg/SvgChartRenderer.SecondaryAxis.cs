using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static bool HasSecondaryYAxis(Chart chart) => chart.Series.Any(series => series.YAxis == ChartAxisSide.Secondary);

    private static ChartMapper SeriesMap(ChartSeries series, ChartMapper primaryMap, ChartMapper? secondaryMap) =>
        series.YAxis == ChartAxisSide.Secondary && secondaryMap != null ? secondaryMap : primaryMap;

    private static void DrawSecondaryYAxis(StringBuilder sb, Chart chart, ChartRect plot, IReadOnlyList<double> yTicks, ChartMapper map) {
        var o = chart.Options;
        if (!o.ShowAxes) return;
        var t = o.Theme;
        var tickStyle = o.TickLabelStyle;
        var tickFontSize = StyleFontSize(tickStyle, t.TickLabelFontSize);
        var tickLabelMaxWidth = Math.Max(24, chart.Options.Size.Width - plot.Right - 18);
        var writer = new SvgMarkupWriter(2048);
        foreach (var yv in yTicks) {
            var y = map.Y(yv);
            var rawLabel = FormatSecondaryValue(chart, yv);
            var labelFontSize = TextFontSizeForSvgWidth(rawLabel, tickLabelMaxWidth, tickFontSize);
            var label = TrimSvgLabelToWidth(rawLabel, labelFontSize, tickLabelMaxWidth);
            if (label.Length == 0) continue;
            WriteSecondaryYAxisTick(writer, chart, tickStyle, yv, plot.Right + 12, y + 4, StyleColor(tickStyle, t.MutedText).ToCss(), labelFontSize, label);
        }

        if (ShowAxisLines(chart)) WriteSecondaryYAxisLine(writer, plot, t.Axis.ToCss());
        if (string.IsNullOrWhiteSpace(chart.SecondaryYAxisTitle)) {
            sb.Append(writer.Build());
            return;
        }

        var style = chart.Options.AxisTitleStyle;
        var titleMaxWidth = Math.Max(40, plot.Height * 0.72);
        var titleFontSize = TextFontSizeForSvgWidth(chart.SecondaryYAxisTitle, titleMaxWidth, StyleFontSize(style, t.AxisTitleFontSize));
        var title = TrimSvgLabelToWidth(chart.SecondaryYAxisTitle, titleFontSize, titleMaxWidth);
        if (title.Length != 0) {
            WriteSecondaryYAxisTitle(
                writer,
                chart,
                style,
                chart.SecondaryYAxisTitle,
                title,
                Math.Min(chart.Options.Size.Width - 18, plot.Right + 54),
                plot.Top + plot.Height / 2.0,
                StyleColor(style, t.MutedText).ToCss(),
                titleFontSize);
        }

        sb.Append(writer.Build());
    }

    private static void WriteSecondaryYAxisTick(SvgMarkupWriter writer, Chart chart, ChartTextStyle tickStyle, double value, double x, double y, string color, double fontSize, string label) {
        writer
            .StartElement("text")
            .Attribute("data-cfx-role", "secondary-y-axis-tick")
            .Attribute("data-cfx-value", value)
            .Attribute("x", x)
            .Attribute("y", y)
            .Attribute("text-anchor", "start")
            .Attribute("fill", color)
            .Attribute("font-family", SvgFontFamilyAttributeValue(StyleFontFamily(chart, tickStyle)))
            .Attribute("font-size", fontSize)
            .Raw(SvgTextStyleAttributes(tickStyle))
            .Text(label)
            .EndElement()
            .Line();
    }

    private static void WriteSecondaryYAxisLine(SvgMarkupWriter writer, ChartRect plot, string color) {
        writer
            .StartElement("line")
            .Attribute("data-cfx-role", "secondary-y-axis")
            .Attribute("x1", plot.Right)
            .Attribute("y1", plot.Top)
            .Attribute("x2", plot.Right)
            .Attribute("y2", plot.Bottom)
            .Attribute("stroke", color)
            .Attribute("stroke-width", ChartVisualPrimitives.AxisStrokeWidth)
            .EndEmptyElement()
            .Line();
    }

    private static void WriteSecondaryYAxisTitle(SvgMarkupWriter writer, Chart chart, ChartTextStyle style, string rawTitle, string title, double x, double y, string color, double fontSize) {
        writer
            .StartElement("text")
            .Attribute("data-cfx-role", "secondary-y-axis-title")
            .Attribute("data-cfx-label", rawTitle)
            .Attribute("transform", "translate(" + F(x) + " " + F(y) + ") rotate(90)")
            .Attribute("text-anchor", "middle")
            .Attribute("fill", color)
            .Attribute("font-family", SvgFontFamilyAttributeValue(StyleFontFamily(chart, style)))
            .Attribute("font-size", fontSize)
            .Attribute("font-weight", StyleWeight(style, "600"))
            .Raw(SvgTextStyleAttributes(style))
            .Text(title)
            .EndElement()
            .Line();
    }

    private static string SvgFontFamilyAttributeValue(string value) {
        return string.IsNullOrWhiteSpace(value) ? "system-ui, sans-serif" : value;
    }

    private static ChartRect ApplySecondaryYAxisLabelReserve(Chart chart, ChartRect plot, IReadOnlyList<double> yTicks) {
        if (!chart.Options.ShowAxes || chart.Options.IsSparkline || IsPieLike(chart) || yTicks.Count == 0) return plot;
        var t = chart.Options.Theme;
        var widest = yTicks.Max(tick => EstimateTextWidth(FormatSecondaryValue(chart, tick), StyleFontSize(chart.Options.TickLabelStyle, t.TickLabelFontSize)));
        var titleReserve = string.IsNullOrWhiteSpace(chart.SecondaryYAxisTitle) ? 0 : t.AxisTitleFontSize + 18;
        var reserve = Math.Min(150, widest + 30 + titleReserve);
        if (reserve <= 0) return plot;
        return new ChartRect(plot.X, plot.Y, Math.Max(1, plot.Width - reserve), plot.Height);
    }
}
