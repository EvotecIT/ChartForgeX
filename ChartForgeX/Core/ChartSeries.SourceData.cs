using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Gets the histogram's immutable measurement/quantity observations in original input order; empty for other series.</summary>
    /// <remarks>
    /// Each <see cref="ChartHistogramBin.SourceIndices"/> member indexes this collection. X is the measurement and Y is the
    /// supplied quantity (one for scalar count input). Derived bin points and this ingestion snapshot are read-only.
    /// Preparation retains detached bin extents, aggregates and contributor identities; original observations remain in this
    /// typed snapshot and do not become individual <see cref="ChartForgeX.Rendering.VisualSemanticRegion"/> entries.
    /// </remarks>
    public IReadOnlyList<ChartPoint> HistogramSourcePoints { get; private set; } = Array.Empty<ChartPoint>();

    /// <summary>Gets the regression's immutable source observations in original input order; empty for other series.</summary>
    /// <remarks>
    /// The collection retains the supplied X/Y values rather than the fitted endpoints in <see cref="Points"/>.
    /// Derived endpoints and this ingestion snapshot are read-only. Preparation captures fitted mark facts;
    /// callers retaining original observations use this typed snapshot rather than semantic region labels.
    /// </remarks>
    public IReadOnlyList<ChartPoint> TrendLineSourcePoints { get; private set; } = Array.Empty<ChartPoint>();

    /// <summary>Gets the raw box plot's immutable sample values in original input order; empty for authored summaries or other series.</summary>
    /// <remarks>
    /// The collection retains unsorted input values rather than the five-number summary in <see cref="BoxPlots"/>.
    /// Updating or replacing a summary clears this provenance; the supplied summary becomes the authored observation.
    /// Preparation captures detached summary mark facts and the sample count; raw samples remain separately inspectable here.
    /// </remarks>
    public IReadOnlyList<double> BoxPlotSourceSamples { get; private set; } = Array.Empty<double>();

    internal void SetTrendLineSourcePoints(IReadOnlyList<ChartPoint> points) {
        var snapshot = new ChartPoint[points.Count];
        for (var index = 0; index < snapshot.Length; index++) snapshot[index] = points[index];
        TrendLineSourcePoints = Array.AsReadOnly(snapshot);
        SourcePointCount = snapshot.Length;
    }
}
