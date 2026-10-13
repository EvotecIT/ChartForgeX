using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualBlocks;

internal static partial class VisualBlockRendering {
    public const int MaximumBlockLayoutItems = 10000;
    public const int MaximumBlockLayoutEdges = 20000;

    public static ChartRect BlockLayoutCaptionBounds(BlockLayoutShape shape, double x, double y, double width, double height) {
        var innerWidth = width; var innerHeight = height;
        if (shape == BlockLayoutShape.Circle) innerWidth = innerHeight = Math.Min(width, height) / Math.Sqrt(2);
        innerWidth = Math.Max(1, innerWidth - 16); innerHeight = Math.Max(1, innerHeight - 12);
        return new ChartRect(x + (width - innerWidth) / 2, y + (height - innerHeight) / 2, innerWidth, innerHeight);
    }

    // Cell and connector captions share measured sizing and baseline placement in both backends.
    public static TextStyle BlockLayoutText(ResolvedVisualTheme theme, string text, double defaultSize, double width, double height) =>
        FitBodyTextStyle(theme, text, defaultSize, width, height, 700);

    public static double BlockLayoutBaseline(TextStyle style, double centerY) {
        var ascent = VisualBlockRendering.TextAscent(style);
        return centerY - TextLayoutEngine.Measure("Mg", style).Height / 2 + ascent;
    }

    public static void ValidateBlockLayout(BlockLayoutBlock block) {
        if (block.Items.Count == 0) throw new InvalidOperationException("Block layouts must contain at least one item.");
        if (block.Items.Count > MaximumBlockLayoutItems) throw new InvalidOperationException("Block layouts must contain no more than " + MaximumBlockLayoutItems.ToString(CultureInfo.InvariantCulture) + " items.");
        if (block.Edges.Count > MaximumBlockLayoutEdges) throw new InvalidOperationException("Block layouts must contain no more than " + MaximumBlockLayoutEdges.ToString(CultureInfo.InvariantCulture) + " edges.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var visible = 0;
        foreach (var item in block.Items) {
            if (item.ColumnSpan > block.Columns) throw new InvalidOperationException("Block layout item spans must not exceed the layout column count.");
            if (item.IsSpace) continue;
            if (item.Id.Length == 0) throw new InvalidOperationException("Block layout items must define ids.");
            if (item.Label.Length == 0) throw new InvalidOperationException("Block layout items must define labels.");
            if (!ids.Add(item.Id)) throw new InvalidOperationException("Block layout item ids must be unique: " + item.Id + ".");
            visible++;
        }

        if (visible == 0) throw new InvalidOperationException("Block layouts must contain at least one visible item.");
        foreach (var edge in block.Edges) {
            if (!ids.Contains(edge.SourceId)) throw new InvalidOperationException("Block layout edge source id was not found: " + edge.SourceId + ".");
            if (!ids.Contains(edge.TargetId)) throw new InvalidOperationException("Block layout edge target id was not found: " + edge.TargetId + ".");
        }
    }

    public static IReadOnlyList<BlockLayoutPlacement> BlockLayoutPlacements(BlockLayoutBlock block) {
        var placements = new List<BlockLayoutPlacement>(block.Items.Count);
        var row = 0;
        var column = 0;
        for (var index = 0; index < block.Items.Count; index++) {
            var item = block.Items[index];
            var span = Math.Min(block.Columns, Math.Max(1, item.ColumnSpan));
            if (column > 0 && column + span > block.Columns) {
                row++;
                column = 0;
            }

            placements.Add(new BlockLayoutPlacement(item, index, row, column, span));
            column += span;
            if (column >= block.Columns) {
                row++;
                column = 0;
            }
        }

        return placements;
    }

    public readonly struct BlockLayoutPlacement {
        public BlockLayoutPlacement(BlockLayoutItem item, int itemIndex, int row, int column, int columnSpan) {
            Item = item;
            ItemIndex = itemIndex;
            Row = row;
            Column = column;
            ColumnSpan = columnSpan;
        }

        public BlockLayoutItem Item { get; }
        public int ItemIndex { get; }
        public int Row { get; }
        public int Column { get; }
        public int ColumnSpan { get; }
    }
}
