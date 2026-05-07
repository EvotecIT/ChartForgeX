using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawPieLike(StringBuilder sb, Chart chart, ChartRect plot, string id) {
        var series = chart.Series[0];
        var values = series.Points
            .Select((point, index) => new IndexedPieValue(point, index))
            .Where(item => item.Point.Y > 0)
            .ToArray();
        if (values.Length == 0) return;
        var legendValues = series.Points
            .Select((point, index) => new IndexedPieValue(point, index))
            .ToArray();

        var t = chart.Options.Theme;
        var total = values.Sum(item => item.Point.Y);
        var chartPlot = PieChartPlot(chart, plot, legendValues);
        var radiusFactor = chart.Options.ShowLegend && IsTopOrBottomLegend(chart.Options.LegendPosition) ? 0.40 : 0.44;
        var radius = Math.Max(1, Math.Min(chartPlot.Width, chartPlot.Height) * radiusFactor);
        var cx = chartPlot.Left + chartPlot.Width / 2;
        var cy = chartPlot.Top + chartPlot.Height / 2;
        var inner = series.Kind == ChartSeriesKind.Donut ? radius * chart.Options.DonutInnerRadiusRatio : 0;
        var start = -Math.PI / 2;
        var sliceRole = series.Kind == ChartSeriesKind.Donut ? "donut-slice" : "pie-slice";
        var outsideLabels = new List<PieLabelCandidate>();
        var writer = new SvgMarkupWriter(4096);

        for (var i = 0; i < values.Length; i++) {
            var point = values[i].Point;
            var pointIndex = values[i].PointIndex;
            var sweep = point.Y / total * Math.PI * 2;
            var end = start + sweep;
            var label = SliceLabel(chart, point, pointIndex);
            var percent = point.Y / total;
            var mid = start + sweep / 2;
            var offset = PieSliceOffset(series, pointIndex) * radius;
            var sliceCx = cx + Math.Cos(mid) * offset;
            var sliceCy = cy + Math.Sin(mid) * offset;
            writer
                .StartElement("path")
                .Attribute("data-cfx-role", sliceRole)
                .Attribute("data-cfx-point", pointIndex)
                .Attribute("data-cfx-label", label)
                .Attribute("data-cfx-value", point.Y)
                .Attribute("data-cfx-percent", percent);
            if (series.Kind == ChartSeriesKind.Donut) writer.Attribute("data-cfx-inner-radius-ratio", chart.Options.DonutInnerRadiusRatio);
            if (offset > 0) writer.Attribute("data-cfx-slice-offset", PieSliceOffset(series, pointIndex));
            writer
                .Attribute("d", BuildSlicePath(sliceCx, sliceCy, radius, inner, start, end))
                .Attribute("fill", PieSliceFill(chart, series, pointIndex, id))
                .Attribute("fill-rule", "evenodd")
                .Attribute("stroke", t.CardBackground.ToCss())
                .Attribute("stroke-width", ChartVisualPrimitives.SliceSeparatorStrokeWidth)
                .EndEmptyElement()
                .Line();

            if (ShouldDrawDataLabels(chart, series) && sweep > 0.22) {
                var placement = DataLabelPlacement(chart, series);
                var sliceText = FormatPieSliceLabel(chart, series, label, point.Y, percent, pointIndex);
                var labelRadius = PieLabelRadius(inner, radius, placement);
                var x = sliceCx + Math.Cos(mid) * labelRadius;
                var y = sliceCy + Math.Sin(mid) * labelRadius + 4;
                var isSideLabel = IsPieSideLabelPlacement(placement);
                var isLeftSide = placement == ChartDataLabelPlacement.Left || (placement == ChartDataLabelPlacement.Outside && Math.Cos(mid) < 0);
                if (isSideLabel) x = cx + (isLeftSide ? -radius * chart.Options.PieOutsideLabelDistanceRatio : radius * chart.Options.PieOutsideLabelDistanceRatio);
                if (placement == ChartDataLabelPlacement.Above) y = cy - radius * 1.10;
                else if (placement == ChartDataLabelPlacement.Below) y = cy + radius * 1.10;
                var style = DataLabelStyle(chart, series, pointIndex);
                if (placement == ChartDataLabelPlacement.Auto || placement == ChartDataLabelPlacement.Inside || placement == ChartDataLabelPlacement.Center) {
                    var maxLabelWidth = Math.Max(24, radius * 1.08);
                    var fontSize = TextFontSizeForSvgWidth(sliceText, maxLabelWidth, StyleFontSize(style, t.DataLabelFontSize));
                    sliceText = TrimSvgLabelToWidth(sliceText, fontSize, maxLabelWidth);
                    if (sliceText.Length > 0) {
                        writer
                            .StartElement("text")
                            .Attribute("data-cfx-role", "data-label")
                            .Attribute("x", x)
                            .Attribute("y", y)
                            .Attribute("text-anchor", "middle")
                            .Attribute("fill", StyleColor(style, t.CardBackground).ToCss())
                            .Attribute("font-family", SvgFontFamily(StyleFontFamily(chart, style)))
                            .Attribute("font-size", fontSize)
                            .Attribute("font-weight", StyleWeight(style, "750"));
                        WritePieTextStyleAttributes(writer, style);
                        writer
                            .Text(sliceText)
                            .EndElement()
                            .Line();
                    }
                } else if (isSideLabel) {
                    outsideLabels.Add(new PieLabelCandidate(pointIndex, sliceText, mid, x, y - 4, sliceCx, sliceCy, isLeftSide));
                } else {
                    DrawPieLabelConnector(writer, chart, series, pointIndex, sliceCx, sliceCy, radius, mid, x, y - 4);
                    sb.Append(writer.Build());
                    writer = new SvgMarkupWriter(1024);
                    DrawDataLabel(sb, chart, sliceText, x, y, plot, series: series, pointIndex: pointIndex);
                }
            }

            start = end;
        }

        sb.Append(writer.Build());
        DrawPieOutsideLabels(sb, chart, outsideLabels, radius, plot, series);

        if (series.Kind == ChartSeriesKind.Donut && chart.Options.ShowDonutCenterLabel && series.ShowDataLabels != false) {
            var centerLabelWidth = Math.Max(24, inner * 1.55);
            var centerValue = chart.Options.DonutCenterValue ?? FormatValue(chart, total);
            var centerLabel = chart.Options.DonutCenterLabel ?? series.Name;
            var valueFontSize = Math.Max(14, Math.Min(26, inner * 0.45));
            var labelFontSize = Math.Max(9, Math.Min(t.TickLabelFontSize, inner * 0.22));
            var centerLineGap = Math.Max(4, Math.Min(8, inner * 0.08));
            var centerGroupHeight = valueFontSize + centerLineGap + labelFontSize;
            var valueY = cy - centerGroupHeight / 2.0 + valueFontSize / 2.0;
            var labelY = valueY + valueFontSize / 2.0 + centerLineGap + labelFontSize / 2.0;
            var centerWriter = new SvgMarkupWriter(512);
            DrawSvgTextCenteredX(centerWriter, chart, "donut-total-label", centerValue, cx, valueY, t.Text, valueFontSize, centerLabelWidth, "850");
            DrawSvgTextCenteredX(centerWriter, chart, "donut-title", centerLabel, cx, labelY, t.MutedText, labelFontSize, centerLabelWidth, "650");
            sb.Append(centerWriter.Build());
        }

        if (chart.Options.ShowLegend) DrawSliceLegend(sb, chart, series, legendValues, plot, total);
    }

    private static void DrawSliceLegend(StringBuilder sb, Chart chart, ChartSeries series, IReadOnlyList<IndexedPieValue> values, ChartRect plot, double total) {
        var t = chart.Options.Theme;
        var area = SliceLegendArea(chart, plot, values);
        var rows = BuildSliceLegendRows(chart, series, values, total, area.Width);
        var y = SliceLegendStartY(chart, area, rows.Count);
        var writer = new SvgMarkupWriter(2048);
        writer
            .StartElement("g")
            .Attribute("data-cfx-role", "slice-legend")
            .Attribute("data-cfx-position", chart.Options.LegendPosition.ToString())
            .EndStartElement()
            .Line();
        foreach (var row in rows) {
            if (y > area.Bottom - 4) break;
            var x = SliceLegendRowX(chart, area, row.Width);
            foreach (var item in row.Items) {
                var itemX = x + item.X;
                var percentWidth = EstimateTextWidth(item.Percent, t.LegendFontSize);
                var labelMaxWidth = Math.Max(8, item.Width - percentWidth - ChartVisualPrimitives.SliceLegendSwatchSize - 28);
                var labelFontSize = TextFontSizeForSvgWidth(item.Label, labelMaxWidth, t.LegendFontSize);
                var label = TrimSvgLabelToWidth(item.Label, labelFontSize, labelMaxWidth);
                if (item.IsZero) {
                    writer
                        .StartElement("rect")
                        .Attribute("data-cfx-role", "slice-legend-swatch")
                        .Attribute("data-cfx-point", item.PointIndex)
                        .Attribute("data-cfx-zero", true)
                        .Attribute("x", itemX)
                        .Attribute("y", y - ChartVisualPrimitives.SliceLegendSwatchSize + 1)
                        .Attribute("width", ChartVisualPrimitives.SliceLegendSwatchSize)
                        .Attribute("height", ChartVisualPrimitives.SliceLegendSwatchSize)
                        .Attribute("rx", ChartVisualPrimitives.SliceLegendSwatchRadius)
                        .Attribute("fill", item.Color.ToCss())
                        .Attribute("fill-opacity", ChartVisualPrimitives.PolarAreaZeroSlotFillOpacity)
                        .Attribute("stroke", item.Color.ToCss())
                        .Attribute("stroke-opacity", ChartVisualPrimitives.PolarAreaZeroSlotStrokeOpacity)
                        .EndEmptyElement()
                        .Line();
                } else {
                    writer
                        .StartElement("rect")
                        .Attribute("data-cfx-role", "slice-legend-swatch")
                        .Attribute("data-cfx-point", item.PointIndex)
                        .Attribute("x", itemX)
                        .Attribute("y", y - ChartVisualPrimitives.SliceLegendSwatchSize + 1)
                        .Attribute("width", ChartVisualPrimitives.SliceLegendSwatchSize)
                        .Attribute("height", ChartVisualPrimitives.SliceLegendSwatchSize)
                        .Attribute("rx", ChartVisualPrimitives.SliceLegendSwatchRadius)
                        .Attribute("fill", item.Color.ToCss())
                        .EndEmptyElement()
                        .Line();
                }
                var labelColor = item.IsZero ? t.MutedText : t.Text;
                if (label.Length > 0) {
                    writer
                        .StartElement("text")
                        .Attribute("data-cfx-role", "slice-legend-label")
                        .Attribute("data-cfx-point", item.PointIndex)
                        .Attribute("x", itemX + ChartVisualPrimitives.SliceLegendSwatchSize + 6)
                        .Attribute("y", y)
                        .Attribute("fill", labelColor.ToCss())
                        .Attribute("font-family", SvgFontFamily(t.FontFamily))
                        .Attribute("font-size", labelFontSize)
                        .Attribute("font-weight", "650")
                        .Text(label)
                        .EndElement()
                        .Line();
                }

                writer
                    .StartElement("text")
                    .Attribute("data-cfx-role", "slice-legend-percent")
                    .Attribute("data-cfx-point", item.PointIndex)
                    .Attribute("x", itemX + item.Width - 10)
                    .Attribute("y", y)
                    .Attribute("text-anchor", "end")
                    .Attribute("fill", t.MutedText.ToCss())
                    .Attribute("font-family", SvgFontFamily(t.FontFamily))
                    .Attribute("font-size", t.LegendFontSize)
                    .Text(item.Percent)
                    .EndElement()
                    .Line();
            }

            y += SliceLegendRowHeight(chart);
        }

        writer.EndElement().Line();
        sb.Append(writer.Build());
    }

    private static ChartRect PieChartPlot(Chart chart, ChartRect plot, IReadOnlyList<IndexedPieValue> values) {
        if (!chart.Options.ShowLegend || values.Count == 0) return plot;
        var reserve = SliceLegendReserve(chart, values, plot);
        if (IsLeftLegend(chart.Options.LegendPosition)) return new ChartRect(plot.X + reserve, plot.Y, Math.Max(1, plot.Width - reserve), plot.Height);
        if (IsRightLegend(chart.Options.LegendPosition)) return new ChartRect(plot.X, plot.Y, Math.Max(1, plot.Width - reserve), plot.Height);
        if (IsTopLegend(chart.Options.LegendPosition)) return new ChartRect(plot.X, plot.Y + reserve, plot.Width, Math.Max(1, plot.Height - reserve));
        if (IsBottomLegend(chart.Options.LegendPosition)) return new ChartRect(plot.X, plot.Y, plot.Width, Math.Max(1, plot.Height - reserve));
        return plot;
    }

    private static double SliceLegendReserve(Chart chart, IReadOnlyList<IndexedPieValue> values, ChartRect plot) {
        if (IsLeftLegend(chart.Options.LegendPosition) || IsRightLegend(chart.Options.LegendPosition)) return Math.Min(230, Math.Max(142, SliceLegendWidestItem(chart, values) + 22)) + ChartVisualPrimitives.SideLegendPlotGap;
        return 18 + BuildSliceLegendRows(chart, chart.Series[0], values, values.Sum(item => item.Point.Y), Math.Max(80, plot.Width - 80)).Count * SliceLegendRowHeight(chart) + ChartVisualPrimitives.LegendPlotGap;
    }

    private static double SliceLegendWidestItem(Chart chart, IReadOnlyList<IndexedPieValue> values) {
        var widest = 0.0;
        var total = Math.Max(0.000001, values.Sum(item => item.Point.Y));
        for (var i = 0; i < values.Count; i++) {
            var label = SliceLabel(chart, values[i].Point, values[i].PointIndex);
            var percent = FormatPercent(values[i].Point.Y / total);
            widest = Math.Max(widest, ChartVisualPrimitives.SliceLegendSwatchSize + EstimateTextWidth(label, chart.Options.Theme.LegendFontSize) + EstimateTextWidth(percent, chart.Options.Theme.LegendFontSize) + 34);
        }

        return widest;
    }

    private static ChartRect SliceLegendArea(Chart chart, ChartRect plot, IReadOnlyList<IndexedPieValue> values) {
        var reserve = SliceLegendReserve(chart, values, plot);
        if (IsLeftLegend(chart.Options.LegendPosition)) return new ChartRect(plot.Left + 18, plot.Top + 20, Math.Max(1, reserve - 28), Math.Max(1, plot.Height - 40));
        if (IsRightLegend(chart.Options.LegendPosition)) return new ChartRect(plot.Right - reserve + 10, plot.Top + 20, Math.Max(1, reserve - 28), Math.Max(1, plot.Height - 40));
        var y = IsTopLegend(chart.Options.LegendPosition) ? plot.Top + 14 : plot.Bottom - reserve - 4;
        return new ChartRect(plot.Left + 36, y, Math.Max(1, plot.Width - 72), reserve);
    }

    private static List<SliceLegendRow> BuildSliceLegendRows(Chart chart, ChartSeries series, IReadOnlyList<IndexedPieValue> values, double total, double width) {
        var rows = new List<SliceLegendRow>();
        var row = new SliceLegendRow();
        rows.Add(row);
        var x = 0.0;
        var vertical = IsLeftLegend(chart.Options.LegendPosition) || IsRightLegend(chart.Options.LegendPosition);
        var maxX = Math.Max(48, width);
        for (var i = 0; i < values.Count; i++) {
            var pointIndex = values[i].PointIndex;
            var percent = FormatPercent(values[i].Point.Y / Math.Max(0.000001, total));
            var percentWidth = EstimateTextWidth(percent, chart.Options.Theme.LegendFontSize);
            var labelMax = Math.Max(24, maxX - percentWidth - ChartVisualPrimitives.SliceLegendSwatchSize - 30);
            var rawLabel = SliceLabel(chart, values[i].Point, pointIndex);
            var labelFontSize = TextFontSizeForSvgWidth(rawLabel, labelMax, chart.Options.Theme.LegendFontSize);
            var label = TrimSvgLabelToWidth(rawLabel, labelFontSize, labelMax);
            var itemWidth = Math.Min(maxX, ChartVisualPrimitives.SliceLegendSwatchSize + EstimateTextWidth(label, labelFontSize) + percentWidth + 32);
            if (row.Items.Count > 0 && (vertical || x + itemWidth > maxX)) {
                row = new SliceLegendRow();
                rows.Add(row);
                x = 0;
            }

            row.Items.Add(new SliceLegendItem(pointIndex, x, itemWidth, label, percent, PieSliceColor(chart, series, pointIndex), values[i].Point.Y <= 0));
            row.Width = Math.Max(row.Width, x + itemWidth);
            x += itemWidth;
        }

        return rows;
    }

    private static double SliceLegendStartY(Chart chart, ChartRect area, int rows) =>
        IsBottomLegend(chart.Options.LegendPosition) ? area.Bottom - 18 - Math.Max(0, rows - 1) * SliceLegendRowHeight(chart) : area.Top + 16;

    private static double SliceLegendRowX(Chart chart, ChartRect area, double rowWidth) {
        if (chart.Options.LegendPosition == ChartLegendPosition.TopRight || chart.Options.LegendPosition == ChartLegendPosition.BottomRight || IsRightLegend(chart.Options.LegendPosition)) return area.Right - Math.Min(area.Width, rowWidth);
        if (chart.Options.LegendPosition == ChartLegendPosition.Top || chart.Options.LegendPosition == ChartLegendPosition.Bottom) return area.X + Math.Max(0, (area.Width - rowWidth) / 2.0);
        return area.X;
    }

    private static bool IsTopOrBottomLegend(ChartLegendPosition position) => IsTopLegend(position) || IsBottomLegend(position);

    private static double SliceLegendRowHeight(Chart chart) => chart.Options.Theme.LegendFontSize + 10;

    private static double PieLabelRadius(double inner, double radius, ChartDataLabelPlacement placement) {
        if (placement == ChartDataLabelPlacement.Outside || placement == ChartDataLabelPlacement.Left || placement == ChartDataLabelPlacement.Right || placement == ChartDataLabelPlacement.Above || placement == ChartDataLabelPlacement.Below) return radius * 1.10;
        return inner > 0 ? (inner + radius) / 2 : radius * 0.66;
    }

    private static double PieSliceOffset(ChartSeries series, int pointIndex) =>
        pointIndex >= 0 && pointIndex < series.PointSliceOffsets.Count ? series.PointSliceOffsets[pointIndex] : 0;

    private static bool IsPieSideLabelPlacement(ChartDataLabelPlacement placement) =>
        placement == ChartDataLabelPlacement.Outside || placement == ChartDataLabelPlacement.Left || placement == ChartDataLabelPlacement.Right;

    private static string FormatPieSliceLabel(Chart chart, ChartSeries series, string label, double value, double percent, int pointIndex) {
        var formattedValue = FormatValue(chart, value);
        var formattedPercent = FormatPercent(percent);
        if (chart.Options.PieSliceLabelFormatter != null) {
            return chart.Options.PieSliceLabelFormatter(new ChartPieSliceLabelContext(series.Name, label, value, percent, formattedValue, formattedPercent, pointIndex)) ?? string.Empty;
        }

        switch (chart.Options.PieSliceLabelContent) {
            case ChartPieSliceLabelContent.Value:
                return formattedValue;
            case ChartPieSliceLabelContent.Label:
                return label;
            case ChartPieSliceLabelContent.LabelAndPercent:
                return label + " " + formattedPercent;
            case ChartPieSliceLabelContent.LabelAndValue:
                return label + " " + formattedValue;
            default:
                return formattedPercent;
        }
    }

    private static void DrawPieOutsideLabels(StringBuilder sb, Chart chart, List<PieLabelCandidate> labels, double radius, ChartRect plot, ChartSeries series) {
        if (labels.Count == 0) return;
        ArrangePieLabelLane(labels.Where(label => label.IsLeftSide).ToList(), chart, plot);
        ArrangePieLabelLane(labels.Where(label => !label.IsLeftSide).ToList(), chart, plot);
        foreach (var label in labels) {
            var anchor = label.IsLeftSide ? "end" : "start";
            var style = DataLabelStyle(chart, series, label.PointIndex);
            var fontSize = StyleFontSize(style, chart.Options.Theme.DataLabelFontSize);
            var maxLabelWidth = label.IsLeftSide ? label.X - plot.Left - 6 : plot.Right - label.X - 6;
            var text = TrimSvgLabelToWidth(label.Text, fontSize, Math.Max(8, maxLabelWidth));
            if (text.Length == 0) continue;
            var writer = new SvgMarkupWriter(256);
            DrawPieLabelConnector(writer, chart, series, label.PointIndex, label.SliceCx, label.SliceCy, radius, label.Angle, label.X, label.Y);
            sb.Append(writer.Build());
            DrawHorizontalValueLabel(sb, chart, text, label.X, label.Y, anchor, plot, series, label.PointIndex);
        }
    }

    private static void ArrangePieLabelLane(List<PieLabelCandidate> labels, Chart chart, ChartRect plot) {
        if (labels.Count == 0) return;
        var gap = Math.Max(14, StyleFontSize(chart.Options.DataLabelStyle, chart.Options.Theme.DataLabelFontSize) + 7);
        var top = plot.Top + gap;
        var bottom = plot.Bottom - gap;
        labels.Sort(static (left, right) => left.Y.CompareTo(right.Y));
        for (var i = 0; i < labels.Count; i++) {
            labels[i].Y = Clamp(labels[i].Y, top, bottom);
            if (i > 0) labels[i].Y = Math.Max(labels[i].Y, labels[i - 1].Y + gap);
        }

        var overflow = labels[labels.Count - 1].Y - bottom;
        if (overflow > 0) {
            for (var i = 0; i < labels.Count; i++) labels[i].Y -= overflow;
        }

        for (var i = 0; i < labels.Count; i++) {
            labels[i].Y = Clamp(labels[i].Y, top, bottom);
            if (i > 0) labels[i].Y = Math.Max(labels[i].Y, labels[i - 1].Y + gap);
        }
    }

    private static string PieSliceFill(Chart chart, ChartSeries series, int pointIndex, string id) =>
        pointIndex < series.PointColors.Count && series.PointColors[pointIndex].HasValue
            ? series.PointColors[pointIndex]!.Value.ToCss()
            : $"url(#{id}-sliceFill{pointIndex % chart.Options.Theme.Palette.Length})";

    private static ChartColor PieSliceColor(Chart chart, ChartSeries series, int pointIndex) =>
        pointIndex < series.PointColors.Count && series.PointColors[pointIndex].HasValue
            ? series.PointColors[pointIndex]!.Value
            : chart.Options.Theme.Palette[pointIndex % chart.Options.Theme.Palette.Length];

    private static ChartColor PieConnectorColor(Chart chart, ChartSeries series, int pointIndex) =>
        chart.Options.DataLabelConnectorColor ?? PieSliceColor(chart, series, pointIndex);

    private static void WritePieTextStyleAttributes(SvgMarkupWriter writer, ChartTextStyle? style) {
        if (style == null) return;
        if (style.Italic) writer.Attribute("font-style", "italic");
        if (style.Underline) writer.Attribute("text-decoration", "underline");
    }

    private static void DrawPieLabelConnector(SvgMarkupWriter writer, Chart chart, ChartSeries series, int pointIndex, double cx, double cy, double radius, double angle, double labelX, double labelY) {
        var cos = Math.Cos(angle);
        var sin = Math.Sin(angle);
        var startX = cx + cos * radius * 1.01;
        var startY = cy + sin * radius * 1.01;
        var elbowX = cx + cos * radius * 1.11;
        var elbowY = cy + sin * radius * 1.11;
        var side = labelX < cx ? -1 : 1;
        var targetX = labelX - side * 8;
        var tickStartX = targetX - side * Math.Min(16, Math.Max(9, radius * 0.07));
        string d;
        if (chart.Options.DataLabelConnectorStyle == ChartDataLabelConnectorStyle.Straight) {
            d = $"M {F(startX)} {F(startY)} L {F(targetX)} {F(labelY)}";
        } else if (chart.Options.DataLabelConnectorStyle == ChartDataLabelConnectorStyle.Curve) {
            var control1X = startX + side * radius * 0.16;
            var control1Y = startY;
            var control2X = tickStartX - side * Math.Min(radius * 0.24, Math.Abs(tickStartX - startX) * 0.42);
            var control2Y = labelY;
            d = $"M {F(startX)} {F(startY)} C {F(control1X)} {F(control1Y)} {F(control2X)} {F(control2Y)} {F(tickStartX)} {F(labelY)} L {F(targetX)} {F(labelY)}";
        } else {
            d = $"M {F(startX)} {F(startY)} L {F(elbowX)} {F(elbowY)} L {F(tickStartX)} {F(labelY)} L {F(targetX)} {F(labelY)}";
        }

        writer
            .StartElement("path")
            .Attribute("data-cfx-role", "data-label-connector")
            .Attribute("data-cfx-point", pointIndex)
            .Attribute("data-cfx-connector-style", chart.Options.DataLabelConnectorStyle.ToString())
            .Attribute("d", d)
            .Attribute("fill", "none")
            .Attribute("stroke", PieConnectorColor(chart, series, pointIndex).ToCss())
            .Attribute("stroke-width", chart.Options.DataLabelConnectorStrokeWidth)
            .Attribute("stroke-opacity", chart.Options.DataLabelConnectorOpacity)
            .Attribute("stroke-linecap", "round")
            .Attribute("stroke-linejoin", "round")
            .EndEmptyElement()
            .Line();
    }

    private sealed class PieLabelCandidate {
        public PieLabelCandidate(int pointIndex, string text, double angle, double x, double y, double sliceCx, double sliceCy, bool isLeftSide) {
            PointIndex = pointIndex;
            Text = text;
            Angle = angle;
            X = x;
            Y = y;
            SliceCx = sliceCx;
            SliceCy = sliceCy;
            IsLeftSide = isLeftSide;
        }

        public int PointIndex { get; }

        public string Text { get; }

        public double Angle { get; }

        public double X { get; }

        public double Y { get; set; }

        public double SliceCx { get; }

        public double SliceCy { get; }

        public bool IsLeftSide { get; }
    }

    private sealed class SliceLegendRow {
        public List<SliceLegendItem> Items { get; } = new();
        public double Width { get; set; }
    }

    private readonly struct SliceLegendItem {
        public SliceLegendItem(int pointIndex, double x, double width, string label, string percent, ChartColor color, bool isZero) {
            PointIndex = pointIndex;
            X = x;
            Width = width;
            Label = label;
            Percent = percent;
            Color = color;
            IsZero = isZero;
        }

        public int PointIndex { get; }
        public double X { get; }
        public double Width { get; }
        public string Label { get; }
        public string Percent { get; }
        public ChartColor Color { get; }
        public bool IsZero { get; }
    }

    private readonly struct IndexedPieValue {
        public IndexedPieValue(ChartPoint point, int pointIndex) {
            Point = point;
            PointIndex = pointIndex;
        }

        public ChartPoint Point { get; }
        public int PointIndex { get; }
    }
}
