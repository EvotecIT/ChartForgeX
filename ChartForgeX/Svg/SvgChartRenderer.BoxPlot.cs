using System;
using System.Collections.Generic;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawBoxPlots(StringBuilder sb, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var series = chart.Series[index];
        var boxCount = Math.Max(1, series.Points.Count / 5);
        var boxWidth = Math.Max(14, Math.Min(46, plot.Width / Math.Max(1, boxCount * 5.0)));
        var capWidth = boxWidth * 0.74;
        var reservedLabels = new List<ChartLabelBounds>();

        for (var pointIndex = 0; pointIndex + 4 < series.Points.Count; pointIndex += 5) {
            var minimum = series.Points[pointIndex];
            var q1 = series.Points[pointIndex + 1];
            var median = series.Points[pointIndex + 2];
            var q3 = series.Points[pointIndex + 3];
            var maximum = series.Points[pointIndex + 4];
            var x = map.X(minimum.X);
            var yMin = map.Y(minimum.Y);
            var yQ1 = map.Y(q1.Y);
            var yMedian = map.Y(median.Y);
            var yQ3 = map.Y(q3.Y);
            var yMax = map.Y(maximum.Y);
            var top = Math.Min(yQ1, yQ3);
            var height = Math.Max(2, Math.Abs(yQ3 - yQ1));
            var item = pointIndex / 5;
            var color = PointColor(chart, series, index, item);
            var summary = FormatValue(chart, minimum.Y) + "-" + FormatValue(chart, maximum.Y) + ", median " + FormatValue(chart, median.Y);

            WriteBoxPlotSummary(sb, index, item, minimum.X, minimum.Y, q1.Y, median.Y, q3.Y, maximum.Y, summary, color, x, yMin, yMedian, yMax, top, height, boxWidth, capWidth);
            var label = FormatValue(chart, median.Y);
            if (ShouldDrawDataLabels(chart, series)) {
                var placement = DataLabelPlacement(chart, series);
                if (placement == ChartDataLabelPlacement.Left || placement == ChartDataLabelPlacement.Right) {
                    var anchor = placement == ChartDataLabelPlacement.Left ? "end" : "start";
                    var labelX = placement == ChartDataLabelPlacement.Left ? x - boxWidth / 2 - 8 : x + boxWidth / 2 + 8;
                    if (ReserveSvgHorizontalLabel(label, labelX, yMedian, anchor, chart, plot, reservedLabels)) DrawHorizontalValueLabel(sb, chart, label, labelX, yMedian, anchor, plot, series, item);
                } else {
                    var labelY = placement == ChartDataLabelPlacement.Below
                        ? Math.Max(yQ1, yQ3) + 11
                        : placement == ChartDataLabelPlacement.Center || placement == ChartDataLabelPlacement.Inside
                            ? yMedian
                            : Math.Min(yQ1, yQ3) - 11;
                    if (ReserveSvgLabel(label, x, labelY, chart, plot, reservedLabels)) DrawDataLabel(sb, chart, label, x, labelY, plot, series: series, pointIndex: item);
                }
            }
        }
    }

    private static void WriteBoxPlotSummary(
        StringBuilder sb,
        int seriesIndex,
        int pointIndex,
        double valueX,
        double minimum,
        double q1,
        double median,
        double q3,
        double maximum,
        string summary,
        ChartColor color,
        double x,
        double yMin,
        double yMedian,
        double yMax,
        double top,
        double height,
        double boxWidth,
        double capWidth) {
        var colorCss = color.ToCss();
        var writer = new SvgMarkupWriter(1024);
        writer
            .StartElement("g")
            .Attribute("data-cfx-role", "box-plot")
            .Attribute("data-cfx-series", seriesIndex)
            .Attribute("data-cfx-point", pointIndex)
            .Attribute("data-cfx-x", valueX)
            .Attribute("data-cfx-min", minimum)
            .Attribute("data-cfx-q1", q1)
            .Attribute("data-cfx-median", median)
            .Attribute("data-cfx-q3", q3)
            .Attribute("data-cfx-max", maximum)
            .Attribute("role", "img")
            .Attribute("aria-label", summary)
            .EndStartElement()
            .Line()
            .StartElement("title")
            .Text(summary)
            .EndElement()
            .Line();
        WriteBoxPlotLine(writer, "box-whisker", x, yMax, x, yMin, colorCss, ChartVisualPrimitives.BoxPlotStrokeWidth, ChartVisualPrimitives.BoxPlotWhiskerOpacity);
        WriteBoxPlotLine(writer, "box-cap", x - capWidth / 2, yMin, x + capWidth / 2, yMin, colorCss, ChartVisualPrimitives.BoxPlotStrokeWidth);
        WriteBoxPlotLine(writer, "box-cap", x - capWidth / 2, yMax, x + capWidth / 2, yMax, colorCss, ChartVisualPrimitives.BoxPlotStrokeWidth);
        writer
            .StartElement("rect")
            .Attribute("data-cfx-role", "box-body")
            .Attribute("x", x - boxWidth / 2)
            .Attribute("y", top)
            .Attribute("width", boxWidth)
            .Attribute("height", height)
            .Attribute("rx", ChartVisualPrimitives.BoxPlotBodyRadius)
            .Attribute("fill", colorCss)
            .Attribute("opacity", ChartVisualPrimitives.BoxPlotBodyFillOpacity)
            .Attribute("stroke", colorCss)
            .Attribute("stroke-width", ChartVisualPrimitives.BoxPlotStrokeWidth)
            .EndEmptyElement()
            .Line();
        WriteBoxPlotLine(writer, "box-median", x - boxWidth / 2, yMedian, x + boxWidth / 2, yMedian, colorCss, ChartVisualPrimitives.BoxPlotMedianStrokeWidth);
        writer.EndElement().Line();
        sb.Append(writer.Build());
    }

    private static void WriteBoxPlotLine(SvgMarkupWriter writer, string role, double x1, double y1, double x2, double y2, string color, double strokeWidth, double? opacity = null) {
        writer
            .StartElement("line")
            .Attribute("data-cfx-role", role)
            .Attribute("x1", x1)
            .Attribute("y1", y1)
            .Attribute("x2", x2)
            .Attribute("y2", y2)
            .Attribute("stroke", color)
            .Attribute("stroke-width", strokeWidth)
            .Attribute("stroke-linecap", "round")
            .OptionalAttribute("opacity", opacity)
            .EndEmptyElement()
            .Line();
    }
}
