using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualBlocks;

internal static partial class VisualBlockRendering {
    internal const int MaximumBlockHierarchyDepth = 64;

    internal static bool ExtendedBlockLayout(BlockLayoutBlock block) => block.AutoColumns || block.Edges.Any(edge => !edge.Visible || edge.Stroke != null) || block.Items.Any(item =>
        item.ParentId != null || item.IsComposite || (int)item.Shape > (int)BlockLayoutShape.Database || item.Fill.HasValue || item.Stroke != null || item.TextStyle.HasOverrides);

    private static void ValidateBlockHierarchy(BlockLayoutBlock block) {
        var ids = new Dictionary<string, BlockLayoutItem>(StringComparer.Ordinal);
        var children = new Dictionary<string, List<BlockLayoutItem>>(StringComparer.Ordinal);
        foreach (var item in block.Items) {
            if (item.Id.Length == 0) throw new InvalidOperationException("Block layout items must define ids.");
            if (ids.ContainsKey(item.Id)) throw new InvalidOperationException("Block layout item ids must be unique: " + item.Id + ".");
            ids.Add(item.Id, item);
            if (!item.IsSpace && !item.IsComposite && item.Label.Length == 0) throw new InvalidOperationException("Block layout items must define labels.");
            if (!Enum.IsDefined(typeof(BlockLayoutShape), item.Shape)) throw new InvalidOperationException("Unknown block layout shape.");
            if (item.IsSpace && item.IsComposite) throw new InvalidOperationException("A spacer cannot be a composite.");
            if (!item.IsComposite && (item.Columns.HasValue || item.AutoColumns)) throw new InvalidOperationException("Only composite items have local columns.");
            if (item.IsComposite && item.Shape != BlockLayoutShape.Rectangle) throw new InvalidOperationException("Composite containers use rectangular bounds.");
            if (item.Shape == BlockLayoutShape.Arrow && ((int)item.ArrowDirections <= 0 || ((int)item.ArrowDirections & ~15) != 0))
                throw new InvalidOperationException("Block arrows require known, nonempty directions.");
            if (item.TextStyle.HasOverrides) {
                var caption = item.TextStyle.Resolve(new TextStyle());
                if (caption.Underline || caption.Strikethrough || caption.Baseline != TextBaseline.Normal)
                    throw new NotSupportedException("Block captions support measured font, color and case overrides; decorated and scripted baselines are unavailable.");
            }
            var parent = item.ParentId ?? "";
            if (!children.TryGetValue(parent, out var list)) children.Add(parent, list = new List<BlockLayoutItem>());
            list.Add(item);
        }
        if (!block.Items.Any(item => !item.IsSpace)) throw new InvalidOperationException("Block layouts must contain at least one visible item.");
        var rootColumns = GridColumns("", block.Columns, block.AutoColumns);
        var depth = new Dictionary<string, int>(StringComparer.Ordinal);
        var columns = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var item in block.Items) {
            var chain = new List<BlockLayoutItem>();
            var active = new HashSet<string>(StringComparer.Ordinal);
            var current = item;
            while (!depth.ContainsKey(current.Id)) {
                if (!active.Add(current.Id)) throw new InvalidOperationException("Block composite parent references cannot contain cycles.");
                chain.Add(current);
                if (chain.Count > MaximumBlockHierarchyDepth) throw new InvalidOperationException("Block composite depth must not exceed sixty-four.");
                if (current.ParentId == null) break;
                if (!ids.TryGetValue(current.ParentId, out var parent) || !parent.IsComposite)
                    throw new InvalidOperationException("Block parent id must identify a composite: " + current.ParentId + ".");
                current = parent;
            }
            var parentDepth = depth.TryGetValue(current.Id, out var known) ? known : -1;
            for (var i = chain.Count - 1; i >= 0; i--) {
                var node = chain[i]; var d = ++parentDepth;
                if (d >= MaximumBlockHierarchyDepth) throw new InvalidOperationException("Block composite depth must not exceed sixty-four.");
                depth[node.Id] = d;
                var inherited = node.ParentId == null ? rootColumns : columns[node.ParentId];
                if (node.ColumnSpan > inherited) throw new InvalidOperationException("Block layout item spans must not exceed their containing grid column count.");
                columns[node.Id] = node.IsComposite ? GridColumns(node.Id, node.Columns ?? inherited, node.AutoColumns) : inherited;
            }
        }
        foreach (var edge in block.Edges) {
            if (!ids.TryGetValue(edge.SourceId, out var source) || source.IsSpace) throw new InvalidOperationException("Block layout edge source id was not found: " + edge.SourceId + ".");
            if (!ids.TryGetValue(edge.TargetId, out var target) || target.IsSpace) throw new InvalidOperationException("Block layout edge target id was not found: " + edge.TargetId + ".");
        }
        int GridColumns(string id, int fixedColumns, bool auto) {
            if (!auto || !children.TryGetValue(id, out var list)) return fixedColumns;
            var count = 0;
            foreach (var child in list) count = checked(count + child.ColumnSpan);
            return Math.Max(1, count);
        }
    }

    internal sealed class BlockHierarchyPlan {
        internal readonly List<BlockHierarchyCell> Cells = new();
        internal readonly Dictionary<string, BlockHierarchyCell> ById = new(StringComparer.Ordinal);
    }
    internal sealed class BlockHierarchyCell {
        internal BlockLayoutItem Item = null!;
        internal int SourceIndex;
        internal int Depth;
        internal ChartRect Bounds;
        internal ChartRect? ParentBounds;
    }

    // Each item is grouped, measured and arranged once. Parent traversal is bounded by the shared depth limit.
    internal static BlockHierarchyPlan BlockHierarchy(BlockLayoutBlock block, ChartRect frame) {
        var groups = new Dictionary<string, List<BlockHierarchyCell>>(StringComparer.Ordinal);
        for (var i = 0; i < block.Items.Count; i++) {
            var item = block.Items[i]; var parent = item.ParentId ?? "";
            if (!groups.TryGetValue(parent, out var list)) groups.Add(parent, list = new List<BlockHierarchyCell>());
            list.Add(new BlockHierarchyCell { Item = item, SourceIndex = i });
        }
        var plan = new BlockHierarchyPlan();
        var measured = new Dictionary<string, double>(StringComparer.Ordinal);
        Arrange("", block.Columns, block.AutoColumns, frame, null, 0);
        plan.Cells.Sort((first, second) => first.SourceIndex.CompareTo(second.SourceIndex));
        return plan;

        List<(BlockHierarchyCell Cell, int Row, int Column)> Rows(string id, int columns) {
            var result = new List<(BlockHierarchyCell, int, int)>();
            if (!groups.TryGetValue(id, out var items)) return result;
            var row = 0; var column = 0;
            foreach (var cell in items) {
                if (column > 0 && column + cell.Item.ColumnSpan > columns) { row++; column = 0; }
                result.Add((cell, row, column));
                column += cell.Item.ColumnSpan;
                if (column >= columns) { row++; column = 0; }
            }
            return result;
        }
        int Columns(string id, int inherited, bool auto) => auto && groups.TryGetValue(id, out var items)
            ? Math.Max(1, items.Sum(cell => cell.Item.ColumnSpan)) : inherited;
        double Measure(string id, int inherited, bool auto) {
            if (measured.TryGetValue(id, out var height)) return height;
            var columns = Columns(id, inherited, auto); var rows = Rows(id, columns);
            if (rows.Count == 0) return measured[id] = 48;
            var heights = new double[rows[rows.Count - 1].Row + 1];
            foreach (var row in rows) heights[row.Row] = Math.Max(heights[row.Row], row.Cell.Item.IsComposite
                ? Measure(row.Cell.Item.Id, row.Cell.Item.Columns ?? columns, row.Cell.Item.AutoColumns) + 28 + (row.Cell.Item.Label.Length > 0 ? 22 : 0) : 66);
            return measured[id] = heights.Sum() + 14 * Math.Max(0, heights.Length - 1);
        }
        void Arrange(string id, int inherited, bool auto, ChartRect bounds, ChartRect? parentBounds, int depth) {
            var columns = Columns(id, inherited, auto); var rows = Rows(id, columns);
            if (rows.Count == 0) return;
            var heights = new double[rows[rows.Count - 1].Row + 1];
            foreach (var row in rows) heights[row.Row] = Math.Max(heights[row.Row], row.Cell.Item.IsComposite
                ? Measure(row.Cell.Item.Id, row.Cell.Item.Columns ?? columns, row.Cell.Item.AutoColumns) + 28 + (row.Cell.Item.Label.Length > 0 ? 22 : 0) : 66);
            var rowGap = Math.Min(14, bounds.Height / Math.Max(1, heights.Length * 5));
            var columnGap = Math.Min(14, bounds.Width / Math.Max(1, columns * 5));
            var scale = Math.Min(1, Math.Max(0, bounds.Height - rowGap * (heights.Length - 1)) / heights.Sum());
            var cellWidth = Math.Max(0, (bounds.Width - columnGap * (columns - 1)) / columns);
            var tops = new double[heights.Length]; var top = bounds.Top;
            for (var i = 0; i < heights.Length; i++) { tops[i] = top; top += heights[i] * scale + rowGap; }
            foreach (var row in rows) {
                var cell = row.Cell; var item = cell.Item;
                cell.Bounds = new ChartRect(bounds.Left + row.Column * (cellWidth + columnGap), tops[row.Row],
                    cellWidth * item.ColumnSpan + columnGap * (item.ColumnSpan - 1), heights[row.Row] * scale);
                if (!item.IsSpace && (cell.Bounds.Width < 1 || cell.Bounds.Height < 1))
                    throw new InvalidOperationException("The block grid does not fit its native frame. Increase the frame or use fewer columns.");
                cell.ParentBounds = parentBounds; cell.Depth = depth;
                plan.Cells.Add(cell); if (!item.IsSpace) plan.ById.Add(item.Id, cell);
                if (!item.IsComposite) continue;
                var inset = Math.Min(12, Math.Min(cell.Bounds.Width, cell.Bounds.Height) * .1);
                var heading = item.Label.Length == 0 ? 0 : Math.Min(22, cell.Bounds.Height * .25);
                var child = new ChartRect(cell.Bounds.Left + inset, cell.Bounds.Top + inset + heading,
                    Math.Max(0, cell.Bounds.Width - 2 * inset), Math.Max(0, cell.Bounds.Height - 2 * inset - heading));
                Arrange(item.Id, item.Columns ?? columns, item.AutoColumns, child, cell.Bounds, depth + 1);
            }
        }
    }
}
