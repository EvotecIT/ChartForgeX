using ChartForgeX.Core;
using ChartForgeX.Primitives;
using PowerForge.Fluent;
using System.Collections.Generic;
using System;

namespace ChartForgeX.VisualBlocks;

/// <summary>Hand-written fluent methods of <see cref="SegmentedMetricItem"/> that do more than set one property.</summary>
public static partial class SegmentedMetricItemFluent {
    /// <summary>Sets optional display text used instead of a derived item value.</summary>
    public static global::ChartForgeX.VisualBlocks.SegmentedMetricItem DisplayValue(this global::ChartForgeX.VisualBlocks.SegmentedMetricItem segmentedMetricItem, object? displayValue, string? format = null)
        => (segmentedMetricItem ?? throw new System.ArgumentNullException(nameof(segmentedMetricItem))).SetDisplayValue(displayValue, format);
}
