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
        ChartGuards.Finite(x, nameof(x));
        if (samples == null) throw new ArgumentNullException(nameof(samples));
        var source = samples.ToArray();
        series.ReplaceBoxPlots(new[] { ChartBoxPlotStatistics.FromValues(x, source) });
        series.SetBoxPlotSourceSamples(source);
        return series;
    }

    /// <summary>Creates a trend line while retaining the original observations used for the regression.</summary>
    public static ChartSeries CreateTrendLine(ChartSeriesId id, string name, IEnumerable<ChartPoint> observations, ChartColor? color = null) =>
        TrendLine(new ChartSeries(id, name, ChartSeriesKind.TrendLine, Array.Empty<ChartPoint>()) { Color = color }, observations);

    internal static ChartSeries CreateTrendLine(string name, IEnumerable<ChartPoint> observations, ChartColor? color) =>
        TrendLine(new ChartSeries(name, ChartSeriesKind.TrendLine, Array.Empty<ChartPoint>()) { Color = color }, observations);

    private static ChartSeries TrendLine(ChartSeries series, IEnumerable<ChartPoint> observations) {
        var source = ChartGuards.Points(observations, nameof(observations));
        if (source.Count < 2) throw new ArgumentException("Trend lines require at least two points.", nameof(observations));
        var sumX = 0d; var sumY = 0d; var sumXY = 0d; var sumXX = 0d;
        var minX = double.PositiveInfinity; var maxX = double.NegativeInfinity;
        foreach (var point in source) {
            sumX += point.X; sumY += point.Y; sumXY += point.X * point.Y; sumXX += point.X * point.X;
            minX = Math.Min(minX, point.X); maxX = Math.Max(maxX, point.X);
        }
        var denominator = source.Count * sumXX - sumX * sumX;
        if (Math.Abs(denominator) < .000001) throw new ArgumentException("Trend lines require at least two distinct x values.", nameof(observations));
        var slope = (source.Count * sumXY - sumX * sumY) / denominator;
        var intercept = (sumY - slope * sumX) / source.Count;
        series.Points.Add(new ChartPoint(minX, slope * minX + intercept));
        series.Points.Add(new ChartPoint(maxX, slope * maxX + intercept));
        series.SetTrendLineSourcePoints(source);
        return series;
    }
}
