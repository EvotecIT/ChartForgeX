using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgVisualBlockRenderer {
    private static void RenderTableCellMicroVisual(SvgMarkupWriter writer, ChartTable table, ChartTableCell cell, double x, double y, double width, double height, int columnIndex) {
        var theme = table.Options.Theme;
        var color = cell.MicroVisualColor ?? VisualBlockRendering.PaletteAt(theme, columnIndex);
        var bounds = VisualBlockRendering.TableCellMicroVisualBounds(cell);
        writer.StartElement("g")
            .Attribute("data-cfx-role", "table-cell-microvisual")
            .Attribute("data-cfx-kind", cell.MicroVisualKind.ToString())
            .Attribute("data-cfx-values", cell.MicroVisualValues.Count)
            .EndStartElement().Line();
        if (cell.MicroVisualKind == ChartTableCellMicroVisualKind.MiniBars) {
            var metrics = VisualBlockRendering.FitRepeatedItems(cell.MicroVisualValues.Count, width, cell.MicroVisualValues.Count > 8 ? 2.0 : 3.0, 2);
            var gap = metrics.Gap;
            var barWidth = metrics.ItemWidth;
            for (var i = 0; i < cell.MicroVisualValues.Count; i++) {
                var ratio = MicroVisualRatio(cell.MicroVisualValues[i], bounds);
                var barHeight = Math.Max(2, height * ratio);
                writer.StartElement("rect")
                    .Attribute("data-cfx-role", "table-cell-mini-bar")
                    .Attribute("x", x + i * (barWidth + gap))
                    .Attribute("y", y + height - barHeight)
                    .Attribute("width", barWidth)
                    .Attribute("height", barHeight)
                    .Attribute("rx", Math.Min(3, barWidth * 0.45))
                    .Attribute("fill", color.WithAlpha(i == cell.MicroVisualValues.Count - 1 ? (byte)230 : (byte)135).ToCss())
                    .EndEmptyElement().Line();
            }
        } else if (cell.MicroVisualKind == ChartTableCellMicroVisualKind.Sparkline) {
            var points = new string[cell.MicroVisualValues.Count];
            var step = width / Math.Max(1, cell.MicroVisualValues.Count - 1);
            for (var i = 0; i < cell.MicroVisualValues.Count; i++) {
                var ratio = MicroVisualRatio(cell.MicroVisualValues[i], bounds);
                points[i] = FormatPoint(x + i * step, y + height - ratio * height);
            }

            writer.StartElement("polyline")
                .Attribute("data-cfx-role", "table-cell-sparkline")
                .Attribute("points", string.Join(" ", points))
                .Attribute("fill", "none")
                .Attribute("stroke", color.ToCss())
                .Attribute("stroke-width", 2)
                .Attribute("stroke-linecap", "round")
                .Attribute("stroke-linejoin", "round")
                .EndEmptyElement().Line();
        }

        writer.EndElement().Line();
    }

    private static double MicroVisualRatio(double value, (double Minimum, double Maximum) bounds) =>
        Math.Max(0, Math.Min(1, (value - bounds.Minimum) / (bounds.Maximum - bounds.Minimum)));
}
