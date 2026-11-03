using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Terminal;

namespace ChartForgeX.Topology;

public sealed partial class TopologyCellLayout {
    private void PaintCellLabels(TopologyCellCanvas canvas, TopologyCellPaintOptions options, bool ascii, bool highlighting, HashSet<TopologyCellPoint> crossings) {
        foreach (var edge in Edges.OrderByDescending(edge => CellLabelPriority(edge.Status))) {
            if (edge.LabelPosition == null || string.IsNullOrWhiteSpace(edge.Label)) continue;
            var text = " " + FitCellText(edge.Label!.Trim(), Columns, ascii) + " ";
            var width = TerminalTextWidth.Measure(text);
            var emphasis = EdgeEmphasis(edge, options, highlighting);
            var placed = false;
            foreach (var i in Enumerable.Range(0, edge.Points.Count - 1).OrderByDescending(i => Math.Abs(edge.Points[i + 1].Column - edge.Points[i].Column))) {
                var a = edge.Points[i]; var b = edge.Points[i + 1];
                if (a.Row != b.Row) continue;
                var at = new TopologyCellPoint((a.Column + b.Column - width) / 2, a.Row);
                if (LineLabelStart(canvas, edge, at, width, crossings) is not int start) continue;
                WriteText(canvas, start, a.Row, text, start + width, TopologyCellRole.EdgeLabel, edge.Id, edge.Status, emphasis);
                placed = true; break;
            }
            if (placed) continue;
            foreach (var i in Enumerable.Range(0, edge.Points.Count - 1)) {
                var a = edge.Points[i]; var b = edge.Points[i + 1];
                if (a.Row != b.Row) continue;
                var left = Math.Min(a.Column, b.Column) + 1;
                var right = Math.Max(a.Column, b.Column) - 1;
                // Keep the caption's center over its run. Its ends can extend past a
                // bend when the adjacent row is clear, instead of dropping the label.
                foreach (var start in Enumerable.Range(left - width / 2, Math.Max(0, right - left + 1))
                    .OrderBy(start => Math.Abs(start + width / 2 - (left + right) / 2))) foreach (var row in new[] { a.Row - 1, a.Row + 1 }) {
                    if (placed) break;
                    if (!CanWrite(canvas, start, row, start + width, false)) continue;
                    WriteText(canvas, start, row, text, start + width, TopologyCellRole.EdgeLabel, edge.Id, edge.Status, emphasis);
                    placed = true; break;
                }
                if (placed) break;
            }
            if (placed) continue;
            // A vertical run can carry a caption beside it. Reserve one blank cell between
            // the caption and the path so no glyph reads as part of a label or a junction.
            foreach (var point in CellPath(edge.Points).OrderBy(point => Math.Abs(point.Row - edge.Points[edge.Points.Count / 2].Row))) {
                var cell = canvas.Get(point.Column, point.Row);
                if (cell.Role != TopologyCellRole.Edge || cell.ElementId != edge.Id || cell.Glyph is not ("│" or "┃" or "┆" or "|" or "#" or ":")) continue;
                foreach (var start in new[] { point.Column + 2, point.Column - width - 1 }) {
                    if (!CanWrite(canvas, start, point.Row, start + width, false)) continue;
                    WriteText(canvas, start, point.Row, text, start + width, TopologyCellRole.EdgeLabel, edge.Id, edge.Status, emphasis);
                    placed = true; break;
                }
                if (placed) break;
            }
        }
    }
}
