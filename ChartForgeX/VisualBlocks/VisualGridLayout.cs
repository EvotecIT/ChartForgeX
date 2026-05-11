using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.VisualBlocks;

internal sealed class VisualGridLayout {
    private VisualGridLayout(int width, int height, int headerHeight, int panelWidth, int panelHeight, IReadOnlyList<VisualGridCell> cells) {
        Width = width;
        Height = height;
        HeaderHeight = headerHeight;
        PanelWidth = panelWidth;
        PanelHeight = panelHeight;
        Cells = cells;
    }

    public int Width { get; }
    public int Height { get; }
    public int HeaderHeight { get; }
    public int PanelWidth { get; }
    public int PanelHeight { get; }
    public IReadOnlyList<VisualGridCell> Cells { get; }

    public static VisualGridLayout FromGrid(VisualGrid grid) {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        if (grid.Items.Count == 0) throw new InvalidOperationException("Visual grids must contain at least one item.");
        var columns = Math.Min(grid.Columns, grid.Items.Count);
        var panelWidth = grid.PanelSize.HasValue ? grid.PanelSize.Value.Width : 1;
        var panelHeight = grid.PanelSize.HasValue ? grid.PanelSize.Value.Height : 1;
        foreach (var item in grid.Items) {
            if (grid.PanelSize.HasValue) break;
            var size = ItemSize(item);
            var columnSpan = Math.Min(item.ColumnSpan, columns);
            var itemPanelWidth = grid.AdaptiveRowHeights ? (int)Math.Ceiling((size.Width - (columnSpan - 1) * grid.Gap) / (double)columnSpan) : size.Width;
            panelWidth = Math.Max(panelWidth, itemPanelWidth);
            panelHeight = Math.Max(panelHeight, size.Height);
        }

        var theme = grid.Theme ?? ItemTheme(grid.Items[0]);
        var headerHeight = grid.Title.Length == 0 && grid.Subtitle.Length == 0 ? 0 : Math.Max(76, (int)Math.Ceiling(theme.TitleFontSize + (grid.Subtitle.Length == 0 ? 0 : theme.SubtitleFontSize) + 37));
        var width = grid.Padding * 2 + columns * panelWidth + (columns - 1) * grid.Gap;
        var occupied = new List<bool[]>();
        var placements = new List<VisualGridPlacement>(grid.Items.Count);
        var rowHeights = new List<int>();
        foreach (var item in grid.Items) {
            var columnSpan = Math.Min(item.ColumnSpan, columns);
            var rowSpan = item.RowSpan;
            var placement = FindPlacement(occupied, columns, columnSpan, rowSpan);
            MarkOccupied(occupied, columns, placement.Row, placement.Column, columnSpan, rowSpan);
            var defaultRowHeight = !grid.PanelSize.HasValue && grid.AdaptiveRowHeights ? 1 : panelHeight;
            while (rowHeights.Count < placement.Row + rowSpan) rowHeights.Add(defaultRowHeight);
            if (!grid.PanelSize.HasValue && grid.AdaptiveRowHeights) {
                var size = ItemSize(item);
                var naturalRowHeight = Math.Max(1, (int)Math.Ceiling((size.Height - (rowSpan - 1) * grid.Gap) / (double)rowSpan));
                for (var row = placement.Row; row < placement.Row + rowSpan; row++) rowHeights[row] = Math.Max(rowHeights[row], naturalRowHeight);
            }

            placements.Add(new VisualGridPlacement(item, placement.Row, placement.Column, columnSpan, rowSpan));
        }

        if (!grid.PanelSize.HasValue && grid.AdaptiveRowHeights) {
            for (var row = 0; row < rowHeights.Count; row++) {
                var occupiedInRow = false;
                for (var column = 0; column < columns; column++) if (occupied[row][column]) { occupiedInRow = true; break; }
                if (!occupiedInRow) rowHeights[row] = 0;
            }
        }

        var rowOffsets = new List<int>(rowHeights.Count);
        var cursorY = grid.Padding + headerHeight;
        for (var row = 0; row < rowHeights.Count; row++) {
            rowOffsets.Add(cursorY);
            cursorY += rowHeights[row] + grid.Gap;
        }

        var cells = new List<VisualGridCell>(grid.Items.Count);
        foreach (var placement in placements) {
            var item = placement.Item;
            var columnSpan = placement.ColumnSpan;
            var rowSpan = placement.RowSpan;
            var panelX = grid.Padding + placement.Column * (panelWidth + grid.Gap);
            var panelY = rowOffsets[placement.Row];
            var spannedWidth = columnSpan * panelWidth + (columnSpan - 1) * grid.Gap;
            var spannedHeight = SpannedRowHeight(rowHeights, placement.Row, rowSpan, grid.Gap);
            cells.Add(FitCell(item, panelX, panelY, spannedWidth, spannedHeight, grid.PanelSize.HasValue, grid.PanelFit));
        }

        var rows = rowHeights.Count;
        var contentHeight = 0;
        for (var row = 0; row < rows; row++) contentHeight += rowHeights[row];
        var height = grid.Padding * 2 + headerHeight + contentHeight + Math.Max(0, rows - 1) * grid.Gap;
        return new VisualGridLayout(width, height, headerHeight, panelWidth, panelHeight, cells);
    }

    public static ChartSize ItemSize(VisualGridItem item) {
        if (item.Chart != null) return item.Chart.Options.Size;
        if (item.Block != null) return item.Block.Options.Size;
        throw new InvalidOperationException("Visual grid items must contain a chart or visual block.");
    }

    public static ChartTheme ItemTheme(VisualGridItem item) {
        if (item.Chart != null) return item.Chart.Options.Theme;
        if (item.Block != null) return item.Block.Options.Theme;
        throw new InvalidOperationException("Visual grid items must contain a chart or visual block.");
    }

    private static VisualGridCell FitCell(VisualGridItem item, int panelX, int panelY, int panelWidth, int panelHeight, bool fixedPanel, VisualGridPanelFit fit) {
        if (fixedPanel && fit == VisualGridPanelFit.Stretch) return new VisualGridCell(item, panelX, panelY, panelWidth, panelHeight);
        var size = ItemSize(item);
        if (!fixedPanel) return new VisualGridCell(item, panelX + (panelWidth - size.Width) / 2, panelY + (panelHeight - size.Height) / 2, size.Width, size.Height);
        var scale = Math.Min(panelWidth / (double)size.Width, panelHeight / (double)size.Height);
        var width = Math.Max(1, (int)Math.Round(size.Width * scale));
        var height = Math.Max(1, (int)Math.Round(size.Height * scale));
        return new VisualGridCell(item, panelX + (panelWidth - width) / 2, panelY + (panelHeight - height) / 2, width, height);
    }

    private static (int Row, int Column) FindPlacement(IReadOnlyList<bool[]> occupied, int columns, int columnSpan, int rowSpan) {
        for (var row = 0; ; row++) {
            for (var column = 0; column <= columns - columnSpan; column++) if (CanPlace(occupied, column, row, columnSpan, rowSpan)) return (row, column);
        }
    }

    private static bool CanPlace(IReadOnlyList<bool[]> occupied, int column, int row, int columnSpan, int rowSpan) {
        for (var r = row; r < row + rowSpan; r++) {
            if (r >= occupied.Count) continue;
            var rowCells = occupied[r];
            for (var c = column; c < column + columnSpan; c++) if (rowCells[c]) return false;
        }

        return true;
    }

    private static void MarkOccupied(List<bool[]> occupied, int columns, int row, int column, int columnSpan, int rowSpan) {
        while (occupied.Count < row + rowSpan) occupied.Add(new bool[columns]);
        for (var r = row; r < row + rowSpan; r++) for (var c = column; c < column + columnSpan; c++) occupied[r][c] = true;
    }

    private static int SpannedRowHeight(IReadOnlyList<int> rowHeights, int row, int rowSpan, int gap) {
        var height = 0;
        for (var i = row; i < row + rowSpan; i++) height += rowHeights[i];
        return height + Math.Max(0, rowSpan - 1) * gap;
    }
}

internal sealed class VisualGridPlacement {
    public VisualGridPlacement(VisualGridItem item, int row, int column, int columnSpan, int rowSpan) {
        Item = item;
        Row = row;
        Column = column;
        ColumnSpan = columnSpan;
        RowSpan = rowSpan;
    }

    public VisualGridItem Item { get; }
    public int Row { get; }
    public int Column { get; }
    public int ColumnSpan { get; }
    public int RowSpan { get; }
}

internal sealed class VisualGridCell {
    public VisualGridCell(VisualGridItem item, int x, int y, int width, int height) {
        Item = item;
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public VisualGridItem Item { get; }
    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }
}
