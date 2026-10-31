using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    private IReadOnlyList<ChartHeatmapCell?> _heatmapCells = Array.Empty<ChartHeatmapCell?>();
    /// <summary>Gets categorical cells in column order, including null masked cells. Empty for numeric rows.</summary>
    /// <remarks>Observation indices and IDs refer to columns, so masking a cell does not change the identity of later cells.</remarks>
    public IReadOnlyList<ChartHeatmapCell?> HeatmapCells => _heatmapCells;

    /// <summary>Replaces categorical columns atomically, including masked cells. Clears observation IDs and presentation overrides.</summary>
    public ChartSeries ReplaceHeatmapCells(IEnumerable<ChartHeatmapCell?> values) {
        RequireCategoricalHeatmap();
        ReplaceObservations(ref _heatmapCells, values, ValidateHeatmapCell);
        HeatmapColumnCount = _heatmapCells.Count;
        return this;
    }

    /// <summary>Updates or masks one column while retaining its index, ID and presentation overrides.</summary>
    public ChartSeries UpdateHeatmapCell(int index, ChartHeatmapCell? value) {
        RequireCategoricalHeatmap();
        UpdateObservation(ref _heatmapCells, index, value, ValidateHeatmapCell);
        return this;
    }

    private void RequireCategoricalHeatmap() {
        RequireObservationKind(ChartSeriesKind.Heatmap);
        if (!IsCategoricalHeatmapRow) throw new InvalidOperationException("Categorical cells require a categorical heatmap row factory.");
    }

    private static ChartHeatmapCell? ValidateHeatmapCell(ChartHeatmapCell? value) => value.HasValue
        ? new ChartHeatmapCell(value.Value.State, value.Value.Text, value.Value.Tooltip, value.Value.Href).WithMarkOptions(value.Value.MarkOptions) : null;

    /// <summary>Creates an independently configurable categorical row. Null cells retain their masked column positions.</summary>
    public static ChartSeries CreateHeatmapCategoryRow(ChartSeriesId id, string name, IEnumerable<ChartHeatmapCell?> cells, string? group = null) =>
        CategoricalHeatmap(new ChartSeries(id, name, ChartSeriesKind.Heatmap, Array.Empty<ChartForgeX.Primitives.ChartPoint>()), cells, group);

    internal static ChartSeries CreateHeatmapCategoryRow(string name, IEnumerable<ChartHeatmapCell?> cells, string? group) =>
        CategoricalHeatmap(new ChartSeries(name, ChartSeriesKind.Heatmap, Array.Empty<ChartForgeX.Primitives.ChartPoint>()), cells, group);

    private static ChartSeries CategoricalHeatmap(ChartSeries series, IEnumerable<ChartHeatmapCell?> cells, string? group) {
        series.IsCategoricalHeatmapRow = true; series.ShowInLegend = false;
        series.LaneGroup = string.IsNullOrWhiteSpace(group) ? null : group;
        series.ReplaceHeatmapCells(cells);
        return series;
    }
}
