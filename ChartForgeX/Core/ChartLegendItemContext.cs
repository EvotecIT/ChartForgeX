using System;
using System.Collections.Generic;

namespace ChartForgeX.Core;

/// <summary>Describes one legend entry to <see cref="ChartLegendOptions.ItemFormatter"/>.</summary>
public sealed class ChartLegendItemContext {
    private readonly ChartValueFormat _format;
    private IReadOnlyList<double>? _values;

    internal ChartLegendItemContext(string label, ChartSeries? series, int seriesIndex, int? pointIndex,
        ChartLegendItem? item, string? formattedValue, ChartValueFormat format) {
        Label = label; Series = series; SeriesIndex = seriesIndex; PointIndex = pointIndex; Item = item;
        FormattedValue = formattedValue; _format = format;
    }

    /// <summary>Gets the text the legend would show without a formatter.</summary>
    public string Label { get; }

    /// <summary>Gets the series the entry describes, or null for a custom item or a family key without one series.</summary>
    public ChartSeries? Series { get; }

    /// <summary>Gets the zero-based index of <see cref="Series"/>, or -1 when the entry has no series.</summary>
    public int SeriesIndex { get; }

    /// <summary>Gets the zero-based point index when the entry describes one observation, such as a point legend or pie slice.</summary>
    public int? PointIndex { get; }

    /// <summary>Gets the custom item the entry shows, or null for a series entry.</summary>
    public ChartLegendItem? Item { get; }

    /// <summary>Gets the value column text a part-to-whole legend already shows, or null.</summary>
    public string? FormattedValue { get; }

    /// <summary>Gets the finite Y values of the entry: the series' values, or the one observation for a point entry. Empty when the series has no numeric points.</summary>
    public IReadOnlyList<double> Values => _values ??= ResolveValues();

    /// <summary>Gets the sum of <see cref="Values"/>.</summary>
    public double Total { get { var total = 0d; foreach (var value in Values) total += value; return total; } }

    /// <summary>Formats a value with the chart's value format, as data labels and value axes do.</summary>
    /// <param name="value">A finite value.</param>
    public string FormatValue(double value) => _format.Format(value);

    private IReadOnlyList<double> ResolveValues() {
        if (Series == null) return Array.Empty<double>();
        var points = Series.Points;
        var values = new List<double>(PointIndex.HasValue ? 1 : points.Count);
        for (var index = 0; index < points.Count; index++) {
            if (PointIndex.HasValue && index != PointIndex.Value) continue;
            var y = points[index].Y;
            if (!double.IsNaN(y) && !double.IsInfinity(y)) values.Add(y);
        }
        return values.AsReadOnly();
    }
}
