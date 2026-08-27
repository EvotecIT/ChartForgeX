using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawHeatmap(RgbaCanvas c, Chart chart, ChartRect plot) {
        var rows = new List<ChartSeries>();
        foreach (var series in chart.Series) if (series.Kind == ChartSeriesKind.Heatmap) rows.Add(series);
        if (rows.Count == 0) return;

        var columns = new SortedSet<double>();
        var min = double.PositiveInfinity;
        var max = double.NegativeInfinity;
        foreach (var series in rows) {
            foreach (var point in series.Points) {
                columns.Add(point.X);
                if (point.Y < min) min = point.Y;
                if (point.Y > max) max = point.Y;
            }

            AddHeatmapColumns(columns, series);
        }

        if (columns.Count == 0) return;
        if (double.IsInfinity(min)) { min = 0; max = 1; }
        if (Math.Abs(max - min) < 0.000001) max = min + 1;

        var columnValues = new List<double>();
        foreach (var column in columns) columnValues.Add(column);
        var tickFontSize = PngTickFontSize(chart);
        var tickStyle = chart.Options.TickLabelStyle;
        var tickHeight = EstimatePngStyledTextBoundsHeight(tickFontSize, tickStyle);
        var rawLabelWidth = 0.0;
        if (chart.Options.ShowAxes) foreach (var row in rows) rawLabelWidth = Math.Max(rawLabelWidth, EstimatePngStyledTextWidth(row.Name, tickFontSize, tickStyle, emphasized: true));
        var labelWidth = chart.Options.ShowAxes ? Math.Min(rawLabelWidth, Math.Max(0, plot.Width - 220)) : 0;
        var axisBottomBase = chart.Options.ShowHeatmapScale ? Math.Max(56, tickHeight + 44) : chart.Options.ShowHeatmapColumnLabels ? tickHeight + 24 : 10;
        var axesBottomReserve = chart.Options.ShowAxes ? axisBottomBase + (string.IsNullOrWhiteSpace(chart.XAxisTitle) ? 0 : EstimatePngStyledTextBoundsHeight(PngXAxisTitleFontSize(chart), chart.Options.AxisTitleStyle) + 8) : 0;
        var bottomReserve = chart.Options.ShowHeatmapScale ? Math.Max(axesBottomReserve, 56) : axesBottomReserve;
        var labelGap = chart.Options.ShowAxes ? 14 : 0;
        var rowLabelMaxWidth = Math.Max(8, labelWidth);
        var sideLabelWidth = HeatmapSideLabelWidth(chart, rows, columnValues);
        var leftLabelReserve = HasHeatmapSideLabels(chart, rows, ChartDataLabelPlacement.Left) ? sideLabelWidth + 22 : 0;
        var rightLabelReserve = HasHeatmapSideLabels(chart, rows, ChartDataLabelPlacement.Right) || HasHeatmapSideLabels(chart, rows, ChartDataLabelPlacement.Outside) ? sideLabelWidth + 22 : 0;
        var labelBounds = new ChartRect(plot.X + labelWidth + labelGap, plot.Y, Math.Max(1, plot.Width - labelWidth - labelGap), Math.Max(1, plot.Height - bottomReserve));
        plot = new ChartRect(labelBounds.X + leftLabelReserve, labelBounds.Y, Math.Max(1, labelBounds.Width - leftLabelReserve - rightLabelReserve), labelBounds.Height);
        var autoGap = Math.Min(6, Math.Max(2, Math.Min(plot.Width / columnValues.Count, plot.Height / rows.Count) * 0.05));
        var gap = VisualBlockRendering.EffectiveHeatmapGap(plot.Width, plot.Height, columnValues.Count, rows.Count, chart.Options.HeatmapCellGap ?? autoGap);
        var cellWidth = Math.Max(1, (plot.Width - gap * (columnValues.Count - 1)) / columnValues.Count);
        var cellHeight = Math.Max(1, (plot.Height - gap * (rows.Count - 1)) / rows.Count);
        var autoRadius = Math.Min(8, Math.Min(cellWidth, cellHeight) * 0.16);
        var radius = Math.Min(chart.Options.HeatmapCellRadius ?? autoRadius, Math.Min(cellWidth, cellHeight) / 2);

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++) {
            var series = rows[rowIndex];
            var y = plot.Top + rowIndex * (cellHeight + gap);
            if (chart.Options.ShowAxes) {
                var rowLabelFontSize = TextFontSizeForEmphasizedWidth(series.Name, rowLabelMaxWidth, tickFontSize, tickStyle);
                var rowLabel = TrimReadablePngLabelToWidth(series.Name, rowLabelFontSize, rowLabelMaxWidth, tickStyle);
                if (rowLabel.Length > 0) {
                    var rowHeight = EstimatePngStyledTextBoundsHeight(rowLabelFontSize, tickStyle);
                    DrawPngTextStyled(c, plot.Left - EstimatePngStyledTextWidth(rowLabel, rowLabelFontSize, tickStyle, emphasized: true) - 10, y + cellHeight / 2 - rowHeight / 2 - PngStyledTextTopExtent(rowLabelFontSize, tickStyle), rowLabel, tickStyle, chart.Options.Theme.MutedText, rowLabelFontSize, emphasized: true);
                }
            }
            for (var columnIndex = 0; columnIndex < columnValues.Count; columnIndex++) {
                var pointIndex = HeatmapPointIndex(series, columnValues[columnIndex]);
                if (pointIndex < 0) continue;
                var value = FindHeatmapValue(series, columnValues[columnIndex]);
                var x = plot.Left + columnIndex * (cellWidth + gap);
                var color = ChartHeatmapSurface.Color(chart, series.Color, value, min, max);
                c.FillRoundedRect(x, y, cellWidth, cellHeight, radius, color);
                c.StrokeRoundedRect(x, y, cellWidth, cellHeight, radius, ApplyOpacity(chart.Options.Theme.CardBackground, ChartVisualPrimitives.HeatmapCellBorderOpacity), ChartVisualPrimitives.HeatmapCellBorderStrokeWidth);
                var dataStyle = DataLabelStyle(chart, series, pointIndex);
                var dataFontSize = PngDataLabelFontSize(chart, series, pointIndex);
                var labelFits = cellWidth >= EstimatePngStyledTextWidth("100%", dataFontSize, dataStyle, emphasized: true) + 12 &&
                    cellHeight >= EstimatePngStyledTextBoundsHeight(dataFontSize, dataStyle) + 10;
                var drawValueText = chart.Options.HeatmapValueTextMode == ChartHeatmapValueTextMode.Always ||
                    chart.Options.HeatmapValueTextMode == ChartHeatmapValueTextMode.Auto && ShouldDrawDataLabels(chart, series) && labelFits;
                if (drawValueText) {
                    var label = FormatDataLabel(chart, series, pointIndex, value);
                    var placement = DataLabelPlacement(chart, series);
                    if (placement == ChartDataLabelPlacement.Auto || placement == ChartDataLabelPlacement.Inside || placement == ChartDataLabelPlacement.Center) {
                        DrawReadablePngLabelCentered(c, new ChartRect(x, y, cellWidth, cellHeight), label, ChartColorMath.TextOnBackground(color), color, dataFontSize, dataStyle);
                    } else {
                        var heatmapLabelWidth = EstimatePngStyledTextWidth(label, dataFontSize, dataStyle, emphasized: true);
                        var heatmapLabelHeight = EstimatePngStyledTextBoundsHeight(dataFontSize, dataStyle);
                        var labelX = placement == ChartDataLabelPlacement.Left
                            ? x - heatmapLabelWidth - 8
                            : placement == ChartDataLabelPlacement.Right || placement == ChartDataLabelPlacement.Outside
                                ? x + cellWidth + 8
                                : x + cellWidth / 2 - heatmapLabelWidth / 2.0;
                        var labelY = placement == ChartDataLabelPlacement.Above ? y - heatmapLabelHeight - 4 : placement == ChartDataLabelPlacement.Below ? y + cellHeight + 4 : y + cellHeight / 2 - heatmapLabelHeight / 2.0;
                        if (placement == ChartDataLabelPlacement.Left || placement == ChartDataLabelPlacement.Right || placement == ChartDataLabelPlacement.Outside) {
                            var connectorStartX = placement == ChartDataLabelPlacement.Left ? x : x + cellWidth;
                            var connectorEndX = placement == ChartDataLabelPlacement.Left ? x - 5 : x + cellWidth + 5;
                            c.DrawLine(connectorStartX, y + cellHeight / 2.0, connectorEndX, y + cellHeight / 2.0, ApplyOpacity(DataLabelConnectorColor(chart), chart.Options.DataLabelConnectorOpacity), chart.Options.DataLabelConnectorStrokeWidth);
                        }
                        DrawReadablePngLabel(c, labelBounds, labelX, labelY - PngStyledTextTopExtent(dataFontSize, dataStyle), label, chart.Options.Theme.Text, ReadableLabelHalo(chart), dataFontSize, dataStyle);
                    }
                }
            }
        }

        if (chart.Options.ShowAxes && chart.Options.ShowHeatmapColumnLabels) {
            for (var columnIndex = 0; columnIndex < columnValues.Count; columnIndex++) {
                var label = FormatX(chart, columnValues[columnIndex]);
                var columnLabelWidth = Math.Max(8, cellWidth + gap);
                var columnLabelFontSize = TextFontSizeForEmphasizedWidth(label, columnLabelWidth, tickFontSize, tickStyle);
                label = TrimReadablePngLabelToWidth(label, columnLabelFontSize, columnLabelWidth, tickStyle);
                if (label.Length == 0) continue;
                var width = EstimatePngStyledTextWidth(label, columnLabelFontSize, tickStyle, emphasized: true);
                var x = Clamp(plot.Left + columnIndex * (cellWidth + gap) + cellWidth / 2 - width / 2.0, plot.Left + 2, plot.Right - width - 2);
                DrawPngTextStyled(c, x, plot.Bottom + 21 - EstimatePngStyledTextHeight(columnLabelFontSize, tickStyle) + 1, label, tickStyle, chart.Options.Theme.MutedText, columnLabelFontSize, emphasized: true);
            }

            DrawDetailAxisTitles(c, chart, plot, DetailTextScale(chart));
        }
        if (chart.Options.ShowHeatmapScale) DrawHeatmapScale(c, chart, plot, min, max, rows[0].Color, tickFontSize);
    }

    private static bool IsHeatmapChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.Heatmap);

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
                var fontSize = PngDataLabelFontSize(chart, row, pointIndex);
                var style = DataLabelStyle(chart, row, pointIndex);
                max = Math.Max(max, EstimatePngStyledTextWidth(FormatDataLabel(chart, row, pointIndex, FindHeatmapValue(row, columns[i])), fontSize, style, emphasized: true));
            }
        }

        return Math.Min(88, max);
    }

    private static bool ShouldReserveHeatmapValueLabels(Chart chart, ChartSeries series) {
        if (chart.Options.HeatmapValueTextMode == ChartHeatmapValueTextMode.Hidden) return false;
        return chart.Options.HeatmapValueTextMode == ChartHeatmapValueTextMode.Always || ShouldDrawDataLabels(chart, series);
    }

    private static void AddHeatmapColumns(SortedSet<double> columns, ChartSeries series) {
        if (!series.HeatmapColumnCount.HasValue) return;
        for (var i = 1; i <= series.HeatmapColumnCount.Value; i++) columns.Add(i);
    }

    private static void DrawHeatmapScale(RgbaCanvas c, Chart chart, ChartRect plot, double min, double max, ChartColor? highColor, double fontSize) {
        const int steps = ChartVisualPrimitives.HeatmapScaleSteps;
        const double width = ChartVisualPrimitives.HeatmapScaleWidth;
        const double height = ChartVisualPrimitives.HeatmapScaleHeight;
        var x = plot.Right - width;
        var y = plot.Bottom + ChartVisualPrimitives.HeatmapScaleOffsetY;
        var stepWidth = width / steps;

        for (var i = 0; i < steps; i++) {
            var ratio = i / (double)(steps - 1);
            var value = min + (max - min) * ratio;
            c.FillRoundedRect(x + i * stepWidth, y, stepWidth + ChartVisualPrimitives.HeatmapScaleStepOverlap, height, ChartVisualPrimitives.HeatmapScaleRadius, ChartHeatmapSurface.Color(chart, highColor, value, min, max));
        }

        var labelMaxWidth = Math.Max(18, width * 0.46);
        var minLabel = FormatValue(chart, min);
        var style = chart.Options.TickLabelStyle;
        var minFontSize = TextFontSizeForWidth(minLabel, labelMaxWidth, fontSize, style);
        minLabel = TrimPngLabelToWidth(minLabel, minFontSize, labelMaxWidth, style);
        var maxLabel = FormatValue(chart, max);
        var maxFontSize = TextFontSizeForWidth(maxLabel, labelMaxWidth, fontSize, style);
        maxLabel = TrimPngLabelToWidth(maxLabel, maxFontSize, labelMaxWidth, style);
        if (minLabel.Length > 0) DrawPngTextStyled(c, x, y + ChartVisualPrimitives.HeatmapScaleLabelOffsetY - EstimatePngStyledTextHeight(minFontSize, style) + 1, minLabel, style, chart.Options.Theme.MutedText, minFontSize, emphasized: false);
        if (maxLabel.Length > 0) DrawPngTextStyled(c, x + width - EstimatePngStyledTextWidth(maxLabel, maxFontSize, style, emphasized: false), y + ChartVisualPrimitives.HeatmapScaleLabelOffsetY - EstimatePngStyledTextHeight(maxFontSize, style) + 1, maxLabel, style, chart.Options.Theme.MutedText, maxFontSize, emphasized: false);
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
