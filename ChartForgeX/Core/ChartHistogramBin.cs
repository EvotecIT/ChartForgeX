using System;
using System.Collections.Generic;

namespace ChartForgeX.Core;

/// <summary>Describes one immutable histogram aggregate and the source observations assigned to its interval.</summary>
public sealed class ChartHistogramBin {
    internal ChartHistogramBin(ChartHistogramBinLayout layout, int index, double? value, double renderedValue, int[] sourceIndices) {
        Layout = layout; Index = index; Value = value; RenderedValue = renderedValue;
        SourceIndices = Array.AsReadOnly((int[])sourceIndices.Clone());
    }

    /// <summary>Gets the shared immutable interval layout.</summary>
    public ChartHistogramBinLayout Layout { get; }
    /// <summary>Gets the bin index in the layout.</summary>
    public int Index { get; }
    /// <summary>Gets the inclusive lower measurement bound.</summary>
    public double LowerBound => Layout.GetLowerBound(Index);
    /// <summary>Gets the excluded upper bound, or the included upper bound for the final bin.</summary>
    public double UpperBound => Layout.GetUpperBound(Index);
    /// <summary>Gets the actual measurement width.</summary>
    public double Width => Layout.GetWidth(Index);
    /// <summary>Gets the measurement midpoint.</summary>
    public double Center => Layout.GetCenter(Index);
    /// <summary>Gets the number of assigned source observations.</summary>
    public int Count => SourceIndices.Count;
    /// <summary>Gets the raw aggregate, or null for the mean of an empty bin.</summary>
    public double? Value { get; }
    /// <summary>Gets the encoded height in value or density units; an undefined aggregate remains at the baseline.</summary>
    public double RenderedValue { get; }
    /// <summary>Gets the original source indices in source order.</summary>
    public IReadOnlyList<int> SourceIndices { get; }
}
