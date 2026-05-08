using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Core;

/// <summary>
/// Describes one reusable heatmap matrix row.
/// </summary>
public sealed class ChartHeatmapRow {
    private readonly IReadOnlyList<double?> _cells;
    private readonly IReadOnlyList<double> _values;

    /// <summary>
    /// Initializes a heatmap row.
    /// </summary>
    /// <param name="name">The row name.</param>
    /// <param name="values">The row values. Values are assigned to columns one through N.</param>
    public ChartHeatmapRow(string name, IEnumerable<double> values) : this(name, ToNullableValues(values), false) {
    }

    private ChartHeatmapRow(string name, IEnumerable<double?> values, bool allowMissingCells) {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        if (values == null) throw new ArgumentNullException(nameof(values));
        var cells = values.ToArray();
        var finiteValues = new List<double>(cells.Length);
        for (var i = 0; i < cells.Length; i++) {
            if (!cells[i].HasValue) {
                if (!allowMissingCells) throw new ArgumentException("Heatmap row values must not contain missing cells unless the row is created as masked.", nameof(values));
                continue;
            }

            var value = cells[i]!.Value;
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(values), value, "Heatmap row values must be finite.");
            finiteValues.Add(value);
        }

        if (finiteValues.Count == 0) throw new ArgumentException("Heatmap rows must contain at least one visible cell value.", nameof(values));
        _cells = Array.AsReadOnly(cells);
        _values = Array.AsReadOnly(finiteValues.ToArray());
    }

    /// <summary>
    /// Gets the row name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the row values.
    /// </summary>
    public IReadOnlyList<double> Values => _values;

    /// <summary>
    /// Gets row cells. Null entries are masked cells that do not render.
    /// </summary>
    public IReadOnlyList<double?> Cells => _cells;

    /// <summary>
    /// Gets a value indicating whether the row contains masked cells.
    /// </summary>
    public bool HasMaskedCells => _cells.Any(value => !value.HasValue);

    /// <summary>
    /// Creates a heatmap row from one-based cell values.
    /// </summary>
    /// <param name="name">The row name.</param>
    /// <param name="values">The row values. Values are assigned to columns one through N.</param>
    /// <returns>A heatmap row.</returns>
    public static ChartHeatmapRow Create(string name, params double[] values) => new(name, values);

    /// <summary>
    /// Creates a heatmap row with optional masked cells. Null cells reserve their column position but do not render.
    /// </summary>
    /// <param name="name">The row name.</param>
    /// <param name="values">The row cells. Null entries are skipped while later values keep their one-based column positions.</param>
    /// <returns>A masked heatmap row.</returns>
    public static ChartHeatmapRow CreateMasked(string name, params double?[] values) => new(name, values, true);

    private static IEnumerable<double?> ToNullableValues(IEnumerable<double> values) {
        if (values == null) throw new ArgumentNullException(nameof(values));
        foreach (var value in values) yield return value;
    }
}
