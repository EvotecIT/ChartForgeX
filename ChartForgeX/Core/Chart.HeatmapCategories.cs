using System;
using System.Collections.Generic;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a categorical heatmap row. Each cell's state is coloured through <see cref="ChartOptions.StateCategories"/>
    /// (see <see cref="WithStateCategories(ChartStateCategory[])"/>), so status colours never come from a numeric ramp.
    /// Cells are assigned to columns one through N; null cells are masked and keep their column position.
    /// </summary>
    /// <param name="name">The row name.</param>
    /// <param name="cells">The row cells in column order.</param>
    /// <returns>The current chart.</returns>
    public Chart AddHeatmapCategoryRow(string name, IEnumerable<ChartHeatmapCell?> cells) {
        if (name == null) throw new ArgumentNullException(nameof(name));
        if (cells == null) throw new ArgumentNullException(nameof(cells));
        var points = new List<ChartPoint>();
        var visible = new List<ChartHeatmapCell>();
        var column = 0;
        foreach (var cell in cells) {
            column++;
            if (!cell.HasValue) continue;
            if (cell.Value.State == null) throw new ArgumentException("Heatmap cells must be created with a state.", nameof(cells));
            points.Add(new ChartPoint(column, 0));
            visible.Add(cell.Value);
        }

        if (visible.Count == 0) throw new ArgumentException("Categorical heatmap rows must contain at least one visible cell.", nameof(cells));
        var series = new ChartSeries(name, ChartSeriesKind.Heatmap, points) { HeatmapColumnCount = column, ShowInLegend = false };
        foreach (var cell in visible) {
            series.HeatmapCells.Add(cell);
            series.PointLabels.Add(cell.Text);
        }

        Series.Add(series);
        return this;
    }

    /// <summary>Adds a categorical heatmap row with every column filled.</summary>
    /// <param name="name">The row name.</param>
    /// <param name="cells">The row cells in column order.</param>
    /// <returns>The current chart.</returns>
    public Chart AddHeatmapCategoryRow(string name, IEnumerable<ChartHeatmapCell> cells) {
        if (cells == null) throw new ArgumentNullException(nameof(cells));
        var nullable = new List<ChartHeatmapCell?>();
        foreach (var cell in cells) nullable.Add(cell);
        return AddHeatmapCategoryRow(name, nullable);
    }

    /// <summary>Adds a categorical heatmap row from cells in column order; null cells are masked.</summary>
    /// <param name="name">The row name.</param>
    /// <param name="cells">The row cells in column order.</param>
    /// <returns>The current chart.</returns>
    public Chart AddHeatmapCategoryRow(string name, params ChartHeatmapCell?[] cells) => AddHeatmapCategoryRow(name, (IEnumerable<ChartHeatmapCell?>)cells);
}
