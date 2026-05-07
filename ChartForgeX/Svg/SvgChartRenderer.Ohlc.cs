using System;
using System.Collections.Generic;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawOhlc(StringBuilder sb, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var series = chart.Series[index];
        var itemCount = Math.Max(1, series.Points.Count / 4);
        var tickWidth = Math.Max(7, Math.Min(18, plot.Width / Math.Max(1, itemCount * 6.0)));
        var reservedLabels = new List<ChartLabelBounds>();
        for (var pointIndex = 0; pointIndex + 3 < series.Points.Count; pointIndex += 4) {
            var open = series.Points[pointIndex];
            var high = series.Points[pointIndex + 1];
            var low = series.Points[pointIndex + 2];
            var close = series.Points[pointIndex + 3];
            var rising = close.Y >= open.Y;
            var item = pointIndex / 4;
            var semanticColor = rising ? chart.Options.Theme.Positive : chart.Options.Theme.Negative;
            var color = item < series.PointColors.Count && series.PointColors[item] is { } pointColor ? pointColor : semanticColor;
            var x = map.X(open.X);
            var yOpen = map.Y(open.Y);
            var yHigh = map.Y(high.Y);
            var yLow = map.Y(low.Y);
            var yClose = map.Y(close.Y);
            var summary = "open " + FormatValue(chart, open.Y) + ", high " + FormatValue(chart, high.Y) + ", low " + FormatValue(chart, low.Y) + ", close " + FormatValue(chart, close.Y);
            var status = rising ? "rising" : "falling";

            WriteOhlcSummary(sb, index, item, open.X, open.Y, high.Y, low.Y, close.Y, status, summary, color, x, yOpen, yHigh, yLow, yClose, tickWidth);
            var label = FormatValue(chart, close.Y);
            if (ShouldDrawDataLabels(chart, series)) {
                var placement = DataLabelPlacement(chart, series);
                if (placement == ChartDataLabelPlacement.Left || placement == ChartDataLabelPlacement.Right || placement == ChartDataLabelPlacement.Auto || placement == ChartDataLabelPlacement.Outside) {
                    var left = placement == ChartDataLabelPlacement.Left;
                    var anchor = left ? "end" : "start";
                    var labelX = left ? x - tickWidth - ChartVisualPrimitives.OhlcLabelOffset : x + tickWidth + ChartVisualPrimitives.OhlcLabelOffset;
                    if (ReserveSvgHorizontalLabel(label, labelX, yClose, anchor, chart, plot, reservedLabels)) DrawHorizontalValueLabel(sb, chart, label, labelX, yClose, anchor, plot, series, item);
                } else {
                    var labelY = placement == ChartDataLabelPlacement.Below
                        ? yClose + 11
                        : placement == ChartDataLabelPlacement.Center || placement == ChartDataLabelPlacement.Inside
                            ? yClose
                            : yClose - 11;
                    if (ReserveSvgLabel(label, x, labelY, chart, plot, reservedLabels)) DrawDataLabel(sb, chart, label, x, labelY, plot, series: series, pointIndex: item);
                }
            }
        }
    }

    private static void WriteOhlcSummary(
        StringBuilder sb,
        int seriesIndex,
        int pointIndex,
        double valueX,
        double open,
        double high,
        double low,
        double close,
        string status,
        string summary,
        ChartColor color,
        double x,
        double yOpen,
        double yHigh,
        double yLow,
        double yClose,
        double tickWidth) {
        var colorCss = color.ToCss();
        var writer = new SvgMarkupWriter(1024);
        writer
            .StartElement("g")
            .Attribute("data-cfx-role", "ohlc")
            .Attribute("data-cfx-series", seriesIndex)
            .Attribute("data-cfx-point", pointIndex)
            .Attribute("data-cfx-x", valueX)
            .Attribute("data-cfx-open", open)
            .Attribute("data-cfx-high", high)
            .Attribute("data-cfx-low", low)
            .Attribute("data-cfx-close", close)
            .Attribute("data-cfx-status", status)
            .Attribute("role", "img")
            .Attribute("aria-label", summary)
            .EndStartElement()
            .Line()
            .StartElement("title")
            .Text(summary)
            .EndElement()
            .Line();
        WriteOhlcLine(writer, "ohlc-stem", x, yHigh, x, yLow, colorCss);
        WriteOhlcLine(writer, "ohlc-open", x - tickWidth, yOpen, x, yOpen, colorCss);
        WriteOhlcLine(writer, "ohlc-close", x, yClose, x + tickWidth, yClose, colorCss);
        writer.EndElement().Line();
        sb.Append(writer.Build());
    }

    private static void WriteOhlcLine(SvgMarkupWriter writer, string role, double x1, double y1, double x2, double y2, string color) {
        writer
            .StartElement("line")
            .Attribute("data-cfx-role", role)
            .Attribute("x1", x1)
            .Attribute("y1", y1)
            .Attribute("x2", x2)
            .Attribute("y2", y2)
            .Attribute("stroke", color)
            .Attribute("stroke-width", ChartVisualPrimitives.OhlcStrokeWidth)
            .Attribute("stroke-linecap", "round")
            .EndEmptyElement()
            .Line();
    }
}
