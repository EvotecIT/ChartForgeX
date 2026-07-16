using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a histogram series by binning raw numeric values.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="values">The raw numeric values to bin.</param>
    /// <param name="binCount">The number of histogram bins.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddHistogram(string name, IEnumerable<double> values, int binCount = 10, ChartColor? color = null) {
        if (values == null) throw new ArgumentNullException(nameof(values));
        if (binCount < 1) throw new ArgumentOutOfRangeException(nameof(binCount), binCount, "Histogram bin count must be at least one.");

        var materialized = values.ToArray();
        ValidateHistogramValues(materialized, nameof(values));
        return AddHistogramCore(name, materialized, ChartHistogramBinLayout.FromCount(materialized.Min(), materialized.Max(), binCount), color);
    }

    /// <summary>
    /// Adds a histogram series using a reusable layout, allowing multiple series to share exact bin bounds.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="values">The raw numeric values to bin.</param>
    /// <param name="layout">The shared bin layout.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddHistogram(string name, IEnumerable<double> values, ChartHistogramBinLayout layout, ChartColor? color = null) {
        if (values == null) throw new ArgumentNullException(nameof(values));
        if (layout == null) throw new ArgumentNullException(nameof(layout));

        var materialized = values.ToArray();
        ValidateHistogramValues(materialized, nameof(values));
        return AddHistogramCore(name, materialized, layout, color);
    }

    private Chart AddHistogramCore(string name, double[] values, ChartHistogramBinLayout layout, ChartColor? color) {
        var counts = new int[layout.Count];
        foreach (var value in values) counts[layout.GetIndex(value)]++;

        var points = new List<ChartPoint>(layout.Count);
        Options.XAxisLabels.Clear();
        for (var i = 0; i < layout.Count; i++) {
            var start = layout.GetLowerBound(i);
            var end = layout.GetUpperBound(i);
            var center = layout.GetCenter(i);
            points.Add(new ChartPoint(center, counts[i]));
            var label = layout.Minimum == layout.Maximum
                ? FormatHistogramNumber(start)
                : FormatHistogramNumber(start) + "-" + FormatHistogramNumber(end);
            Options.XAxisLabels.Add(new ChartAxisLabel(center, label));
        }

        return Add(name, ChartSeriesKind.Bar, points, color);
    }

    private static void ValidateHistogramValues(double[] values, string parameterName) {
        if (values.Length == 0) throw new ArgumentException("Histogram values must contain at least one value.", parameterName);
        for (var i = 0; i < values.Length; i++) ChartGuards.Finite(values[i], parameterName);
    }

    private static string FormatHistogramNumber(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
