using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a box plot series from raw sample values.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="x">The horizontal coordinate.</param>
    /// <param name="values">The raw sample values used to compute the five-number summary.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddBoxPlot(string name, double x, IEnumerable<double> values, ChartColor? color = null) {
        ChartGuards.Finite(x, nameof(x));
        if (values == null) throw new ArgumentNullException(nameof(values));
        var source = values.ToArray();
        AddBoxPlot(name, new[] { BoxPlotFromValues(x, source) }, color);
        Series[Series.Count - 1].SetBoxPlotSourceSamples(source);
        return this;
    }

    /// <summary>
    /// Adds a box plot series.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="boxes">The box plot summaries to render.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddBoxPlot(string name, IEnumerable<ChartBoxPlot> boxes, ChartColor? color = null) {
        if (boxes == null) throw new ArgumentNullException(nameof(boxes));
        var points = new List<ChartPoint>();
        foreach (var box in boxes) {
            points.Add(new ChartPoint(box.X, box.Minimum));
            points.Add(new ChartPoint(box.X, box.Q1));
            points.Add(new ChartPoint(box.X, box.Median));
            points.Add(new ChartPoint(box.X, box.Q3));
            points.Add(new ChartPoint(box.X, box.Maximum));
        }

        if (points.Count == 0) throw new ArgumentException("Box plot summaries must contain at least one value.", nameof(boxes));
        return Add(name, ChartSeriesKind.BoxPlot, points, color);
    }

    private static ChartBoxPlot BoxPlotFromValues(double x, IEnumerable<double> values) {
        if (values == null) throw new ArgumentNullException(nameof(values));
        var sorted = values.ToArray();
        if (sorted.Length == 0) throw new ArgumentException("Box plot values must contain at least one value.", nameof(values));
        for (var i = 0; i < sorted.Length; i++) ChartGuards.Finite(sorted[i], nameof(values));
        Array.Sort(sorted);
        return new ChartBoxPlot(
            x,
            sorted[0],
            Quantile(sorted, 0.25),
            Quantile(sorted, 0.50),
            Quantile(sorted, 0.75),
            sorted[sorted.Length - 1]);
    }

    private static double Quantile(IReadOnlyList<double> sorted, double probability) {
        if (sorted.Count == 1) return sorted[0];
        var position = (sorted.Count - 1) * probability;
        var lowerIndex = (int)Math.Floor(position);
        var upperIndex = (int)Math.Ceiling(position);
        if (lowerIndex == upperIndex) return sorted[lowerIndex];
        var fraction = position - lowerIndex;
        return sorted[lowerIndex] + (sorted[upperIndex] - sorted[lowerIndex]) * fraction;
    }
}
