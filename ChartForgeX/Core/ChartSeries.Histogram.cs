using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Gets the histogram's immutable measurement layout, or null for an ordinary series.</summary>
    public ChartHistogramBinLayout? HistogramBinLayout { get; private set; }

    /// <summary>Gets the histogram aggregation, or null for an ordinary series.</summary>
    public ChartHistogramAggregation? HistogramAggregation { get; private set; }

    /// <summary>Gets the histogram height encoding, or null for an ordinary series.</summary>
    public ChartHistogramEncoding? HistogramEncoding { get; private set; }

    /// <summary>Gets immutable aggregates in layout order, including empty intervals.</summary>
    public IReadOnlyList<ChartHistogramBin> HistogramBins { get; private set; } = Array.Empty<ChartHistogramBin>();

    /// <summary>Gets explicit source weights in observation order. Empty means the original point/scalar input uses unit weights.</summary>
    /// <remarks>Each bin's SourceIndices indexes this detached collection and HistogramSourcePoints. Only WeightedMean consumes weights.</remarks>
    public IReadOnlyList<double> HistogramSourceWeights { get; private set; } = Array.Empty<double>();

    internal bool IsHistogramDensity => HistogramEncoding == ChartHistogramEncoding.Density || HistogramEncoding == ChartHistogramEncoding.ProbabilityDensity;
    internal bool IsHistogramProbability => HistogramEncoding == ChartHistogramEncoding.Probability || HistogramEncoding == ChartHistogramEncoding.ProbabilityDensity;
    internal double RenderedPointValue(int index) => HistogramBinLayout == null ? Points[index].Y : HistogramBins[index].RenderedValue;

    internal void SetHistogram(ChartHistogramBinLayout layout, ChartHistogramAggregation aggregation, ChartHistogramEncoding encoding,
        ChartHistogramBin[] bins, ChartPoint[] observations, double[]? weights = null) {
        HistogramBinLayout = layout; HistogramAggregation = aggregation; HistogramEncoding = encoding;
        HistogramBins = Array.AsReadOnly(bins);
        HistogramSourcePoints = Array.AsReadOnly((ChartPoint[])observations.Clone());
        HistogramSourceWeights = weights == null ? Array.Empty<double>() : Array.AsReadOnly((double[])weights.Clone());
        SourcePointCount = observations.Length;
    }
}
