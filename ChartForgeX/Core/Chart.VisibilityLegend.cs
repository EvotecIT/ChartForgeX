using System;
using System.Collections.Generic;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    // Set only on a freshly reconstructed private view and consumed synchronously by existing native preparation.
    // VisualLegendEntry.Marker delegates are never stored in the immutable source/capture.
    private Func<VisualRenderContext, IReadOnlyList<VisualLegendEntry>>? _visibilityLegendEntries;
    internal void UseVisibilitySourceLegend(ChartVisibilitySource source) => _visibilityLegendEntries = source.LegendEntries;
}
