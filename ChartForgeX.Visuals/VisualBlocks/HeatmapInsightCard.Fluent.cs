using ChartForgeX.Core;
using ChartForgeX.Primitives;
using PowerForge.Fluent;
using System.Collections.Generic;
using System;

namespace ChartForgeX.VisualBlocks;

/// <summary>Hand-written fluent methods of <see cref="HeatmapInsightCard"/> that do more than set one property.</summary>
public static partial class HeatmapInsightCardFluent {
    /// <summary>Sets heatmap column labels.</summary>
    public static global::ChartForgeX.VisualBlocks.HeatmapInsightCard Columns(this global::ChartForgeX.VisualBlocks.HeatmapInsightCard heatmapInsightCard, params string[] columns)
        => (heatmapInsightCard ?? throw new System.ArgumentNullException(nameof(heatmapInsightCard))).SetColumns(columns);
}
