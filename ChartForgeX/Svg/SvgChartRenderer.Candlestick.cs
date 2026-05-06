using System;
using System.Collections.Generic;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawCandlesticks(StringBuilder sb, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var series = chart.Series[index];
        var itemCount = Math.Max(1, series.Points.Count / 4);
        var candleWidth = Math.Max(8, Math.Min(22, plot.Width / Math.Max(1, itemCount * 5.0)));
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
            var bodyTop = Math.Min(yOpen, yClose);
            var bodyHeight = Math.Max(2, Math.Abs(yClose - yOpen));
            var summary = "open " + FormatValue(chart, open.Y) + ", high " + FormatValue(chart, high.Y) + ", low " + FormatValue(chart, low.Y) + ", close " + FormatValue(chart, close.Y);
            var fillOpacity = rising ? ChartVisualPrimitives.CandlestickRisingFillOpacity : ChartVisualPrimitives.CandlestickFallingFillOpacity;
            var status = rising ? "rising" : "falling";

            WriteCandlestickSummary(sb, index, item, open.X, open.Y, high.Y, low.Y, close.Y, status, summary, color, x, yHigh, yLow, candleWidth, bodyTop, bodyHeight, fillOpacity);
            var label = FormatValue(chart, close.Y);
            if (ShouldDrawDataLabels(chart, series)) {
                var placement = DataLabelPlacement(chart, series);
                if (placement == ChartDataLabelPlacement.Left || placement == ChartDataLabelPlacement.Right) {
                    var anchor = placement == ChartDataLabelPlacement.Left ? "end" : "start";
                    var labelX = placement == ChartDataLabelPlacement.Left ? x - candleWidth / 2 - 8 : x + candleWidth / 2 + 8;
                    if (ReserveSvgHorizontalLabel(label, labelX, bodyTop + bodyHeight / 2, anchor, chart, plot, reservedLabels)) DrawHorizontalValueLabel(sb, chart, label, labelX, bodyTop + bodyHeight / 2, anchor, plot, series, item);
                } else {
                    var aboveY = bodyTop - chart.Options.Theme.DataLabelFontSize - 4;
                    var belowY = Math.Max(yOpen, yClose) + 5;
                    var labelY = placement == ChartDataLabelPlacement.Below
                        ? belowY
                        : placement == ChartDataLabelPlacement.Center || placement == ChartDataLabelPlacement.Inside
                            ? bodyTop + bodyHeight / 2
                            : aboveY < plot.Top + 2 ? belowY : aboveY;
                    if (ReserveSvgLabel(label, x, labelY, chart, plot, reservedLabels)) DrawDataLabel(sb, chart, label, x, labelY, plot, series: series, pointIndex: item);
                }
            }
        }
    }

    private static void WriteCandlestickSummary(
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
        double yHigh,
        double yLow,
        double candleWidth,
        double bodyTop,
        double bodyHeight,
        double fillOpacity) {
        var colorCss = color.ToCss();
        var writer = new SvgMarkupWriter(1024);
        writer
            .StartElement("g")
            .Attribute("data-cfx-role", "candlestick")
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
            .Line()
            .StartElement("line")
            .Attribute("data-cfx-role", "candlestick-wick")
            .Attribute("x1", x)
            .Attribute("y1", yHigh)
            .Attribute("x2", x)
            .Attribute("y2", yLow)
            .Attribute("stroke", colorCss)
            .Attribute("stroke-width", ChartVisualPrimitives.CandlestickStrokeWidth)
            .Attribute("stroke-linecap", "round")
            .Attribute("opacity", ChartVisualPrimitives.CandlestickWickOpacity)
            .EndEmptyElement()
            .Line()
            .StartElement("rect")
            .Attribute("data-cfx-role", "candlestick-body")
            .Attribute("x", x - candleWidth / 2)
            .Attribute("y", bodyTop)
            .Attribute("width", candleWidth)
            .Attribute("height", bodyHeight)
            .Attribute("rx", ChartVisualPrimitives.CandlestickBodyRadius)
            .Attribute("fill", colorCss)
            .Attribute("fill-opacity", fillOpacity)
            .Attribute("stroke", colorCss)
            .Attribute("stroke-width", ChartVisualPrimitives.CandlestickStrokeWidth)
            .EndEmptyElement()
            .Line()
            .EndElement()
            .Line();
        sb.Append(writer.Build());
    }
}
