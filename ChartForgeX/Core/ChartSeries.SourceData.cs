using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Gets the histogram's immutable measurement/quantity observations in original input order; empty for other series.</summary>
    /// <remarks>
    /// Each <see cref="ChartHistogramBin.SourceIndices"/> member indexes this collection. X is the measurement and Y is the
    /// supplied quantity (one for scalar count input). Input and series point edits do not rewrite this ingestion snapshot.
    /// Preparation retains detached bin extents, aggregates and contributor identities; original observations remain in this
    /// typed snapshot and do not become individual <see cref="ChartForgeX.Rendering.VisualSemanticRegion"/> entries.
    /// </remarks>
    public IReadOnlyList<ChartPoint> HistogramSourcePoints { get; private set; } = Array.Empty<ChartPoint>();

    /// <summary>Gets the regression's immutable source observations in original input order; empty for other series.</summary>
    /// <remarks>
    /// The collection retains the supplied X/Y values rather than the fitted endpoints in <see cref="Points"/>.
    /// Input and endpoint edits do not rewrite this ingestion snapshot. Preparation captures fitted mark facts;
    /// callers retaining original observations use this typed snapshot rather than semantic region labels.
    /// </remarks>
    public IReadOnlyList<ChartPoint> TrendLineSourcePoints { get; private set; } = Array.Empty<ChartPoint>();

    /// <summary>Gets the raw box plot's immutable sample values in original input order; empty for authored summaries or other series.</summary>
    /// <remarks>
    /// The collection retains unsorted input values rather than the five-number summary in <see cref="Points"/>.
    /// Input and summary edits do not rewrite this ingestion snapshot. Preparation captures summary mark facts;
    /// callers retaining original samples use this typed snapshot rather than semantic region labels.
    /// </remarks>
    public IReadOnlyList<double> BoxPlotSourceSamples { get; private set; } = Array.Empty<double>();

    internal void SetBoxPlotSourceSamples(IReadOnlyList<double> samples) {
        var snapshot = new double[samples.Count];
        for (var index = 0; index < snapshot.Length; index++) snapshot[index] = samples[index];
        BoxPlotSourceSamples = Array.AsReadOnly(snapshot);
        SourcePointCount = snapshot.Length;
    }

    internal void SetTrendLineSourcePoints(IReadOnlyList<ChartPoint> points) {
        var snapshot = new ChartPoint[points.Count];
        for (var index = 0; index < snapshot.Length; index++) snapshot[index] = points[index];
        TrendLineSourcePoints = Array.AsReadOnly(snapshot);
        SourcePointCount = snapshot.Length;
    }
}
