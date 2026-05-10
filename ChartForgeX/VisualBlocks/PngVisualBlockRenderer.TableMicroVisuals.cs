using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.VisualBlocks;

public sealed partial class PngVisualBlockRenderer {
    private static void DrawTableCellMicroVisual(RgbaCanvas canvas, ChartTable table, ChartTableCell cell, double x, double y, double width, double height, int columnIndex) {
        var theme = table.Options.Theme;
        var color = cell.MicroVisualColor ?? VisualBlockRendering.PaletteAt(theme, columnIndex);
        var bounds = VisualBlockRendering.TableCellMicroVisualBounds(cell);
        if (cell.MicroVisualKind == ChartTableCellMicroVisualKind.MiniBars) {
            var metrics = VisualBlockRendering.FitRepeatedItems(cell.MicroVisualValues.Count, width, cell.MicroVisualValues.Count > 8 ? 2.0 : 3.0, 2);
            var gap = metrics.Gap;
            var barWidth = metrics.ItemWidth;
            for (var i = 0; i < cell.MicroVisualValues.Count; i++) {
                var ratio = MicroVisualRatio(cell.MicroVisualValues[i], bounds);
                var barHeight = Math.Max(2, height * ratio);
                canvas.FillRoundedRect(x + i * (barWidth + gap), y + height - barHeight, barWidth, barHeight, Math.Min(3, barWidth * 0.45), color.WithAlpha(i == cell.MicroVisualValues.Count - 1 ? (byte)230 : (byte)135));
            }
        } else if (cell.MicroVisualKind == ChartTableCellMicroVisualKind.Sparkline) {
            var points = new ChartPoint[cell.MicroVisualValues.Count];
            var step = width / Math.Max(1, cell.MicroVisualValues.Count - 1);
            for (var i = 0; i < cell.MicroVisualValues.Count; i++) {
                var ratio = MicroVisualRatio(cell.MicroVisualValues[i], bounds);
                points[i] = new ChartPoint(x + i * step, y + height - ratio * height);
            }

            canvas.DrawPolyline(points, color, 2);
        }
    }

    private static double MicroVisualRatio(double value, (double Minimum, double Maximum) bounds) =>
        Math.Max(0, Math.Min(1, (value - bounds.Minimum) / (bounds.Maximum - bounds.Minimum)));
}
