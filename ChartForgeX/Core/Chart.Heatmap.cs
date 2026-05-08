using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <inheritdoc />
public sealed partial class Chart {
    /// <summary>
    /// Adds a heatmap row series.
    /// </summary>
    /// <param name="name">The row name.</param>
    /// <param name="points">The cell values. The x values identify columns and the y values set cell intensity.</param>
    /// <param name="color">An optional high-intensity cell color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddHeatmapRow(string name, IEnumerable<ChartPoint> points, ChartColor? color = null) {
        var materialized = ChartGuards.Points(points, nameof(points));
        if (materialized.Count == 0) throw new ArgumentException("Heatmap rows must contain at least one cell value.", nameof(points));
        Series.Add(new ChartSeries(name ?? throw new ArgumentNullException(nameof(name)), ChartSeriesKind.Heatmap, materialized) { Color = color });
        return this;
    }

    /// <summary>
    /// Adds a heatmap row series from one-based cell values.
    /// </summary>
    /// <param name="name">The row name.</param>
    /// <param name="values">The cell intensities. Values are assigned to columns one through N.</param>
    /// <param name="color">An optional high-intensity cell color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddHeatmapRow(string name, IEnumerable<double> values, ChartColor? color = null) => AddHeatmapRow(name, ChartGuards.Values(values, nameof(values)), color);

    /// <summary>
    /// Adds a heatmap row series from one-based optional cell values.
    /// </summary>
    /// <param name="name">The row name.</param>
    /// <param name="values">The cell intensities. Null cells reserve their column position but do not render.</param>
    /// <param name="color">An optional high-intensity cell color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddHeatmapRow(string name, IEnumerable<double?> values, ChartColor? color = null) {
        var points = OptionalValues(values, nameof(values), out var columnCount);
        AddHeatmapRow(name, points, color);
        Series[Series.Count - 1].HeatmapColumnCount = columnCount;
        return this;
    }

    /// <summary>
    /// Adds multiple heatmap rows from row names and one-based value rows.
    /// </summary>
    /// <param name="rowNames">The row names.</param>
    /// <param name="rows">The row values. Each inner sequence is assigned to columns one through N.</param>
    /// <param name="color">An optional high-intensity cell color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddHeatmapRows(IEnumerable<string> rowNames, IEnumerable<IEnumerable<double>> rows, ChartColor? color = null) => AddValueRows(rowNames, rows, color, ChartSeriesKind.Heatmap);

    /// <summary>
    /// Adds multiple heatmap rows from reusable row models.
    /// </summary>
    /// <param name="rows">The heatmap rows.</param>
    /// <param name="color">An optional high-intensity cell color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddHeatmapRows(IEnumerable<ChartHeatmapRow> rows, ChartColor? color = null) => AddMatrixRows(rows, color, ChartSeriesKind.Heatmap);

    /// <summary>
    /// Adds a hexbin heatmap row series.
    /// </summary>
    /// <param name="name">The row name.</param>
    /// <param name="points">The cell values. The x values identify columns and the y values set hexagon intensity.</param>
    /// <param name="color">An optional high-intensity hexagon color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddHexbinHeatmapRow(string name, IEnumerable<ChartPoint> points, ChartColor? color = null) {
        var materialized = ChartGuards.Points(points, nameof(points));
        if (materialized.Count == 0) throw new ArgumentException("Hexbin heatmap rows must contain at least one cell value.", nameof(points));
        Series.Add(new ChartSeries(name ?? throw new ArgumentNullException(nameof(name)), ChartSeriesKind.HexbinHeatmap, materialized) { Color = color });
        return this;
    }

    /// <summary>
    /// Adds a hexbin heatmap row series from one-based cell values.
    /// </summary>
    /// <param name="name">The row name.</param>
    /// <param name="values">The hexagon intensities. Values are assigned to columns one through N.</param>
    /// <param name="color">An optional high-intensity hexagon color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddHexbinHeatmapRow(string name, IEnumerable<double> values, ChartColor? color = null) => AddHexbinHeatmapRow(name, ChartGuards.Values(values, nameof(values)), color);

    /// <summary>
    /// Adds a hexbin heatmap row series from one-based optional cell values.
    /// </summary>
    /// <param name="name">The row name.</param>
    /// <param name="values">The hexagon intensities. Null cells reserve their column position but do not render.</param>
    /// <param name="color">An optional high-intensity hexagon color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddHexbinHeatmapRow(string name, IEnumerable<double?> values, ChartColor? color = null) {
        var points = OptionalValues(values, nameof(values), out var columnCount);
        AddHexbinHeatmapRow(name, points, color);
        Series[Series.Count - 1].HeatmapColumnCount = columnCount;
        return this;
    }

    /// <summary>
    /// Adds multiple hexbin heatmap rows from row names and one-based value rows.
    /// </summary>
    /// <param name="rowNames">The row names.</param>
    /// <param name="rows">The row values. Each inner sequence is assigned to columns one through N.</param>
    /// <param name="color">An optional high-intensity hexagon color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddHexbinHeatmapRows(IEnumerable<string> rowNames, IEnumerable<IEnumerable<double>> rows, ChartColor? color = null) => AddValueRows(rowNames, rows, color, ChartSeriesKind.HexbinHeatmap);

    /// <summary>
    /// Adds multiple hexbin heatmap rows from reusable row models.
    /// </summary>
    /// <param name="rows">The heatmap rows.</param>
    /// <param name="color">An optional high-intensity hexagon color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddHexbinHeatmapRows(IEnumerable<ChartHeatmapRow> rows, ChartColor? color = null) => AddMatrixRows(rows, color, ChartSeriesKind.HexbinHeatmap);

    private Chart AddValueRows(IEnumerable<string> rowNames, IEnumerable<IEnumerable<double>> rows, ChartColor? color, ChartSeriesKind kind) {
        if (rowNames == null) throw new ArgumentNullException(nameof(rowNames));
        if (rows == null) throw new ArgumentNullException(nameof(rows));
        var names = rowNames.ToArray();
        var values = rows.ToArray();
        if (names.Length != values.Length) throw new ArgumentException("Row name count must match row value count.", nameof(rows));
        if (names.Length == 0) throw new ArgumentException("At least one row is required.", nameof(rows));
        for (var i = 0; i < names.Length; i++) {
            if (kind == ChartSeriesKind.Heatmap) AddHeatmapRow(names[i], values[i], color);
            else AddHexbinHeatmapRow(names[i], values[i], color);
        }

        return this;
    }

    private Chart AddMatrixRows(IEnumerable<ChartHeatmapRow> rows, ChartColor? color, ChartSeriesKind kind) {
        if (rows == null) throw new ArgumentNullException(nameof(rows));
        var materialized = rows.ToArray();
        if (materialized.Length == 0) throw new ArgumentException("At least one row is required.", nameof(rows));
        foreach (var row in materialized) {
            if (row == null) throw new ArgumentException("Heatmap row collection must not contain null entries.", nameof(rows));
            if (kind == ChartSeriesKind.Heatmap) AddHeatmapRow(row.Name, row.Cells, color);
            else AddHexbinHeatmapRow(row.Name, row.Cells, color);
        }

        return this;
    }

    private static IReadOnlyList<ChartPoint> OptionalValues(IEnumerable<double?> values, string parameterName, out int columnCount) {
        if (values == null) throw new ArgumentNullException(parameterName);
        var points = new List<ChartPoint>();
        var index = 1;
        foreach (var value in values) {
            if (value.HasValue) {
                ChartGuards.Finite(value.Value, parameterName + "[" + (index - 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + "]");
                points.Add(new ChartPoint(index, value.Value));
            }

            index++;
        }

        columnCount = index - 1;
        if (points.Count == 0) throw new ArgumentException("At least one visible cell value is required.", parameterName);
        return points;
    }
}
