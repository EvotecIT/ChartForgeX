using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawHeatmap(StringBuilder sb, Chart chart, ChartRect basePlot) {
        var rows = chart.Series.Where(series => series.Kind == ChartSeriesKind.Heatmap).ToArray();
        if (rows.Length == 0) return;

        var columns = HeatmapColumns(rows);
        if (columns.Length == 0) return;

        var t = chart.Options.Theme;
        var values = rows.SelectMany(series => series.Points.Select(point => point.Y)).ToArray();
        var min = values.Length == 0 ? 0 : values.Min();
        var max = values.Length == 0 ? 1 : values.Max();
        if (Math.Abs(max - min) < 0.000001) max = min + 1;

        var plot = ApplyHeatmapLabelReserve(chart, basePlot, rows, columns);
        var autoGap = Math.Min(6, Math.Max(2, Math.Min(plot.Width / columns.Length, plot.Height / rows.Length) * 0.05));
        var gap = VisualBlockRendering.EffectiveHeatmapGap(plot.Width, plot.Height, columns.Length, rows.Length, chart.Options.HeatmapCellGap ?? autoGap);
        var cellWidth = Math.Max(1, (plot.Width - gap * (columns.Length - 1)) / columns.Length);
        var cellHeight = Math.Max(1, (plot.Height - gap * (rows.Length - 1)) / rows.Length);
        var autoRadius = Math.Min(8, Math.Min(cellWidth, cellHeight) * 0.16);
        var radius = Math.Min(chart.Options.HeatmapCellRadius ?? autoRadius, Math.Min(cellWidth, cellHeight) / 2);

        var body = new StringBuilder();
        for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++) {
            var series = rows[rowIndex];
            var y = plot.Top + rowIndex * (cellHeight + gap);
            if (chart.Options.ShowAxes) {
                var rowLabelWidth = Math.Max(8, plot.Left - 24);
                var rowLabelFontSize = TextFontSizeForSvgWidth(series.Name, rowLabelWidth, t.TickLabelFontSize);
                var rowLabel = TrimSvgLabelToWidth(series.Name, rowLabelFontSize, rowLabelWidth);
                if (rowLabel.Length > 0) {
                    WriteHeatmapRowLabel(body, chart, plot.Left - 12, y + cellHeight / 2, rowLabelFontSize, rowLabel);
                }
            }
            for (var columnIndex = 0; columnIndex < columns.Length; columnIndex++) {
                var column = columns[columnIndex];
                var pointIndex = HeatmapPointIndex(series, column);
                if (pointIndex < 0) continue;
                var value = FindHeatmapValue(series, column);
                var x = plot.Left + columnIndex * (cellWidth + gap);
                var ratio = ChartHeatmapSurface.Ratio(value, min, max);
                var status = ChartHeatmapSurface.Status(ratio);
                var color = ChartHeatmapSurface.Color(chart, series.Color, value, min, max);
                var summary = series.Name + ", " + FormatX(chart, column) + ": " + FormatValue(chart, value);
                if (chart.Options.HeatmapScale == ChartHeatmapScale.Semantic) summary += ", " + status;
                WriteHeatmapCell(body, chart, rowIndex, columnIndex, status, summary, x, y, cellWidth, cellHeight, radius, color);
                var labelFits = cellWidth >= 34 && cellHeight >= 20;
                var drawValueText = chart.Options.HeatmapValueTextMode == ChartHeatmapValueTextMode.Always ||
                    chart.Options.HeatmapValueTextMode == ChartHeatmapValueTextMode.Auto && ShouldDrawDataLabels(chart, series) && labelFits;
                if (drawValueText) {
                    var label = FormatDataLabel(chart, series, pointIndex, value);
                    var placement = DataLabelPlacement(chart, series);
                    if (placement == ChartDataLabelPlacement.Auto || placement == ChartDataLabelPlacement.Inside || placement == ChartDataLabelPlacement.Center) {
                        DrawSvgTextCenteredX(body, chart, "data-label", label, x + cellWidth / 2, y + cellHeight / 2, ChartColorMath.TextOnBackground(color), t.DataLabelFontSize, cellWidth - 6, "750", style: DataLabelStyle(chart, series, pointIndex));
                    } else if (placement == ChartDataLabelPlacement.Left || placement == ChartDataLabelPlacement.Right || placement == ChartDataLabelPlacement.Outside) {
                        var labelX = placement == ChartDataLabelPlacement.Left ? x - 8 : x + cellWidth + 8;
                        var anchor = placement == ChartDataLabelPlacement.Left ? "end" : "start";
                        var connectorStartX = placement == ChartDataLabelPlacement.Left ? x : x + cellWidth;
                        var connectorEndX = placement == ChartDataLabelPlacement.Left ? x - 5 : x + cellWidth + 5;
                        DrawHeatmapLabelConnector(body, chart, rowIndex, columnIndex, connectorStartX, y + cellHeight / 2, connectorEndX);
                        DrawHorizontalValueLabel(body, chart, label, labelX, y + cellHeight / 2, anchor, basePlot, series, pointIndex);
                    } else {
                        DrawDataLabel(body, chart, label, x + cellWidth / 2, placement == ChartDataLabelPlacement.Above ? y - 8 : y + cellHeight + 12, plot, series: series, pointIndex: pointIndex);
                    }
                }
            }
        }

        if (chart.Options.ShowAxes && chart.Options.ShowHeatmapColumnLabels) {
            for (var columnIndex = 0; columnIndex < columns.Length; columnIndex++) {
                var x = plot.Left + columnIndex * (cellWidth + gap) + cellWidth / 2;
                var label = FormatX(chart, columns[columnIndex]);
                var labelWidth = Math.Max(8, cellWidth + gap);
                var labelFontSize = TextFontSizeForSvgWidth(label, labelWidth, t.TickLabelFontSize);
                label = TrimSvgLabelToWidth(label, labelFontSize, labelWidth);
                if (label.Length == 0) continue;
                var anchor = EdgeAwareAnchor(label, x, plot, labelFontSize);
                var labelX = EdgeAwareTextX(label, x, plot, labelFontSize);
                WriteHeatmapColumnLabel(body, chart, labelX, plot.Bottom + 22, anchor, labelFontSize, label);
            }

            DrawSvgXAxisTitle(body, chart, plot, plot.Bottom + 48, "heatmap-x-axis-title");
            if (!string.IsNullOrWhiteSpace(chart.YAxisTitle)) {
                var widestRowLabel = rows.Max(series => EstimateTextWidth(series.Name, t.TickLabelFontSize));
                var axisX = Math.Max(24, plot.Left - widestRowLabel - 48);
                DrawSvgYAxisTitle(body, chart, plot, axisX, "heatmap-y-axis-title");
            }
        }

        if (chart.Options.ShowHeatmapScale) DrawHeatmapScale(body, chart, plot, min, max, rows[0].Color);

        var writer = new SvgMarkupWriter(body.Length + 128);
        writer
            .StartElement("g")
            .Attribute("data-cfx-role", "heatmap")
            .Attribute("data-cfx-row-count", rows.Length)
            .Attribute("data-cfx-column-count", columns.Length)
            .Attribute("data-cfx-min", min)
            .Attribute("data-cfx-max", max)
            .Attribute("data-cfx-cell-gap", gap)
            .Attribute("data-cfx-cell-radius", radius)
            .Attribute("data-cfx-value-text-mode", chart.Options.HeatmapValueTextMode.ToString())
            .Raw(Environment.NewLine)
            .Raw(body.ToString())
            .EndElement()
            .Line();
        sb.Append(writer.Build());
    }

    private static void WriteHeatmapRowLabel(StringBuilder sb, Chart chart, double x, double y, double fontSize, string label) {
        var t = chart.Options.Theme;
        var writer = new SvgMarkupWriter(384);
        writer
            .StartElement("text")
            .Attribute("data-cfx-role", "heatmap-row-label")
            .Attribute("x", x)
            .Attribute("y", y)
            .Attribute("text-anchor", "end")
            .Attribute("dominant-baseline", "middle")
            .Attribute("fill", t.MutedText.ToCss())
            .Attribute("font-family", SvgFontFamilyAttributeValue(t.FontFamily))
            .Attribute("font-size", fontSize)
            .Attribute("font-weight", "650")
            .Text(label)
            .EndElement()
            .Line();
        sb.Append(writer.Build());
    }

    private static void WriteHeatmapCell(StringBuilder sb, Chart chart, int rowIndex, int columnIndex, string status, string summary, double x, double y, double width, double height, double radius, ChartColor color) {
        var t = chart.Options.Theme;
        var writer = new SvgMarkupWriter(768);
        writer
            .StartElement("rect")
            .Attribute("class", "cfx-interactive-region")
            .Attribute("tabindex", "0")
            .Attribute("focusable", "true")
            .Attribute("data-cfx-role", "heatmap-cell")
            .Attribute("data-cfx-row", rowIndex)
            .Attribute("data-cfx-column", columnIndex)
            .Attribute("data-cfx-status", status)
            .Attribute("role", "img")
            .Attribute("aria-label", summary)
            .Attribute("x", x)
            .Attribute("y", y)
            .Attribute("width", width)
            .Attribute("height", height)
            .Attribute("rx", radius)
            .Attribute("fill", color.ToCss())
            .Attribute("stroke", t.CardBackground.ToCss())
            .Attribute("stroke-opacity", ChartVisualPrimitives.HeatmapCellBorderOpacity)
            .Attribute("stroke-width", ChartVisualPrimitives.HeatmapCellBorderStrokeWidth)
            .EndStartElement()
            .StartElement("title")
            .Text(summary)
            .EndElement()
            .EndElement()
            .Line();
        sb.Append(writer.Build());
    }

    private static void WriteHeatmapColumnLabel(StringBuilder sb, Chart chart, double x, double y, string anchor, double fontSize, string label) {
        var t = chart.Options.Theme;
        var writer = new SvgMarkupWriter(384);
        writer
            .StartElement("text")
            .Attribute("data-cfx-role", "heatmap-column-label")
            .Attribute("x", x)
            .Attribute("y", y)
            .Attribute("text-anchor", anchor)
            .Attribute("fill", t.MutedText.ToCss())
            .Attribute("font-family", SvgFontFamilyAttributeValue(t.FontFamily))
            .Attribute("font-size", fontSize)
            .Attribute("font-weight", "650")
            .Text(label)
            .EndElement()
            .Line();
        sb.Append(writer.Build());
    }

    private static ChartRect ApplyHeatmapLabelReserve(Chart chart, ChartRect plot, IReadOnlyList<ChartSeries> rows, IReadOnlyList<double> columns) {
        var t = chart.Options.Theme;
        var yAxisReserve = chart.Options.ShowAxes && !string.IsNullOrWhiteSpace(chart.YAxisTitle) ? 28 : 0;
        var rowLabelReserve = chart.Options.ShowAxes ? rows.Max(series => EstimateTextWidth(series.Name, t.TickLabelFontSize)) + yAxisReserve + 18 : 0;
        var leftReserve = chart.Options.ShowAxes ? chart.Options.Padding.Left + rowLabelReserve : plot.Left;
        var sideLabelWidth = HeatmapSideLabelWidth(chart, rows, columns);
        var leftLabelReserve = HasHeatmapSideLabels(chart, rows, ChartDataLabelPlacement.Left) ? sideLabelWidth + 22 : 0;
        var rightLabelReserve = HasHeatmapSideLabels(chart, rows, ChartDataLabelPlacement.Right) || HasHeatmapSideLabels(chart, rows, ChartDataLabelPlacement.Outside) ? sideLabelWidth + 22 : 0;
        var axisBottomBase = chart.Options.ShowHeatmapScale ? 56 : chart.Options.ShowHeatmapColumnLabels ? 36 : 10;
        var bottomReserve = chart.Options.ShowAxes ? axisBottomBase + (string.IsNullOrWhiteSpace(chart.XAxisTitle) ? 0 : 20) : 0;
        var desiredLeft = Math.Max(plot.Left, leftReserve + leftLabelReserve);
        var maxLeft = Math.Max(plot.Left, chart.Options.Size.Width - chart.Options.Padding.Right - 220);
        var shift = Math.Max(0, Math.Min(desiredLeft, maxLeft) - plot.Left);
        var maxColumnLabel = chart.Options.ShowAxes && chart.Options.ShowHeatmapColumnLabels ? columns.Max(column => EstimateTextWidth(FormatX(chart, column), t.TickLabelFontSize)) : 0;
        var axesBottom = Math.Max(bottomReserve, maxColumnLabel > 68 ? 70 : bottomReserve);
        var bottom = chart.Options.ShowHeatmapScale ? Math.Max(axesBottom, 56) : axesBottom;
        return new ChartRect(plot.X + shift, plot.Y, Math.Max(1, plot.Width - shift - rightLabelReserve), Math.Max(1, plot.Height - bottom));
    }

    private static bool HasHeatmapSideLabels(Chart chart, IReadOnlyList<ChartSeries> rows, ChartDataLabelPlacement placement) {
        foreach (var row in rows) if (ShouldReserveHeatmapValueLabels(chart, row) && DataLabelPlacement(chart, row) == placement) return true;
        return false;
    }

    private static double HeatmapSideLabelWidth(Chart chart, IReadOnlyList<ChartSeries> rows, IReadOnlyList<double> columns) {
        var max = 0.0;
        foreach (var row in rows) {
            if (!ShouldReserveHeatmapValueLabels(chart, row)) continue;
            var placement = DataLabelPlacement(chart, row);
            if (placement != ChartDataLabelPlacement.Left && placement != ChartDataLabelPlacement.Right && placement != ChartDataLabelPlacement.Outside) continue;
            for (var i = 0; i < columns.Count; i++) {
                var pointIndex = HeatmapPointIndex(row, columns[i]);
                if (pointIndex < 0) continue;
                var style = DataLabelStyle(chart, row, pointIndex);
                var fontSize = StyleFontSize(style, chart.Options.Theme.DataLabelFontSize);
                max = Math.Max(max, EstimateTextWidth(FormatDataLabel(chart, row, pointIndex, FindHeatmapValue(row, columns[i])), fontSize));
            }
        }

        return Math.Min(88, max);
    }

    private static bool ShouldReserveHeatmapValueLabels(Chart chart, ChartSeries series) {
        if (chart.Options.HeatmapValueTextMode == ChartHeatmapValueTextMode.Hidden) return false;
        return chart.Options.HeatmapValueTextMode == ChartHeatmapValueTextMode.Always || ShouldDrawDataLabels(chart, series);
    }

    private static void DrawHeatmapScale(StringBuilder sb, Chart chart, ChartRect plot, double min, double max, ChartColor? highColor) {
        var t = chart.Options.Theme;
        const int steps = ChartVisualPrimitives.HeatmapScaleSteps;
        const double width = ChartVisualPrimitives.HeatmapScaleWidth;
        const double height = ChartVisualPrimitives.HeatmapScaleHeight;
        var x = plot.Right - width;
        var y = plot.Bottom + ChartVisualPrimitives.HeatmapScaleOffsetY;
        for (var i = 0; i < steps; i++) {
            var ratio = i / (double)(steps - 1);
            var value = min + (max - min) * ratio;
            var color = ChartHeatmapSurface.Color(chart, highColor, value, min, max);
            WriteHeatmapScaleStep(sb, x + i * width / steps, y, width / steps + ChartVisualPrimitives.HeatmapScaleStepOverlap, height, ChartHeatmapSurface.Status(ChartHeatmapSurface.Ratio(value, min, max)), color);
        }

        var labelMaxWidth = Math.Max(18, width * 0.46);
        var minLabel = FormatValue(chart, min);
        var minFontSize = TextFontSizeForSvgWidth(minLabel, labelMaxWidth, t.TickLabelFontSize);
        minLabel = TrimSvgLabelToWidth(minLabel, minFontSize, labelMaxWidth);
        var maxLabel = FormatValue(chart, max);
        var maxFontSize = TextFontSizeForSvgWidth(maxLabel, labelMaxWidth, t.TickLabelFontSize);
        maxLabel = TrimSvgLabelToWidth(maxLabel, maxFontSize, labelMaxWidth);
        if (minLabel.Length > 0) {
            WriteHeatmapScaleLabel(sb, chart, x, y + ChartVisualPrimitives.HeatmapScaleLabelOffsetY, "start", minFontSize, minLabel);
        }
        if (maxLabel.Length > 0) {
            WriteHeatmapScaleLabel(sb, chart, x + width, y + ChartVisualPrimitives.HeatmapScaleLabelOffsetY, "end", maxFontSize, maxLabel);
        }
    }

    private static void WriteHeatmapScaleStep(StringBuilder sb, double x, double y, double width, double height, string status, ChartColor color) {
        var writer = new SvgMarkupWriter(384);
        writer
            .StartElement("rect")
            .Attribute("data-cfx-role", "heatmap-scale-step")
            .Attribute("data-cfx-status", status)
            .Attribute("x", x)
            .Attribute("y", y)
            .Attribute("width", width)
            .Attribute("height", height)
            .Attribute("rx", ChartVisualPrimitives.HeatmapScaleRadius)
            .Attribute("fill", color.ToCss())
            .EndEmptyElement()
            .Line();
        sb.Append(writer.Build());
    }

    private static void WriteHeatmapScaleLabel(StringBuilder sb, Chart chart, double x, double y, string anchor, double fontSize, string label) {
        var t = chart.Options.Theme;
        var writer = new SvgMarkupWriter(384);
        writer
            .StartElement("text")
            .Attribute("data-cfx-role", "heatmap-scale-label")
            .Attribute("x", x)
            .Attribute("y", y)
            .Attribute("text-anchor", anchor)
            .Attribute("fill", t.MutedText.ToCss())
            .Attribute("font-family", SvgFontFamilyAttributeValue(t.FontFamily))
            .Attribute("font-size", fontSize)
            .Text(label)
            .EndElement()
            .Line();
        sb.Append(writer.Build());
    }

    private static void DrawHeatmapLabelConnector(StringBuilder sb, Chart chart, int rowIndex, int columnIndex, double startX, double y, double endX) {
        var writer = new SvgMarkupWriter(384);
        writer
            .StartElement("line")
            .Attribute("data-cfx-role", "data-label-connector")
            .Attribute("data-cfx-row", rowIndex)
            .Attribute("data-cfx-column", columnIndex)
            .Attribute("x1", startX)
            .Attribute("y1", y)
            .Attribute("x2", endX)
            .Attribute("y2", y)
            .Attribute("stroke", DataLabelConnectorColor(chart).ToCss())
            .Attribute("stroke-width", chart.Options.DataLabelConnectorStrokeWidth)
            .Attribute("stroke-opacity", chart.Options.DataLabelConnectorOpacity)
            .Attribute("stroke-linecap", "round")
            .EndEmptyElement()
            .Line();
        sb.Append(writer.Build());
    }

    private static double[] HeatmapColumns(IReadOnlyList<ChartSeries> rows) {
        var columns = new SortedSet<double>();
        foreach (var series in rows) {
            foreach (var point in series.Points) columns.Add(point.X);
            if (series.HeatmapColumnCount.HasValue) {
                for (var i = 1; i <= series.HeatmapColumnCount.Value; i++) columns.Add(i);
            }
        }

        return columns.ToArray();
    }

    private static double FindHeatmapValue(ChartSeries series, double column) {
        foreach (var point in series.Points) {
            if (Math.Abs(point.X - column) < 0.000001) return point.Y;
        }

        return 0;
    }

    private static int HeatmapPointIndex(ChartSeries series, double column) {
        for (var i = 0; i < series.Points.Count; i++) {
            if (Math.Abs(series.Points[i].X - column) < 0.000001) return i;
        }

        return -1;
    }
}
