using System;
using System.Collections.Generic;
using ChartForgeX.Core;

namespace ChartForgeX.Data;

/// <summary>Represents one numeric interval in a shared layout and its source rows.</summary>
public sealed class ChartDataBin<T> {
    internal ChartDataBin(ChartHistogramBinLayout layout, int index, ChartDataset<T> dataset, double[] measurements, int[] sourceIndices,
        object sourceIdentity, int sourceCount) {
        Layout = layout; Index = index; Dataset = dataset;
        Measurements = Array.AsReadOnly(measurements);
        SourceIndices = Array.AsReadOnly(sourceIndices);
        SourceIdentity = sourceIdentity; SourceCount = sourceCount;
    }

    /// <summary>Gets the immutable measurement layout shared by every bin of the source dataset.</summary>
    public ChartHistogramBinLayout Layout { get; }

    /// <summary>Gets the interval index in the shared layout.</summary>
    public int Index { get; }

    /// <summary>Gets the inclusive lower bound.</summary>
    public double LowerBound => Layout.GetLowerBound(Index);

    /// <summary>Gets the upper bound. The final bin includes its upper bound.</summary>
    public double UpperBound => Layout.GetUpperBound(Index);

    /// <summary>Gets the midpoint used for chart coordinates.</summary>
    public double Center => Layout.GetCenter(Index);

    /// <summary>Gets the actual measurement width.</summary>
    public double Width => Layout.GetWidth(Index);

    /// <summary>Gets the number of source rows in the bin.</summary>
    public int Count => Dataset.Count;

    /// <summary>Gets the source rows in the bin.</summary>
    public ChartDataset<T> Dataset { get; }

    /// <summary>Gets the indices of the rows in the dataset before binning, in source order.</summary>
    public IReadOnlyList<int> SourceIndices { get; }

    internal IReadOnlyList<double> Measurements { get; }
    internal object SourceIdentity { get; }
    internal int SourceCount { get; }
}
