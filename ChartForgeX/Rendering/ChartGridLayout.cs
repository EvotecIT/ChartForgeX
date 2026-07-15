using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal sealed class ChartGridLayout {
    private ChartGridLayout(int width, int height, int headerHeight, IReadOnlyList<ChartGridCell> cells) {
        Width = width;
        Height = height;
        HeaderHeight = headerHeight;
        Cells = cells;
    }

    public int Width { get; }
    public int Height { get; }
    public int HeaderHeight { get; }
    public IReadOnlyList<ChartGridCell> Cells { get; }

    public static ChartGridLayout FromGrid(ChartGrid grid) {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        if (grid.Charts.Count == 0) throw new InvalidOperationException("Chart grids must contain at least one chart.");

        var columns = Math.Min(grid.Columns, grid.Charts.Count);
        var panelWidth = grid.PanelSize.HasValue ? grid.PanelSize.Value.Width : 1;
        var panelHeight = grid.PanelSize.HasValue ? grid.PanelSize.Value.Height : 1;
        foreach (var chart in grid.Charts) {
            if (grid.PanelSize.HasValue) break;
            panelWidth = Math.Max(panelWidth, chart.Options.Size.Width);
            panelHeight = Math.Max(panelHeight, chart.Options.Size.Height);
        }

        var theme = grid.Theme ?? grid.Charts[0].Options.Theme;
        var titleFontSize = grid.TitleStyle.FontSize ?? theme.TitleFontSize;
        var subtitleFontSize = grid.SubtitleStyle.FontSize ?? theme.SubtitleFontSize;
        var headerHeight = grid.Title.Length == 0 && grid.Subtitle.Length == 0 ? 0 : Math.Max(76, (int)Math.Ceiling(titleFontSize + (grid.Subtitle.Length == 0 ? 0 : subtitleFontSize) + 37));
        var width = grid.Padding * 2 + columns * panelWidth + (columns - 1) * grid.Gap;
        var occupied = new List<bool[]>();
        var cells = new List<ChartGridCell>(grid.Charts.Count);
        for (var i = 0; i < grid.Charts.Count; i++) {
            var chart = grid.Charts[i];
            var span = i < grid.PanelSpans.Count ? grid.PanelSpans[i] : new ChartGridPanelSpan(1, 1);
            var columnSpan = Math.Min(span.ColumnSpan, columns);
            var rowSpan = span.RowSpan;
            var (row, column) = FindPlacement(occupied, columns, columnSpan, rowSpan);
            MarkOccupied(occupied, columns, row, column, columnSpan, rowSpan);
            var panelX = grid.Padding + column * (panelWidth + grid.Gap);
            var panelY = grid.Padding + headerHeight + row * (panelHeight + grid.Gap);
            var spannedPanelWidth = columnSpan * panelWidth + (columnSpan - 1) * grid.Gap;
            var spannedPanelHeight = rowSpan * panelHeight + (rowSpan - 1) * grid.Gap;
            if (grid.PanelSize.HasValue) {
                cells.Add(FitCell(chart, panelX, panelY, spannedPanelWidth, spannedPanelHeight, grid.PanelFit));
            } else {
                var x = panelX + (spannedPanelWidth - chart.Options.Size.Width) / 2;
                var y = panelY + (spannedPanelHeight - chart.Options.Size.Height) / 2;
                cells.Add(new ChartGridCell(chart, x, y, chart.Options.Size.Width, chart.Options.Size.Height));
            }
        }

        var rows = occupied.Count;
        var height = grid.Padding * 2 + headerHeight + rows * panelHeight + Math.Max(0, rows - 1) * grid.Gap;
        return new ChartGridLayout(width, height, headerHeight, cells);
    }

    private static (int Row, int Column) FindPlacement(IReadOnlyList<bool[]> occupied, int columns, int columnSpan, int rowSpan) {
        for (var row = 0; ; row++) {
            for (var column = 0; column <= columns - columnSpan; column++) {
                if (CanPlace(occupied, column, row, columnSpan, rowSpan)) return (row, column);
            }
        }
    }

    private static bool CanPlace(IReadOnlyList<bool[]> occupied, int column, int row, int columnSpan, int rowSpan) {
        for (var r = row; r < row + rowSpan; r++) {
            if (r >= occupied.Count) continue;
            var rowCells = occupied[r];
            for (var c = column; c < column + columnSpan; c++) {
                if (rowCells[c]) return false;
            }
        }

        return true;
    }

    private static void MarkOccupied(List<bool[]> occupied, int columns, int row, int column, int columnSpan, int rowSpan) {
        while (occupied.Count < row + rowSpan) occupied.Add(new bool[columns]);
        for (var r = row; r < row + rowSpan; r++) {
            for (var c = column; c < column + columnSpan; c++) occupied[r][c] = true;
        }
    }

    private static ChartGridCell FitCell(Chart chart, int panelX, int panelY, int panelWidth, int panelHeight, VisualPanelFit fit) {
        if (fit == VisualPanelFit.Stretch) return new ChartGridCell(chart, panelX, panelY, panelWidth, panelHeight);
        var scale = Math.Min(panelWidth / (double)chart.Options.Size.Width, panelHeight / (double)chart.Options.Size.Height);
        var width = Math.Max(1, (int)Math.Round(chart.Options.Size.Width * scale));
        var height = Math.Max(1, (int)Math.Round(chart.Options.Size.Height * scale));
        var x = panelX + (panelWidth - width) / 2;
        var y = panelY + (panelHeight - height) / 2;
        return new ChartGridCell(chart, x, y, width, height);
    }
}
