using System;
using System.Collections.Generic;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawPointLabels(StringBuilder sb, Chart chart, ChartSeries series, IReadOnlyList<ChartPoint> mapped, ChartRect plot) {
        var offset = chart.Options.Theme.MarkerRadius + 12;
        var reserved = new List<ChartLabelBounds>();
        var placement = DataLabelPlacement(chart, series);
        for (var i = 0; i < mapped.Count; i++) {
            var point = mapped[i];
            var label = FormatDataLabel(chart, series, i, series.Points[i].Y);
            if (IsPointCalloutSeries(series)) {
                DrawPointCalloutLabel(sb, chart, label, point.X, point.Y, placement, plot);
                continue;
            }

            if (placement == ChartDataLabelPlacement.Left || placement == ChartDataLabelPlacement.Right) {
                var labelX = placement == ChartDataLabelPlacement.Right ? point.X + offset : point.X - offset;
                var anchor = placement == ChartDataLabelPlacement.Right ? "start" : "end";
                if (!ReserveSvgHorizontalLabel(label, labelX, point.Y, anchor, chart, plot, reserved)) continue;
                DrawHorizontalValueLabel(sb, chart, label, labelX, point.Y, anchor, plot, series, i);
                continue;
            }

            var labelY = placement == ChartDataLabelPlacement.Below
                ? point.Y + offset
                : placement == ChartDataLabelPlacement.Center || placement == ChartDataLabelPlacement.Inside
                    ? point.Y
                    : point.Y - offset;
            if (placement == ChartDataLabelPlacement.Auto && labelY < plot.Top + chart.Options.Theme.DataLabelFontSize) labelY = point.Y + offset;
            if (!ReserveSvgLabel(label, point.X, labelY, chart, plot, reserved)) continue;
            DrawDataLabel(sb, chart, label, point.X, labelY, plot, series: series, pointIndex: i);
        }
    }

    private static bool IsPointCalloutSeries(ChartSeries series) => series.SemanticRole == "point-callout";

    private static void DrawPointCalloutLabel(StringBuilder sb, Chart chart, string label, double x, double y, ChartDataLabelPlacement placement, ChartRect plot) {
        var t = chart.Options.Theme;
        var fontSize = Math.Max(t.DataLabelFontSize, 15);
        label = TrimSvgLabelToWidth(label, fontSize, Math.Max(72, plot.Width * 0.42));
        if (label.Length == 0) return;
        var padX = 12.0;
        var padY = 8.0;
        var width = EstimateTextWidth(label, fontSize) + padX * 2;
        var height = fontSize + padY * 2;
        var gap = 14.0;
        var rectX = x - width / 2;
        var rectY = y - gap - height;
        if (placement == ChartDataLabelPlacement.Below) rectY = y + gap;
        else if (placement == ChartDataLabelPlacement.Left) {
            rectX = x - gap - width;
            rectY = y - height / 2;
        } else if (placement == ChartDataLabelPlacement.Right) {
            rectX = x + gap;
            rectY = y - height / 2;
        }

        rectX = Clamp(rectX, plot.Left + 4, plot.Right - width - 4);
        rectY = Clamp(rectY, plot.Top + 4, plot.Bottom - height - 4);
        var fill = ChartColor.FromRgba(20, 20, 22, 238);
        var writer = new SvgMarkupWriter(768);
        writer.StartElement("rect")
            .Attribute("data-cfx-role", "point-callout-label")
            .Attribute("data-cfx-label", label)
            .Attribute("x", rectX)
            .Attribute("y", rectY)
            .Attribute("width", width)
            .Attribute("height", height)
            .Attribute("rx", 9)
            .Attribute("fill", fill.ToCss())
            .EndEmptyElement()
            .Line();
        WritePointCalloutPointer(writer, x, y, rectX, rectY, width, height, placement, fill);
        writer.StartElement("text")
            .Attribute("data-cfx-role", "point-callout-label-text")
            .Attribute("x", rectX + width / 2)
            .Attribute("y", rectY + padY + fontSize * 0.78)
            .Attribute("text-anchor", "middle")
            .Attribute("fill", "#fff")
            .Attribute("font-family", SvgFontFamilyAttributeValue(t.FontFamily))
            .Attribute("font-size", fontSize)
            .Attribute("font-weight", "800")
            .Raw(Escape(label))
            .EndElement()
            .Line();
        sb.Append(writer.Build());
    }

    private static void WritePointCalloutPointer(SvgMarkupWriter writer, double x, double y, double rectX, double rectY, double width, double height, ChartDataLabelPlacement placement, ChartColor fill) {
        var baseX = Clamp(x, rectX + 10, rectX + width - 10);
        string points;
        if (placement == ChartDataLabelPlacement.Below) {
            points = F(baseX - 6) + "," + F(rectY) + " " + F(baseX + 6) + "," + F(rectY) + " " + F(x) + "," + F(y + 5);
        } else if (placement == ChartDataLabelPlacement.Left) {
            var baseY = Clamp(y, rectY + 10, rectY + height - 10);
            points = F(rectX + width) + "," + F(baseY - 6) + " " + F(rectX + width) + "," + F(baseY + 6) + " " + F(x - 5) + "," + F(y);
        } else if (placement == ChartDataLabelPlacement.Right) {
            var baseY = Clamp(y, rectY + 10, rectY + height - 10);
            points = F(rectX) + "," + F(baseY - 6) + " " + F(rectX) + "," + F(baseY + 6) + " " + F(x + 5) + "," + F(y);
        } else {
            points = F(baseX - 6) + "," + F(rectY + height) + " " + F(baseX + 6) + "," + F(rectY + height) + " " + F(x) + "," + F(y - 5);
        }

        writer.StartElement("polygon")
            .Attribute("data-cfx-role", "point-callout-pointer")
            .Attribute("points", points)
            .Attribute("fill", fill.ToCss())
            .EndEmptyElement()
            .Line();
    }
}
