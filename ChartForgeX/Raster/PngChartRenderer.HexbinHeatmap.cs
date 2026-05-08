using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawHexbinHeatmap(RgbaCanvas c, Chart chart, ChartRect basePlot) {
        var rows = new List<ChartSeries>();
        foreach (var series in chart.Series) if (series.Kind == ChartSeriesKind.HexbinHeatmap) rows.Add(series);
        if (rows.Count == 0) return;

        var columns = new SortedSet<double>();
        var min = double.PositiveInfinity;
        var max = double.NegativeInfinity;
        foreach (var series in rows) {
            foreach (var point in series.Points) {
                columns.Add(point.X);
                min = Math.Min(min, point.Y);
                max = Math.Max(max, point.Y);
            }

            AddHeatmapColumns(columns, series);
        }

        if (columns.Count == 0) return;
        if (double.IsInfinity(min)) { min = 0; max = 1; }
        if (Math.Abs(max - min) < 0.000001) max = min + 1;

        var columnValues = new List<double>();
        foreach (var column in columns) columnValues.Add(column);
        var plot = ApplyHexbinHeatmapReserve(chart, basePlot, rows);
        var layout = ChartHexbinLayout.Build(plot, rows.Count, columnValues.Count);
        var tickFontSize = PngTickFontSize(chart);

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++) {
            var series = rows[rowIndex];
            var cy = layout.Top + layout.Radius + rowIndex * layout.RowStep;
            if (chart.Options.ShowAxes) DrawHexbinPngLabel(c, chart, plot.Left - 12, cy, series.Name, tickFontSize, true);

            for (var columnIndex = 0; columnIndex < columnValues.Count; columnIndex++) {
                var pointIndex = HeatmapPointIndex(series, columnValues[columnIndex]);
                if (pointIndex < 0) continue;
                var value = FindHeatmapValue(series, columnValues[columnIndex]);
                var cx = layout.Left + layout.HexWidth / 2 + columnIndex * layout.ColumnStep + (rowIndex % 2) * layout.HexWidth / 2;
                var color = HeatmapColor(chart, series.Color, value, min, max);
                var points = ChartHexbinLayout.Points(cx, cy, layout.Radius);
                c.FillPolygon(points, color);
                DrawPolygonOutline(c, points, ApplyOpacity(chart.Options.Theme.CardBackground, ChartVisualPrimitives.HeatmapCellBorderOpacity), Math.Max(1, ChartVisualPrimitives.HeatmapCellBorderStrokeWidth + 0.8));

                var fontSize = PngDataLabelFontSize(chart, series, pointIndex);
                if (ShouldDrawDataLabels(chart, series) && layout.Radius >= 16) {
                    var label = FormatDataLabel(chart, series, pointIndex, value);
                    var width = EstimatePngEmphasizedTextWidth(label, fontSize);
                    DrawReadablePngLabel(c, new ChartRect(cx - layout.HexWidth / 2, cy - layout.Radius, layout.HexWidth, layout.Radius * 2), cx - width / 2, cy - fontSize / 2, label, HeatmapTextColor(color), color, fontSize, DataLabelStyle(chart, series, pointIndex));
                }
            }
        }

        if (chart.Options.ShowAxes && chart.Options.ShowHeatmapColumnLabels) {
            var columnLabelFontSize = Math.Min(tickFontSize, Math.Max(9, layout.ColumnStep * 0.32));
            var columnLabelWidth = Math.Max(20, layout.ColumnStep * 0.82);
            for (var columnIndex = 0; columnIndex < columnValues.Count; columnIndex++) {
                var cx = layout.Left + layout.HexWidth / 2 + columnIndex * layout.ColumnStep;
                DrawHexbinPngLabel(c, chart, cx, plot.Bottom + 22, FormatX(chart, columnValues[columnIndex]), columnLabelFontSize, false, columnLabelWidth);
            }

            DrawDetailAxisTitles(c, chart, plot, DetailTextScale(chart));
        }

        if (chart.Options.ShowHeatmapScale) DrawHeatmapScale(c, chart, plot, min, max, rows[0].Color, tickFontSize);
    }

    private static ChartRect ApplyHexbinHeatmapReserve(Chart chart, ChartRect plot, IReadOnlyList<ChartSeries> rows) {
        var tickFontSize = PngTickFontSize(chart);
        var rowLabelReserve = 0.0;
        if (chart.Options.ShowAxes) foreach (var row in rows) rowLabelReserve = Math.Max(rowLabelReserve, EstimatePngEmphasizedTextWidth(row.Name, tickFontSize));
        var bottomReserve = chart.Options.ShowAxes ? (chart.Options.ShowHeatmapScale ? 58 : chart.Options.ShowHeatmapColumnLabels ? 38 : 10) : chart.Options.ShowHeatmapScale ? 46 : 0;
        var desiredLeft = plot.Left + rowLabelReserve + (chart.Options.ShowAxes ? 18 : 0);
        var maxLeft = Math.Max(plot.Left, plot.Right - 160);
        var left = Math.Max(plot.Left, Math.Min(maxLeft, desiredLeft));
        return new ChartRect(left, plot.Top, Math.Max(1, plot.Right - left), Math.Max(1, plot.Height - bottomReserve));
    }

    private static void DrawHexbinPngLabel(RgbaCanvas c, Chart chart, double x, double y, string text, double fontSize, bool rightAligned, double? maxLabelWidth = null) {
        var maxWidth = maxLabelWidth ?? (rightAligned ? Math.Max(24, x - 8) : 84);
        var label = TrimReadablePngLabelToWidth(text, fontSize, maxWidth);
        if (label.Length == 0) return;
        var width = EstimatePngEmphasizedTextWidth(label, fontSize);
        c.DrawTextEmphasized(rightAligned ? x - width : x - width / 2, y - fontSize / 2, label, chart.Options.Theme.MutedText, fontSize);
    }

    private static void DrawPolygonOutline(RgbaCanvas c, IReadOnlyList<ChartPoint> points, ChartColor color, double thickness) {
        for (var i = 0; i < points.Count; i++) {
            var next = i == points.Count - 1 ? points[0] : points[i + 1];
            c.DrawLine(points[i].X, points[i].Y, next.X, next.Y, color, thickness);
        }
    }

}
