using System;
using System.Collections.Generic;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawErrorBars(StringBuilder sb, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var series = chart.Series[index];
        var itemCount = Math.Max(1, series.Points.Count / 3);
        var capWidth = Math.Max(9, Math.Min(24, plot.Width / Math.Max(1, itemCount * 8.0)));
        var radius = Math.Max(ChartVisualPrimitives.ErrorBarMarkerMinRadius, chart.Options.Theme.MarkerRadius + ChartVisualPrimitives.ErrorBarMarkerRadiusExtra);
        var reservedLabels = new List<ChartLabelBounds>();

        for (var pointIndex = 0; pointIndex + 2 < series.Points.Count; pointIndex += 3) {
            var center = series.Points[pointIndex];
            var lower = series.Points[pointIndex + 1];
            var upper = series.Points[pointIndex + 2];
            var x = map.X(center.X);
            var y = map.Y(center.Y);
            var yLower = map.Y(lower.Y);
            var yUpper = map.Y(upper.Y);
            var item = pointIndex / 3;
            var color = PointColor(chart, series, index, item);
            var summary = "value " + FormatValue(chart, center.Y) + ", range " + FormatValue(chart, lower.Y) + "-" + FormatValue(chart, upper.Y);

            WriteErrorBarSummary(sb, chart, index, item, center.X, center.Y, lower.Y, upper.Y, summary, color, x, y, yLower, yUpper, radius, capWidth);
            var label = FormatValue(chart, center.Y);
            if (ShouldDrawDataLabels(chart, series)) DrawErrorBarLabel(sb, chart, series, item, plot, reservedLabels, label, x, y, yLower, yUpper, radius, capWidth);
        }
    }

    private static void WriteErrorBarSummary(
        StringBuilder sb,
        Chart chart,
        int seriesIndex,
        int pointIndex,
        double valueX,
        double value,
        double lower,
        double upper,
        string summary,
        ChartColor color,
        double x,
        double y,
        double yLower,
        double yUpper,
        double radius,
        double capWidth) {
        var colorCss = color.ToCss();
        var writer = new SvgMarkupWriter(1024);
        writer
            .StartElement("g")
            .Attribute("data-cfx-role", "error-bar")
            .Attribute("data-cfx-series", seriesIndex)
            .Attribute("data-cfx-point", pointIndex)
            .Attribute("data-cfx-x", valueX)
            .Attribute("data-cfx-value", value)
            .Attribute("data-cfx-lower", lower)
            .Attribute("data-cfx-upper", upper)
            .Attribute("role", "img")
            .Attribute("aria-label", summary)
            .EndStartElement()
            .Line();
        WriteErrorBarRange(writer, seriesIndex, pointIndex, lower, upper, x, yUpper, x, yLower, colorCss);
        WriteErrorBarCap(writer, seriesIndex, pointIndex, "upper", upper, x - capWidth / 2, yUpper, x + capWidth / 2, yUpper, colorCss);
        WriteErrorBarCap(writer, seriesIndex, pointIndex, "lower", lower, x - capWidth / 2, yLower, x + capWidth / 2, yLower, colorCss);
        writer
            .StartElement("circle")
            .Attribute("data-cfx-role", "error-marker")
            .Attribute("data-cfx-series", seriesIndex)
            .Attribute("data-cfx-point", pointIndex)
            .Attribute("data-cfx-value", value)
            .Attribute("cx", x)
            .Attribute("cy", y)
            .Attribute("r", radius)
            .Attribute("fill", colorCss)
            .Attribute("stroke", chart.Options.Theme.CardBackground.ToCss())
            .Attribute("stroke-width", ChartVisualPrimitives.MarkerStrokeWidth)
            .EndEmptyElement()
            .Line()
            .EndElement()
            .Line();
        sb.Append(writer.Build());
    }

    private static void WriteErrorBarRange(SvgMarkupWriter writer, int seriesIndex, int pointIndex, double lower, double upper, double x1, double y1, double x2, double y2, string color) {
        writer
            .StartElement("line")
            .Attribute("data-cfx-role", "error-range")
            .Attribute("data-cfx-series", seriesIndex)
            .Attribute("data-cfx-point", pointIndex)
            .Attribute("data-cfx-lower", lower)
            .Attribute("data-cfx-upper", upper)
            .Attribute("x1", x1)
            .Attribute("y1", y1)
            .Attribute("x2", x2)
            .Attribute("y2", y2)
            .Attribute("stroke", color)
            .Attribute("stroke-width", ChartVisualPrimitives.ErrorBarStrokeWidth)
            .Attribute("stroke-linecap", "round")
            .Attribute("opacity", ChartVisualPrimitives.ErrorBarRangeOpacity)
            .EndEmptyElement()
            .Line();
    }

    private static void WriteErrorBarCap(SvgMarkupWriter writer, int seriesIndex, int pointIndex, string bound, double value, double x1, double y1, double x2, double y2, string color) {
        writer
            .StartElement("line")
            .Attribute("data-cfx-role", "error-cap")
            .Attribute("data-cfx-series", seriesIndex)
            .Attribute("data-cfx-point", pointIndex)
            .Attribute("data-cfx-bound", bound)
            .Attribute("data-cfx-value", value)
            .Attribute("x1", x1)
            .Attribute("y1", y1)
            .Attribute("x2", x2)
            .Attribute("y2", y2)
            .Attribute("stroke", color)
            .Attribute("stroke-width", ChartVisualPrimitives.ErrorBarStrokeWidth)
            .Attribute("stroke-linecap", "round")
            .EndEmptyElement()
            .Line();
    }

    private static void DrawErrorBarLabel(StringBuilder sb, Chart chart, ChartSeries series, int pointIndex, ChartRect plot, List<ChartLabelBounds> reservedLabels, string label, double x, double y, double yLower, double yUpper, double radius, double capWidth) {
        var placement = DataLabelPlacement(chart, series);
        if (placement == ChartDataLabelPlacement.Left || placement == ChartDataLabelPlacement.Right) {
            var anchor = placement == ChartDataLabelPlacement.Left ? "end" : "start";
            var labelX = placement == ChartDataLabelPlacement.Left ? x - capWidth / 2 - 8 : x + capWidth / 2 + 8;
            if (ReserveSvgHorizontalLabel(label, labelX, y, anchor, chart, plot, reservedLabels)) DrawHorizontalValueLabel(sb, chart, label, labelX, y, anchor, plot, series, pointIndex);
            return;
        }

        var top = Math.Min(yLower, yUpper);
        var bottom = Math.Max(yLower, yUpper);
        var labelY = placement == ChartDataLabelPlacement.Below
            ? bottom + radius + 9
            : placement == ChartDataLabelPlacement.Center || placement == ChartDataLabelPlacement.Inside
                ? y
                : top - radius - 9;
        if (ReserveSvgLabel(label, x, labelY, chart, plot, reservedLabels)) DrawDataLabel(sb, chart, label, x, labelY, plot, series: series, pointIndex: pointIndex);
    }
}
