using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Creates histogram bins while retaining the original measurement and quantity observations.</summary>
    public static ChartSeries CreateHistogram(ChartSeriesId id, string name, IEnumerable<ChartPoint> observations,
        ChartHistogramBinLayout layout, ChartHistogramAggregation aggregation,
        ChartHistogramEncoding encoding = ChartHistogramEncoding.Value, ChartColor? color = null) =>
        Histogram(new ChartSeries(id, name, ChartSeriesKind.Bar, Array.Empty<ChartPoint>()) { Color = color }, observations, layout, aggregation, encoding);

    /// <summary>Creates histogram counts from measurements using explicit reusable bin bounds.</summary>
    public static ChartSeries CreateHistogram(ChartSeriesId id, string name, IEnumerable<double> values,
        ChartHistogramBinLayout layout, ChartHistogramEncoding encoding = ChartHistogramEncoding.Value, ChartColor? color = null) {
        if (values == null) throw new ArgumentNullException(nameof(values));
        return CreateHistogram(id, name, values.Select(value => new ChartPoint(value, 1)), layout, ChartHistogramAggregation.Count, encoding, color);
    }

    internal static ChartSeries CreateHistogram(string name, IEnumerable<ChartPoint> observations,
        ChartHistogramBinLayout layout, ChartHistogramAggregation aggregation, ChartHistogramEncoding encoding, ChartColor? color) =>
        Histogram(new ChartSeries(name, ChartSeriesKind.Bar, Array.Empty<ChartPoint>()) { Color = color }, observations, layout, aggregation, encoding);

    private static ChartSeries Histogram(ChartSeries series, IEnumerable<ChartPoint> observations,
        ChartHistogramBinLayout layout, ChartHistogramAggregation aggregation, ChartHistogramEncoding encoding) {
        if (observations == null) throw new ArgumentNullException(nameof(observations));
        if (layout == null) throw new ArgumentNullException(nameof(layout));
        var source = observations.ToArray();
        var bins = ChartHistogramAggregator.Aggregate(source, layout, aggregation, encoding);
        foreach (var bin in bins) series.Points.Add(new ChartPoint(bin.Center, bin.Value ?? 0));
        series.SetHistogram(layout, aggregation, encoding, bins, source);
        return series;
    }

    /// <summary>Creates one box plot from raw samples, preserving those samples alongside the calculated summary.</summary>
    public static ChartSeries CreateBoxPlot(ChartSeriesId id, string name, double x, IEnumerable<double> samples, ChartColor? color = null) =>
        BoxPlotSamples(new ChartSeries(id, name, ChartSeriesKind.BoxPlot, Array.Empty<ChartPoint>()) { Color = color }, x, samples);

    internal static ChartSeries CreateBoxPlot(string name, double x, IEnumerable<double> samples, ChartColor? color) =>
        BoxPlotSamples(new ChartSeries(name, ChartSeriesKind.BoxPlot, Array.Empty<ChartPoint>()) { Color = color }, x, samples);

    private static ChartSeries BoxPlotSamples(ChartSeries series, double x, IEnumerable<double> samples) {
        return series.ReplaceBoxPlotSampleGroups(new[] { new ChartBoxPlotSampleGroup(x, samples) });
    }

    /// <summary>Creates a trend line while retaining the original observations used for the regression.</summary>
    public static ChartSeries CreateTrendLine(ChartSeriesId id, string name, IEnumerable<ChartPoint> observations, ChartColor? color = null) =>
        TrendLine(new ChartSeries(id, name, ChartSeriesKind.TrendLine, Array.Empty<ChartPoint>()) { Color = color }, observations);

    internal static ChartSeries CreateTrendLine(string name, IEnumerable<ChartPoint> observations, ChartColor? color) =>
        TrendLine(new ChartSeries(name, ChartSeriesKind.TrendLine, Array.Empty<ChartPoint>()) { Color = color }, observations);

    private static ChartSeries TrendLine(ChartSeries series, IEnumerable<ChartPoint> observations) {
        var source = ChartGuards.Points(observations, nameof(observations));
        if (source.Count < 2) throw new ArgumentException("Trend lines require at least two points.", nameof(observations));
        var minX = double.PositiveInfinity; var maxX = double.NegativeInfinity;
        foreach (var point in source) {
            minX = Math.Min(minX, point.X); maxX = Math.Max(maxX, point.X);
        }
        if (minX == maxX) throw new ArgumentException("Trend lines require at least two distinct x values.", nameof(observations));
        var span = maxX - minX;
        ChartGuards.Finite(span, nameof(observations));
        // Center before multiplying, and normalize X so timestamps and small coordinate units
        // do not lose their variance in a subtraction of large, nearly equal squared sums.
        var originY = source[0].Y; var meanX = 0d; var meanY = 0d;
        foreach (var point in source) {
            meanX += (point.X - minX) / span / source.Count;
            meanY += (point.Y - originY) / source.Count;
        }
        var covariance = 0d; var variance = 0d;
        foreach (var point in source) {
            var x = (point.X - minX) / span - meanX;
            covariance += x * (point.Y - originY - meanY);
            variance += x * x;
        }
        var slope = covariance / variance;
        // Evaluate in centered coordinates as well; slope * absoluteX + intercept can
        // discard the fitted variation even after a stable regression calculation.
        series.Points.Add(new ChartPoint(minX, originY + (meanY - slope * meanX)));
        series.Points.Add(new ChartPoint(maxX, originY + (meanY + slope * (1 - meanX))));
        series.SetTrendLineSourcePoints(source);
        return series;
    }
}
