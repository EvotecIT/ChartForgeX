using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a histogram series in equal-width bins aligned to a nice decimal step that covers the data.
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
        if (materialized.Length == 0) throw new ArgumentException("Automatic histogram layouts require at least one observation; supply an explicit layout for empty input.", nameof(values));
        return AddHistogram(name, materialized, ChartHistogramBinLayout.FromCount(materialized.Min(), materialized.Max(), binCount), color);
    }

    /// <summary>
    /// Adds a histogram series using a reusable layout, allowing multiple series to share exact bin bounds.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="values">The raw numeric values to bin.</param>
    /// <param name="layout">The shared bin layout.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddHistogram(string name, IEnumerable<double> values, ChartHistogramBinLayout layout, ChartColor? color = null) =>
        AddHistogram(name, values, layout, ChartHistogramEncoding.Value, color);

    /// <summary>Adds observation counts using an explicit layout and value or density heights. Empty input retains every bin.</summary>
    /// <param name="name">The series name.</param>
    /// <param name="values">The finite measurements to bin.</param>
    /// <param name="layout">The immutable measurement intervals; observations outside intervals are rejected.</param>
    /// <param name="encoding">Value heights or count per measurement unit with full-bin rectangular area.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddHistogram(string name, IEnumerable<double> values, ChartHistogramBinLayout layout, ChartHistogramEncoding encoding, ChartColor? color = null) {
        if (values == null) throw new ArgumentNullException(nameof(values));
        return AddHistogram(name, values.Select(value => new ChartPoint(value, 1)), layout, ChartHistogramAggregation.Count, encoding, color);
    }

    /// <summary>Adds histogram aggregates from measurements in X and signed quantities in Y, preserving source observations and empty bins.</summary>
    /// <param name="name">The series name.</param>
    /// <param name="observations">Finite measurement/quantity observations. Sum and Mean use Y; Count counts rows.</param>
    /// <param name="layout">The shared measurement intervals; outside observations and gaps are rejected.</param>
    /// <param name="aggregation">Count, signed Sum, or arithmetic Mean. Empty means are undefined.</param>
    /// <param name="encoding">Value heights or quantity per actual measurement width.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddHistogram(string name, IEnumerable<ChartPoint> observations, ChartHistogramBinLayout layout,
        ChartHistogramAggregation aggregation, ChartHistogramEncoding encoding = ChartHistogramEncoding.Value, ChartColor? color = null) {
        if (observations == null) throw new ArgumentNullException(nameof(observations));
        if (layout == null) throw new ArgumentNullException(nameof(layout));
        EnsureCanAddSeries();
        var source = observations.ToArray();
        var bins = ChartHistogramAggregator.Aggregate(source, layout, aggregation, encoding);
        var points = bins.Select(bin => new ChartPoint(bin.Center, bin.Value ?? 0));

        Add(name, ChartSeriesKind.Bar, points, color);
        Series[Series.Count - 1].SetHistogram(layout, aggregation, encoding, bins, source);
        return this;
    }
}
