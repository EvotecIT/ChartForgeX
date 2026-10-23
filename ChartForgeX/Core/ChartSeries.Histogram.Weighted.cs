using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Creates histogram aggregates from measurements, signed quantities and independent nonnegative averaging weights.</summary>
    /// <param name="id">The retained series identity.</param>
    /// <param name="name">The displayed series name.</param>
    /// <param name="observations">Source observations. Only WeightedMean consumes Weight; Count counts rows and Sum/Mean use Quantity.</param>
    /// <param name="layout">The shared immutable interval layout.</param>
    /// <param name="aggregation">The quantity to aggregate. A weighted mean with no positive weight is undefined.</param>
    /// <param name="encoding">Raw height, quantity density, probability or probability density. Probability requires Count or nonnegative Sum.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>A histogram retaining detached source quantities and weights.</returns>
    public static ChartSeries CreateHistogram(ChartSeriesId id, string name, IEnumerable<ChartHistogramObservation> observations,
        ChartHistogramBinLayout layout, ChartHistogramAggregation aggregation,
        ChartHistogramEncoding encoding = ChartHistogramEncoding.Value, ChartColor? color = null) =>
        WeightedHistogram(new ChartSeries(id, name, ChartSeriesKind.Bar, Array.Empty<ChartPoint>()) { Color = color }, observations, layout, aggregation, encoding);

    internal static ChartSeries CreateHistogram(string name, IEnumerable<ChartHistogramObservation> observations,
        ChartHistogramBinLayout layout, ChartHistogramAggregation aggregation, ChartHistogramEncoding encoding, ChartColor? color) =>
        WeightedHistogram(new ChartSeries(name, ChartSeriesKind.Bar, Array.Empty<ChartPoint>()) { Color = color }, observations, layout, aggregation, encoding);

    private static ChartSeries WeightedHistogram(ChartSeries series, IEnumerable<ChartHistogramObservation> observations,
        ChartHistogramBinLayout layout, ChartHistogramAggregation aggregation, ChartHistogramEncoding encoding) {
        if (observations == null) throw new ArgumentNullException(nameof(observations));
        if (layout == null) throw new ArgumentNullException(nameof(layout));
        var source = observations.ToArray();
        var points = source.Select(value => new ChartPoint(value.Measurement, value.Quantity)).ToArray();
        var weights = source.Select(value => value.Weight).ToArray();
        var bins = ChartHistogramAggregator.Aggregate(points, layout, aggregation, encoding, weights);
        foreach (var bin in bins) series.Points.Add(new ChartPoint(bin.Center, bin.Value ?? 0));
        series.SetHistogram(layout, aggregation, encoding, bins, points, weights);
        return series;
    }
}
