using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawRadialBar(StringBuilder sb, Chart chart, ChartRect plot) {
        var series = chart.Series.FirstOrDefault(item => item.Kind == ChartSeriesKind.RadialBar);
        if (series == null || series.Points.Count == 0) return;

        var t = chart.Options.Theme;
        var count = series.Points.Count;
        var chartPlot = RadialBarPlot(chart, plot, series);
        var cx = chartPlot.Left + chartPlot.Width * 0.50;
        var cy = chartPlot.Top + chartPlot.Height * 0.50;
        var baseOuterRadius = Math.Min(chartPlot.Width * 0.36, chartPlot.Height * 0.38);
        var maxOuterRadius = Math.Min(chartPlot.Width * 0.44, chartPlot.Height * 0.44);
        var outerRadius = Math.Max(28, Math.Min(maxOuterRadius, baseOuterRadius * chart.Options.RadialBarRadiusScale));
        var gap = Math.Max(5, outerRadius * 0.035);
        var stroke = Math.Max(5, Math.Min(24, ((outerRadius - 18) / Math.Max(1, count) - gap) * chart.Options.RadialBarStrokeScale));
        if (stroke * count + gap * Math.Max(0, count - 1) > outerRadius - 12) {
            stroke = Math.Max(6, (outerRadius - 12 - gap * Math.Max(0, count - 1)) / Math.Max(1, count));
        }

        var start = -Math.PI / 2;
        var average = series.Points.Average(point => point.Y);
        var centerLabel = FormatValue(chart, average);
        var labelWidth = Math.Max(54, Math.Min(chartPlot.Width * 0.32, outerRadius * 1.25));
        var centerDiskRadius = Math.Max(26, outerRadius - count * (stroke + gap) - 2);
        var showLabels = series.ShowDataLabels != false;

        var writer = new SvgMarkupWriter(4096);
        writer
            .StartElement("g")
            .Attribute("data-cfx-role", "radial-bar-chart")
            .Attribute("data-cfx-radius-scale", chart.Options.RadialBarRadiusScale)
            .Attribute("data-cfx-stroke-scale", chart.Options.RadialBarStrokeScale)
            .EndStartElement()
            .Line();
        for (var i = 0; i < count; i++) {
            var point = series.Points[i];
            var ratio = Clamp(point.Y / 100.0, 0, 1);
            var radius = outerRadius - i * (stroke + gap) - stroke / 2;
            if (radius <= stroke / 2) continue;
            var color = RadialBarColor(series, t, i);
            var label = SliceLabel(chart, point, i);
            var valueLabel = FormatValue(chart, point.Y);
            var summary = label + ": " + valueLabel;
            var end = start + Math.PI * 2 * ratio;

            writer
                .StartElement("path")
                .Attribute("data-cfx-role", "radial-bar-track")
                .Attribute("data-cfx-point", i)
                .Attribute("data-cfx-label", label)
                .Attribute("d", BuildRadialBarArc(cx, cy, radius, start, start + Math.PI * 2))
                .Attribute("fill", "none")
                .Attribute("stroke", t.Grid.ToCss())
                .Attribute("stroke-width", stroke)
                .Attribute("stroke-linecap", "round")
                .Attribute("opacity", ChartVisualPrimitives.RadialTrackOpacity)
                .EndEmptyElement()
                .Line();
            if (ratio > 0) {
                writer
                    .StartElement("path")
                    .Attribute("data-cfx-role", "radial-bar-ring")
                    .Attribute("data-cfx-point", i)
                    .Attribute("data-cfx-label", label)
                    .Attribute("data-cfx-value", point.Y)
                    .Attribute("data-cfx-percent", ratio)
                    .Attribute("role", "img")
                    .Attribute("aria-label", summary)
                    .Attribute("d", BuildRadialBarArc(cx, cy, radius, start, end))
                    .Attribute("fill", "none")
                    .Attribute("stroke", color.ToCss())
                    .Attribute("stroke-width", stroke)
                    .Attribute("stroke-linecap", "round")
                    .EndEmptyElement()
                    .Line();
            }
        }

        writer
            .StartElement("circle")
            .Attribute("data-cfx-role", "radial-bar-center")
            .Attribute("cx", cx)
            .Attribute("cy", cy)
            .Attribute("r", centerDiskRadius)
            .Attribute("fill", t.CardBackground.ToCss())
            .Attribute("fill-opacity", ChartVisualPrimitives.RadialCenterFillOpacity)
            .Attribute("stroke", t.Grid.ToCss())
            .Attribute("stroke-opacity", ChartVisualPrimitives.RadialCenterStrokeOpacity)
            .EndEmptyElement()
            .Line();
        if (showLabels && chart.Options.ShowRadialBarCenterLabel) {
            DrawSvgTextCenteredX(writer, chart, "radial-bar-total", centerLabel, cx, cy - t.TitleFontSize * 0.42, t.Text, Math.Max(26, t.TitleFontSize * 1.32), labelWidth, "850", t.CardBackground, 3.2);
            DrawSvgTextCenteredX(writer, chart, "radial-bar-title", series.Name, cx, cy + t.LegendFontSize + 14, t.MutedText, Math.Max(9, t.LegendFontSize - 1), labelWidth, "700", t.CardBackground, 2.4, middleBaseline: false);
        }
        if (chart.Options.ShowLegend) DrawRadialBarLegend(writer, chart, plot, series);
        writer.EndElement().Line();
        sb.Append(writer.Build());
    }

    private static void DrawRadialBarLegend(SvgMarkupWriter writer, Chart chart, ChartRect plot, ChartSeries series) {
        var t = chart.Options.Theme;
        var fontSize = t.LegendFontSize;
        var area = RadialBarLegendArea(chart, plot, series);
        var rows = BuildRadialBarLegendRows(chart, series, area.Width);
        var y = RadialBarLegendStartY(chart, area, rows.Count);
        writer
            .StartElement("g")
            .Attribute("data-cfx-role", "radial-bar-legend")
            .Attribute("data-cfx-position", chart.Options.LegendPosition.ToString())
            .EndStartElement()
            .Line();
        foreach (var row in rows) {
            if (y > area.Bottom - 4) break;
            var x = RadialBarLegendRowX(chart, area, row.Width);
            foreach (var item in row.Items) {
                var itemX = x + item.X;
                var valueWidth = EstimateTextWidth(item.Value, fontSize);
                var labelMaxWidth = Math.Max(8, item.Width - valueWidth - ChartVisualPrimitives.RadialLegendMarkerRadius * 2 - 26);
                var labelFontSize = TextFontSizeForSvgWidth(item.Label, labelMaxWidth, fontSize);
                var label = TrimSvgLabelToWidth(item.Label, labelFontSize, labelMaxWidth);
                writer
                    .StartElement("circle")
                    .Attribute("data-cfx-role", "radial-bar-legend-marker")
                    .Attribute("data-cfx-point", item.PointIndex)
                    .Attribute("cx", itemX)
                    .Attribute("cy", y - 4)
                    .Attribute("r", ChartVisualPrimitives.RadialLegendMarkerRadius)
                    .Attribute("fill", item.Color.ToCss())
                    .EndEmptyElement()
                    .Line();
                if (label.Length > 0) {
                    writer
                        .StartElement("text")
                        .Attribute("data-cfx-role", "radial-bar-legend-label")
                        .Attribute("data-cfx-point", item.PointIndex)
                        .Attribute("x", itemX + 13)
                        .Attribute("y", y)
                        .Attribute("fill", t.Text.ToCss())
                        .Attribute("font-family", SvgFontFamily(t.FontFamily))
                        .Attribute("font-size", labelFontSize)
                        .Attribute("font-weight", "700")
                        .Text(label)
                        .EndElement()
                        .Line();
                }

                writer
                    .StartElement("text")
                    .Attribute("data-cfx-role", "radial-bar-legend-value")
                    .Attribute("data-cfx-point", item.PointIndex)
                    .Attribute("x", itemX + item.Width - 10)
                    .Attribute("y", y)
                    .Attribute("text-anchor", "end")
                    .Attribute("fill", t.MutedText.ToCss())
                    .Attribute("font-family", SvgFontFamily(t.FontFamily))
                    .Attribute("font-size", fontSize)
                    .Attribute("font-weight", "700")
                    .Text(item.Value)
                    .EndElement()
                    .Line();
            }

            y += RadialBarLegendRowHeight(chart);
        }

        writer.EndElement().Line();
    }

    private static ChartRect RadialBarPlot(Chart chart, ChartRect plot, ChartSeries series) {
        if (!chart.Options.ShowLegend) return plot;
        var reserve = RadialBarLegendReserve(chart, series, plot);
        if (IsLeftLegend(chart.Options.LegendPosition)) return new ChartRect(plot.X + reserve, plot.Y, Math.Max(1, plot.Width - reserve), plot.Height);
        if (IsRightLegend(chart.Options.LegendPosition)) return new ChartRect(plot.X, plot.Y, Math.Max(1, plot.Width - reserve), plot.Height);
        if (IsTopLegend(chart.Options.LegendPosition)) return new ChartRect(plot.X, plot.Y + reserve, plot.Width, Math.Max(1, plot.Height - reserve));
        if (IsBottomLegend(chart.Options.LegendPosition)) return new ChartRect(plot.X, plot.Y, plot.Width, Math.Max(1, plot.Height - reserve));
        return plot;
    }

    private static double RadialBarLegendReserve(Chart chart, ChartSeries series, ChartRect plot) {
        if (IsLeftLegend(chart.Options.LegendPosition) || IsRightLegend(chart.Options.LegendPosition)) return Math.Min(230, Math.Max(142, RadialBarLegendWidestItem(chart, series) + 22)) + ChartVisualPrimitives.SideLegendPlotGap;
        return 18 + BuildRadialBarLegendRows(chart, series, Math.Max(80, plot.Width - 80)).Count * RadialBarLegendRowHeight(chart) + ChartVisualPrimitives.LegendPlotGap;
    }

    private static double RadialBarLegendWidestItem(Chart chart, ChartSeries series) {
        var widest = 0.0;
        for (var i = 0; i < series.Points.Count; i++) {
            var label = SliceLabel(chart, series.Points[i], i);
            var value = FormatValue(chart, series.Points[i].Y);
            widest = Math.Max(widest, ChartVisualPrimitives.RadialLegendMarkerRadius * 2 + EstimateTextWidth(label, chart.Options.Theme.LegendFontSize) + EstimateTextWidth(value, chart.Options.Theme.LegendFontSize) + 34);
        }

        return widest;
    }

    private static ChartRect RadialBarLegendArea(Chart chart, ChartRect plot, ChartSeries series) {
        var reserve = RadialBarLegendReserve(chart, series, plot);
        if (IsLeftLegend(chart.Options.LegendPosition)) return new ChartRect(plot.Left + 18, plot.Top + 20, Math.Max(1, reserve - 28), Math.Max(1, plot.Height - 40));
        if (IsRightLegend(chart.Options.LegendPosition)) return new ChartRect(plot.Right - reserve + 10, plot.Top + 20, Math.Max(1, reserve - 28), Math.Max(1, plot.Height - 40));
        var y = IsTopLegend(chart.Options.LegendPosition) ? plot.Top + 14 : plot.Bottom - reserve - 4;
        return new ChartRect(plot.Left + 36, y, Math.Max(1, plot.Width - 72), reserve);
    }

    private static List<RadialBarLegendRow> BuildRadialBarLegendRows(Chart chart, ChartSeries series, double width) {
        var rows = new List<RadialBarLegendRow>();
        var row = new RadialBarLegendRow();
        rows.Add(row);
        var x = 0.0;
        var vertical = IsLeftLegend(chart.Options.LegendPosition) || IsRightLegend(chart.Options.LegendPosition);
        var maxX = Math.Max(48, width);
        for (var i = 0; i < series.Points.Count; i++) {
            var value = FormatValue(chart, series.Points[i].Y);
            var valueWidth = EstimateTextWidth(value, chart.Options.Theme.LegendFontSize);
            var labelMax = Math.Max(24, maxX - valueWidth - ChartVisualPrimitives.RadialLegendMarkerRadius * 2 - 28);
            var rawLabel = SliceLabel(chart, series.Points[i], i);
            var labelFontSize = TextFontSizeForSvgWidth(rawLabel, labelMax, chart.Options.Theme.LegendFontSize);
            var label = TrimSvgLabelToWidth(rawLabel, labelFontSize, labelMax);
            var itemWidth = Math.Min(maxX, ChartVisualPrimitives.RadialLegendMarkerRadius * 2 + EstimateTextWidth(label, labelFontSize) + valueWidth + 34);
            if (row.Items.Count > 0 && (vertical || x + itemWidth > maxX)) {
                row = new RadialBarLegendRow();
                rows.Add(row);
                x = 0;
            }

            row.Items.Add(new RadialBarLegendItem(i, x, itemWidth, label, value, RadialBarColor(series, chart.Options.Theme, i)));
            row.Width = Math.Max(row.Width, x + itemWidth);
            x += itemWidth;
        }

        return rows;
    }

    private static double RadialBarLegendStartY(Chart chart, ChartRect area, int rows) =>
        IsBottomLegend(chart.Options.LegendPosition) ? area.Bottom - 18 - Math.Max(0, rows - 1) * RadialBarLegendRowHeight(chart) : area.Top + 16;

    private static double RadialBarLegendRowX(Chart chart, ChartRect area, double rowWidth) {
        if (chart.Options.LegendPosition == ChartLegendPosition.TopRight || chart.Options.LegendPosition == ChartLegendPosition.BottomRight || IsRightLegend(chart.Options.LegendPosition)) return area.Right - Math.Min(area.Width, rowWidth);
        if (chart.Options.LegendPosition == ChartLegendPosition.Top || chart.Options.LegendPosition == ChartLegendPosition.Bottom) return area.X + Math.Max(0, (area.Width - rowWidth) / 2.0);
        return area.X;
    }

    private static double RadialBarLegendRowHeight(Chart chart) => chart.Options.Theme.LegendFontSize + 10;

    private static ChartColor RadialBarColor(ChartSeries series, ChartForgeX.Themes.ChartTheme theme, int pointIndex) {
        if (pointIndex < series.PointColors.Count && series.PointColors[pointIndex].HasValue) return series.PointColors[pointIndex]!.Value;
        return series.Color ?? theme.Palette[pointIndex % theme.Palette.Length];
    }

    private static string BuildRadialBarArc(double cx, double cy, double radius, double start, double end) {
        if (end - start >= Math.PI * 2 - 0.000001) {
            var mid = start + Math.PI;
            var x1 = cx + Math.Cos(start) * radius;
            var y1 = cy + Math.Sin(start) * radius;
            var xm = cx + Math.Cos(mid) * radius;
            var ym = cy + Math.Sin(mid) * radius;
            var x2 = cx + Math.Cos(start + Math.PI * 2) * radius;
            var y2 = cy + Math.Sin(start + Math.PI * 2) * radius;
            return new SvgPathDataBuilder()
                .MoveTo(x1, y1)
                .ArcTo(radius, radius, 0, false, true, xm, ym)
                .ArcTo(radius, radius, 0, false, true, x2, y2)
                .Build();
        }

        return BuildRadialBarArcSegment(cx, cy, radius, start, end, end - start > Math.PI);
    }

    private static string BuildRadialBarArcSegment(double cx, double cy, double radius, double start, double end, bool largeArc) {
        var x1 = cx + Math.Cos(start) * radius;
        var y1 = cy + Math.Sin(start) * radius;
        var x2 = cx + Math.Cos(end) * radius;
        var y2 = cy + Math.Sin(end) * radius;
        return new SvgPathDataBuilder()
            .MoveTo(x1, y1)
            .ArcTo(radius, radius, 0, largeArc, true, x2, y2)
            .Build();
    }

    private sealed class RadialBarLegendRow {
        public List<RadialBarLegendItem> Items { get; } = new();
        public double Width { get; set; }
    }

    private readonly struct RadialBarLegendItem {
        public RadialBarLegendItem(int pointIndex, double x, double width, string label, string value, ChartColor color) {
            PointIndex = pointIndex;
            X = x;
            Width = width;
            Label = label;
            Value = value;
            Color = color;
        }

        public int PointIndex { get; }
        public double X { get; }
        public double Width { get; }
        public string Label { get; }
        public string Value { get; }
        public ChartColor Color { get; }
    }
}
