using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Creates common <see cref="ChartPoint"/> sequences for chart series.
/// </summary>
public static class ChartPoints {
    /// <summary>
    /// Creates one-based x/y points from y values.
    /// </summary>
    /// <param name="values">The y values.</param>
    /// <returns>Chart points with x values starting at one.</returns>
    public static ChartPoint[] FromValues(params double[] values) => FromValues((IEnumerable<double>)values);

    /// <summary>
    /// Creates one-based x/y points from y values.
    /// </summary>
    /// <param name="values">The y values.</param>
    /// <returns>Chart points with x values starting at one.</returns>
    public static ChartPoint[] FromValues(IEnumerable<double> values) {
        if (values is null) throw new ArgumentNullException(nameof(values));
        var array = values.ToArray();
        if (array.Length == 0) throw new ArgumentException("Chart values cannot be empty.", nameof(values));

        var points = new ChartPoint[array.Length];
        for (var i = 0; i < array.Length; i++) {
            points[i] = new ChartPoint(i + 1, array[i]);
        }

        return points;
    }

    /// <summary>
    /// Creates x/y points from matching x and y value sequences.
    /// </summary>
    /// <param name="x">The x values.</param>
    /// <param name="y">The y values.</param>
    /// <returns>Chart points using the supplied x and y values.</returns>
    public static ChartPoint[] FromXY(IEnumerable<double> x, IEnumerable<double> y) {
        if (x is null) throw new ArgumentNullException(nameof(x));
        if (y is null) throw new ArgumentNullException(nameof(y));

        var xValues = x.ToArray();
        var yValues = y.ToArray();
        if (xValues.Length == 0) throw new ArgumentException("X values cannot be empty.", nameof(x));
        if (xValues.Length != yValues.Length) throw new ArgumentException("X and Y values must contain the same number of items.", nameof(y));

        var points = new ChartPoint[xValues.Length];
        for (var i = 0; i < xValues.Length; i++) {
            points[i] = new ChartPoint(xValues[i], yValues[i]);
        }

        return points;
    }
}
