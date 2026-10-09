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

    internal bool IsHistogramDensity => HistogramEncoding == ChartHistogramEncoding.Density;
    internal double RenderedPointValue(int index) => HistogramBinLayout == null ? Points[index].Y : HistogramBins[index].RenderedValue;

    internal void SetHistogram(ChartHistogramBinLayout layout, ChartHistogramAggregation aggregation, ChartHistogramEncoding encoding,
        ChartHistogramBin[] bins, ChartPoint[] observations) {
        HistogramBinLayout = layout; HistogramAggregation = aggregation; HistogramEncoding = encoding;
        HistogramBins = Array.AsReadOnly(bins);
        HistogramSourcePoints = Array.AsReadOnly((ChartPoint[])observations.Clone());
        SourcePointCount = observations.Length;
    }
}
