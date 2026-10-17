using System;
using System.Collections.Generic;

namespace ChartForgeX.Core;

/// <summary>Immutable calculated summary and measured-source provenance for one raw sample group.</summary>
public sealed class ChartBoxPlotDistribution {
    internal ChartBoxPlotDistribution(int index, ChartBoxPlot summary, double sourceMinimum, double sourceMaximum,
        double? inlierMinimum, double? inlierMaximum, int[] sourceIndices, int[] outliers, ChartBoxPlotStatisticsOptions options) {
        Index = index; Summary = summary; SourceMinimum = sourceMinimum; SourceMaximum = sourceMaximum;
        InlierMinimum = inlierMinimum; InlierMaximum = inlierMaximum;
        SourceIndices = Array.AsReadOnly(sourceIndices); OutlierSourceIndices = Array.AsReadOnly(outliers);
        Estimator = options.Estimator; WhiskerPolicy = options.WhiskerPolicy; IqrMultiplier = options.IqrMultiplier;
    }
    /// <summary>Gets the original group ordinal.</summary>
    public int Index { get; }
    /// <summary>Gets drawable ordered whiskers and quartiles.</summary>
    public ChartBoxPlot Summary { get; }
    /// <summary>Gets the number of original measurements.</summary>
    public int Count => SourceIndices.Count;
    /// <summary>Gets the actual observed source minimum, including outliers.</summary>
    public double SourceMinimum { get; }
    /// <summary>Gets the actual observed source maximum, including outliers.</summary>
    public double SourceMaximum { get; }
    /// <summary>Gets the observed inlier minimum, or null when no sample is inside the fences.</summary>
    public double? InlierMinimum { get; }
    /// <summary>Gets the observed inlier maximum, or null when no sample is inside the fences.</summary>
    public double? InlierMaximum { get; }
    /// <summary>Gets flattened original sample indices in source order.</summary>
    public IReadOnlyList<int> SourceIndices { get; }
    /// <summary>Gets flattened outlier sample indices in source order.</summary>
    public IReadOnlyList<int> OutlierSourceIndices { get; }
    /// <summary>Gets the installed quartile estimator.</summary>
    public ChartBoxPlotQuartileEstimator Estimator { get; }
    /// <summary>Gets the installed whisker policy.</summary>
    public ChartBoxPlotWhiskerPolicy WhiskerPolicy { get; }
    /// <summary>Gets the installed multiplier.</summary>
    public double IqrMultiplier { get; }
}
