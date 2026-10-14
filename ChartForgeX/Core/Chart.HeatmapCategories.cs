using System;
using System.Collections.Generic;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a categorical heatmap row. Each cell's state is coloured through <see cref="ChartOptions.StateCategories"/>
    /// (see <see cref="WithStateCategories(ChartStateCategory[])"/>), so status colours never come from a numeric ramp.
    /// Cells are assigned to columns one through N; null cells are masked and keep their column position. A row may
    /// have no visible cells, for an entity nothing is known about: it keeps its label and an empty row.
    /// </summary>
    /// <param name="name">The row name.</param>
    /// <param name="cells">The row cells in column order.</param>
    /// <param name="group">Optional group the row is listed under. Consecutive rows with the same group share one
    /// header row; a row without a group after a grouped one is set apart by a small gap.</param>
    /// <param name="id">Optional stable series identity. When omitted, the chart assigns a local ID.</param>
    /// <returns>The current chart.</returns>
    public Chart AddHeatmapCategoryRow(string name, IEnumerable<ChartHeatmapCell?> cells, string? group = null, ChartSeriesId? id = null) {
        AppendSeries(id.HasValue ? ChartSeries.CreateHeatmapCategoryRow(id.Value, name, cells, group)
            : ChartSeries.CreateHeatmapCategoryRow(name, cells, group));
        return this;
    }

    /// <summary>Adds a categorical heatmap row with every column filled.</summary>
    /// <param name="name">The row name.</param>
    /// <param name="cells">The row cells in column order.</param>
    /// <param name="group">Optional group the row is listed under.</param>
    /// <param name="id">Optional stable series identity. When omitted, the chart assigns a local ID.</param>
    /// <returns>The current chart.</returns>
    public Chart AddHeatmapCategoryRow(string name, IEnumerable<ChartHeatmapCell> cells, string? group = null, ChartSeriesId? id = null) {
        if (cells == null) throw new ArgumentNullException(nameof(cells));
        var nullable = new List<ChartHeatmapCell?>();
        foreach (var cell in cells) nullable.Add(cell);
        return AddHeatmapCategoryRow(name, nullable, group, id);
    }

    /// <summary>Adds a categorical heatmap row from cells in column order; null cells are masked.</summary>
    /// <param name="name">The row name.</param>
    /// <param name="cells">The row cells in column order.</param>
    /// <returns>The current chart.</returns>
    public Chart AddHeatmapCategoryRow(string name, params ChartHeatmapCell?[] cells) => AddHeatmapCategoryRow(name, (IEnumerable<ChartHeatmapCell?>)cells);
}
