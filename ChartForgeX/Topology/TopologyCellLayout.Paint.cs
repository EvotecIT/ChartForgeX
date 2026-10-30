using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Terminal;

namespace ChartForgeX.Topology;

public sealed partial class TopologyCellLayout {
    private const int North = 1, East = 2, South = 4, West = 8;

    /// <summary>Paints the layout into a character grid. Painting is cheap, so hosts repaint for every step, focus change, or live value.</summary>
    /// <param name="options">Optional paint options.</param>
    /// <returns>The painted canvas.</returns>
    public TopologyCellCanvas Paint(TopologyCellPaintOptions? options = null) {
        options ??= new TopologyCellPaintOptions();
        var ascii = options.Glyphs == TopologyCellGlyphSet.Ascii;
        var highlighting = options.HighlightNodeIds.Count > 0 || options.HighlightEdgeIds.Count > 0;
        var legend = options.IncludeLegend ? LegendItems(options, highlighting, ascii) : new List<(string Marker, string Text, TopologyHealthStatus Status, TopologyCellEmphasis Emphasis)>();
        var legendRows = LegendRows(legend, Columns);
        var canvas = new TopologyCellCanvas(Columns, Rows + (legendRows.Count == 0 ? 0 : legendRows.Count + 1));
        foreach (var group in Groups) PaintGroup(canvas, group, ascii, highlighting && options.DimOthers);
        PaintEdges(canvas, options, ascii, highlighting);
        foreach (var node in Nodes) PaintNode(canvas, node, options, ascii, highlighting);
        PaintLegend(canvas, legendRows, Rows + 1);
        return canvas;
    }

    private void PaintGroup(TopologyCellCanvas canvas, TopologyCellGroup group, bool ascii, bool dimmed) {
        var emphasis = dimmed ? TopologyCellEmphasis.Dimmed : TopologyCellEmphasis.Normal;
        var right = group.Column + group.Width - 1;
        var bottom = group.Row + group.Height - 1;
        var horizontal = ascii ? "." : "╌";
        var vertical = ascii ? ":" : "╎";
        for (var column = group.Column + 1; column < right; column++) {
            canvas.Set(column, group.Row, Cell(horizontal, TopologyCellRole.GroupBorder, group.Id, group.Status, emphasis));
            canvas.Set(column, bottom, Cell(horizontal, TopologyCellRole.GroupBorder, group.Id, group.Status, emphasis));
        }

        for (var row = group.Row + 1; row < bottom; row++) {
            canvas.Set(group.Column, row, Cell(vertical, TopologyCellRole.GroupBorder, group.Id, group.Status, emphasis));
            canvas.Set(right, row, Cell(vertical, TopologyCellRole.GroupBorder, group.Id, group.Status, emphasis));
        }

        canvas.Set(group.Column, group.Row, Cell(ascii ? "." : "╭", TopologyCellRole.GroupBorder, group.Id, group.Status, emphasis));
        canvas.Set(right, group.Row, Cell(ascii ? "." : "╮", TopologyCellRole.GroupBorder, group.Id, group.Status, emphasis));
        canvas.Set(group.Column, bottom, Cell(ascii ? "'" : "╰", TopologyCellRole.GroupBorder, group.Id, group.Status, emphasis));
        canvas.Set(right, bottom, Cell(ascii ? "'" : "╯", TopologyCellRole.GroupBorder, group.Id, group.Status, emphasis));
        WriteText(canvas, group.Column + 2, group.Row, " " + TerminalTextWidth.Fit(group.Label, Math.Max(1, group.Width - 6)) + " ", group.Column + group.Width - 2,
            TopologyCellRole.GroupLabel, group.Id, group.Status, emphasis);
    }

    private void PaintEdges(TopologyCellCanvas canvas, TopologyCellPaintOptions options, bool ascii, bool highlighting) {
        var masks = new Dictionary<TopologyCellPoint, EdgeCell>();
        var arrows = new List<(TopologyCellPoint At, int Direction, TopologyCellEdge Edge, TopologyCellEmphasis Emphasis)>();
        foreach (var edge in Edges) {
            var emphasis = EdgeEmphasis(edge, options, highlighting);
            var path = CellPath(edge.Points);
            for (var i = 0; i < path.Count; i++) {
                var cell = path[i];
                if (InsideNode(cell)) continue;
                var bits = 0;
                if (i > 0) bits |= DirectionBetween(cell, path[i - 1]);
                if (i + 1 < path.Count) bits |= DirectionBetween(cell, path[i + 1]);
                masks.TryGetValue(cell, out var current);
                masks[cell] = current.Add(bits, edge, emphasis);
            }

            var highlighted = options.HighlightEdgeIds.Contains(edge.Id);
            var reversed = highlighted && options.ReversedEdgeIds.Contains(edge.Id);
            var toTarget = highlighted ? !reversed : edge.Direction == VisualLinkDirection.Forward || edge.Direction == VisualLinkDirection.Bidirectional;
            var toSource = highlighted ? reversed : edge.Direction == VisualLinkDirection.Backward || edge.Direction == VisualLinkDirection.Bidirectional;
            if (toTarget && ArrowAt(path, fromEnd: true, out var targetArrow, out var targetDirection)) arrows.Add((targetArrow, targetDirection, edge, emphasis));
            if (toSource && ArrowAt(path, fromEnd: false, out var sourceArrow, out var sourceDirection)) arrows.Add((sourceArrow, sourceDirection, edge, emphasis));
        }

        foreach (var item in masks) {
            var edgeCell = item.Value;
            canvas.Set(item.Key.Column, item.Key.Row, Cell(LineGlyph(edgeCell.Bits, edgeCell.Heavy, edgeCell.AllDashed, ascii), TopologyCellRole.Edge, edgeCell.Edge!.Id, edgeCell.Edge.Status, edgeCell.Emphasis));
        }

        foreach (var arrow in arrows.OrderBy(item => item.Emphasis == TopologyCellEmphasis.Dimmed ? 0 : 1)) {
            canvas.Set(arrow.At.Column, arrow.At.Row, Cell(ArrowGlyph(arrow.Direction, ascii), TopologyCellRole.EdgeArrow, arrow.Edge.Id, arrow.Edge.Status, arrow.Emphasis));
        }

        foreach (var edge in Edges) {
            if (edge.LabelPosition == null || string.IsNullOrWhiteSpace(edge.Label)) continue;
            var emphasis = EdgeEmphasis(edge, options, highlighting);
            var at = edge.LabelPosition.Value;
            var text = " " + edge.Label!.Trim() + " ";
            if (edge.LabelOnLine) {
                var limit = at.Column + TerminalTextWidth.Measure(text);
                if (CanWrite(canvas, at.Column, at.Row, limit, allowEdges: true)) WriteText(canvas, at.Column, at.Row, text, limit, TopologyCellRole.EdgeLabel, edge.Id, edge.Status, emphasis);
            } else {
                var fitted = TerminalTextWidth.Fit(edge.Label!.Trim(), Math.Max(0, Columns - at.Column));
                var limit = at.Column + TerminalTextWidth.Measure(fitted);
                if (fitted.Length > 0 && CanWrite(canvas, at.Column, at.Row, limit, allowEdges: false)) WriteText(canvas, at.Column, at.Row, fitted, limit, TopologyCellRole.EdgeLabel, edge.Id, edge.Status, emphasis);
            }
        }
    }

    private void PaintNode(TopologyCellCanvas canvas, TopologyCellNode node, TopologyCellPaintOptions options, bool ascii, bool highlighting) {
        var status = options.NodeStatuses.TryGetValue(node.Id, out var live) ? live : node.Status;
        var emphasis = string.Equals(options.FocusNodeId, node.Id, StringComparison.Ordinal) ? TopologyCellEmphasis.Focused
            : options.HighlightNodeIds.Contains(node.Id) ? TopologyCellEmphasis.Highlighted
            : highlighting && options.DimOthers ? TopologyCellEmphasis.Dimmed : TopologyCellEmphasis.Normal;
        var box = BoxGlyphs(emphasis, ascii);
        var right = node.Column + node.Width - 1;
        var bottom = node.Row + node.Height - 1;
        for (var row = node.Row; row <= bottom; row++) {
            for (var column = node.Column; column <= right; column++) {
                var edgeRow = row == node.Row || row == bottom;
                var edgeColumn = column == node.Column || column == right;
                var glyph = edgeRow && edgeColumn
                    ? row == node.Row ? (column == node.Column ? box[0] : box[1]) : (column == node.Column ? box[2] : box[3])
                    : edgeRow ? box[4] : edgeColumn ? box[5] : " ";
                canvas.Set(column, row, Cell(glyph, edgeRow || edgeColumn ? TopologyCellRole.NodeBorder : TopologyCellRole.NodeFill, node.Id, status, emphasis));
            }
        }

        var marker = StatusGlyph(status, ascii);
        var labelWidth = Math.Max(1, node.Width - (marker.Length > 0 ? 5 : 4));
        var label = TerminalTextWidth.Fit(node.Label, labelWidth);
        var start = node.Column + 2 + Math.Max(0, (labelWidth - TerminalTextWidth.Measure(label)) / 2);
        if (node.Height >= 3) WriteText(canvas, start, node.Row + 1, label, node.Column + 2 + labelWidth, TopologyCellRole.NodeLabel, node.Id, status, emphasis);
        if (marker.Length > 0 && node.Height >= 3) canvas.Set(right - 1, node.Row + 1, Cell(marker, TopologyCellRole.NodeStatus, node.Id, status, emphasis));
        var detail = options.NodeValues.TryGetValue(node.Id, out var value) ? value : node.Subtitle;
        if (node.Height >= 4 && !string.IsNullOrWhiteSpace(detail)) {
            var width = Math.Max(1, node.Width - 4);
            var fitted = TerminalTextWidth.Fit(detail!.Trim(), width);
            WriteText(canvas, node.Column + 2 + Math.Max(0, (width - TerminalTextWidth.Measure(fitted)) / 2), node.Row + 2, fitted, node.Column + 2 + width, TopologyCellRole.NodeDetail, node.Id, status, emphasis);
        }
    }

    private List<(string Marker, string Text, TopologyHealthStatus Status, TopologyCellEmphasis Emphasis)> LegendItems(TopologyCellPaintOptions options, bool highlighting, bool ascii) {
        var items = new List<(string, string, TopologyHealthStatus, TopologyCellEmphasis)>();
        var statuses = new HashSet<TopologyHealthStatus>(Nodes.Select(node => options.NodeStatuses.TryGetValue(node.Id, out var live) ? live : node.Status));
        foreach (var status in new[] { TopologyHealthStatus.Healthy, TopologyHealthStatus.Warning, TopologyHealthStatus.Critical, TopologyHealthStatus.Disabled }) {
            if (statuses.Contains(status)) items.Add((StatusGlyph(status, ascii), status.ToString(), status, TopologyCellEmphasis.Normal));
        }

        if (highlighting) items.Add((ascii ? "==>" : "━━▶", "active step", TopologyHealthStatus.Unknown, TopologyCellEmphasis.Highlighted));
        if (options.FailedEdgeIds.Count > 0) items.Add((ascii ? "=x=" : "━━✕", "failed", TopologyHealthStatus.Critical, TopologyCellEmphasis.Failed));
        return items;
    }

    private static List<List<(string Marker, string Text, TopologyHealthStatus Status, TopologyCellEmphasis Emphasis)>> LegendRows(
        List<(string Marker, string Text, TopologyHealthStatus Status, TopologyCellEmphasis Emphasis)> items, int columns) {
        var rows = new List<List<(string, string, TopologyHealthStatus, TopologyCellEmphasis)>>();
        var width = 0;
        foreach (var item in items) {
            var itemWidth = TerminalTextWidth.Measure(item.Marker) + 1 + TerminalTextWidth.Measure(item.Text) + 3;
            if (rows.Count == 0 || width + itemWidth > columns && width > 0) { rows.Add(new List<(string, string, TopologyHealthStatus, TopologyCellEmphasis)>()); width = 0; }
            rows[rows.Count - 1].Add(item);
            width += itemWidth;
        }

        return rows;
    }

    private static void PaintLegend(TopologyCellCanvas canvas, List<List<(string Marker, string Text, TopologyHealthStatus Status, TopologyCellEmphasis Emphasis)>> rows, int firstRow) {
        for (var index = 0; index < rows.Count; index++) {
            var column = 0;
            foreach (var item in rows[index]) {
                column = WriteText(canvas, column, firstRow + index, item.Marker, canvas.Columns, TopologyCellRole.LegendMarker, null, item.Status, item.Emphasis);
                column = WriteText(canvas, column + 1, firstRow + index, item.Text, canvas.Columns, TopologyCellRole.LegendText, null, item.Status, TopologyCellEmphasis.Normal) + 3;
            }
        }
    }

    private bool InsideNode(TopologyCellPoint cell) => Nodes.Any(node => node.Contains(cell.Column, cell.Row));

    private static bool CanWrite(TopologyCellCanvas canvas, int column, int row, int limit, bool allowEdges) {
        if (row < 0 || row >= canvas.Rows || column < 0 || limit > canvas.Columns) return false;
        for (var c = column; c < limit; c++) {
            var role = canvas.Get(c, row).Role;
            if (role == TopologyCellRole.Empty) continue;
            if (allowEdges && role == TopologyCellRole.Edge) continue;
            return false;
        }

        return true;
    }

    private static TopologyCellEmphasis EdgeEmphasis(TopologyCellEdge edge, TopologyCellPaintOptions options, bool highlighting) =>
        options.FailedEdgeIds.Contains(edge.Id) ? TopologyCellEmphasis.Failed
        : options.HighlightEdgeIds.Contains(edge.Id) ? TopologyCellEmphasis.Highlighted
        : highlighting && options.DimOthers ? TopologyCellEmphasis.Dimmed : TopologyCellEmphasis.Normal;

    private static List<TopologyCellPoint> CellPath(IReadOnlyList<TopologyCellPoint> points) {
        var path = new List<TopologyCellPoint> { points[0] };
        for (var i = 1; i < points.Count; i++) {
            var from = points[i - 1];
            var to = points[i];
            var dc = Math.Sign(to.Column - from.Column);
            var dr = Math.Sign(to.Row - from.Row);
            var steps = Math.Max(Math.Abs(to.Column - from.Column), Math.Abs(to.Row - from.Row));
            for (var step = 1; step <= steps; step++) path.Add(new TopologyCellPoint(from.Column + dc * step, from.Row + dr * step));
        }

        return path;
    }

    private bool ArrowAt(List<TopologyCellPoint> path, bool fromEnd, out TopologyCellPoint at, out int direction) {
        for (var offset = 0; offset < path.Count; offset++) {
            var index = fromEnd ? path.Count - 1 - offset : offset;
            if (InsideNode(path[index])) continue;
            var next = fromEnd ? index + 1 : index - 1;
            if (next < 0 || next >= path.Count) break;
            at = path[index];
            direction = DirectionBetween(path[index], path[next]);
            return direction != 0;
        }

        at = default;
        direction = 0;
        return false;
    }

    private static int DirectionBetween(TopologyCellPoint from, TopologyCellPoint to) =>
        to.Row < from.Row ? North : to.Row > from.Row ? South : to.Column > from.Column ? East : to.Column < from.Column ? West : 0;

    private static string LineGlyph(int bits, bool heavy, bool dashed, bool ascii) {
        if ((bits & (North | South)) != 0 && (bits & (East | West)) == 0) return ascii ? (heavy ? "#" : dashed ? ":" : "|") : heavy ? "┃" : dashed ? "┆" : "│";
        if ((bits & (East | West)) != 0 && (bits & (North | South)) == 0) return ascii ? (heavy ? "=" : dashed ? "." : "-") : heavy ? "━" : dashed ? "┄" : "─";
        if (ascii) return heavy ? "#" : "+";
        const string light = "   └ │┌├ ┘─┴┐┤┬┼";
        const string bold = "   ┗ ┃┏┣ ┛━┻┓┫┳╋";
        var glyph = (heavy ? bold : light)[bits & 15];
        return glyph == ' ' ? (heavy ? "╋" : "┼") : glyph.ToString();
    }

    private static string ArrowGlyph(int direction, bool ascii) => direction switch {
        North => ascii ? "^" : "▲",
        South => ascii ? "v" : "▼",
        West => ascii ? "<" : "◀",
        _ => ascii ? ">" : "▶"
    };

    private static string StatusGlyph(TopologyHealthStatus status, bool ascii) => status switch {
        TopologyHealthStatus.Healthy => ascii ? "+" : "●",
        TopologyHealthStatus.Warning => ascii ? "!" : "▲",
        TopologyHealthStatus.Critical => ascii ? "x" : "■",
        TopologyHealthStatus.Disabled => ascii ? "-" : "○",
        _ => string.Empty
    };

    private static string[] BoxGlyphs(TopologyCellEmphasis emphasis, bool ascii) => emphasis switch {
        TopologyCellEmphasis.Focused => ascii ? new[] { "*", "*", "*", "*", "=", "#" } : new[] { "╔", "╗", "╚", "╝", "═", "║" },
        TopologyCellEmphasis.Highlighted => ascii ? new[] { "#", "#", "#", "#", "=", "#" } : new[] { "┏", "┓", "┗", "┛", "━", "┃" },
        _ => ascii ? new[] { "+", "+", "+", "+", "-", "|" } : new[] { "┌", "┐", "└", "┘", "─", "│" }
    };

    private static TopologyCell Cell(string glyph, TopologyCellRole role, string? id, TopologyHealthStatus status, TopologyCellEmphasis emphasis) =>
        new(glyph, 1, role, id, status, emphasis);

    /// <summary>Writes text cell by cell up to <paramref name="limit"/> and returns the column after it.</summary>
    private static int WriteText(TopologyCellCanvas canvas, int column, int row, string text, int limit, TopologyCellRole role, string? id, TopologyHealthStatus status, TopologyCellEmphasis emphasis) {
        foreach (var element in TerminalTextWidth.Elements(text)) {
            var width = TerminalTextWidth.Measure(element);
            if (width <= 0) continue;
            if (column + width > Math.Min(limit, canvas.Columns)) break;
            canvas.Set(column, row, new TopologyCell(element, width, role, id, status, emphasis));
            if (width == 2) canvas.Set(column + 1, row, new TopologyCell(string.Empty, 0, role, id, status, emphasis));
            column += width;
        }

        return column;
    }

    private struct EdgeCell {
        public int Bits;
        public bool Heavy;
        public bool AllDashed;
        public bool Seen;
        public TopologyCellEdge? Edge;
        public TopologyCellEmphasis Emphasis;

        public EdgeCell Add(int bits, TopologyCellEdge edge, TopologyCellEmphasis emphasis) {
            var strong = emphasis == TopologyCellEmphasis.Highlighted || emphasis == TopologyCellEmphasis.Failed;
            var keep = Seen && Rank(Emphasis) >= Rank(emphasis);
            return new EdgeCell {
                Bits = Bits | bits,
                Heavy = Heavy || strong,
                AllDashed = (!Seen || AllDashed) && edge.Dashed,
                Seen = true,
                Edge = keep ? Edge : edge,
                Emphasis = keep ? Emphasis : emphasis
            };
        }

        private static int Rank(TopologyCellEmphasis emphasis) => emphasis switch {
            TopologyCellEmphasis.Failed => 3,
            TopologyCellEmphasis.Highlighted => 2,
            TopologyCellEmphasis.Normal => 1,
            _ => 0
        };
    }
}
