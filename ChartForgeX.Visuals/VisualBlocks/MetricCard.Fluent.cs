using ChartForgeX.Core;
using ChartForgeX.Primitives;
using PowerForge.Fluent;
using System.Collections.Generic;
using System;

namespace ChartForgeX.VisualBlocks;

/// <summary>Hand-written fluent methods of <see cref="MetricCard"/> that do more than set one property.</summary>
public static partial class MetricCardFluent {
    /// <summary>Replaces compact bar values rendered inside the metric card.</summary>
    public static global::ChartForgeX.VisualBlocks.MetricCard MiniBars(this global::ChartForgeX.VisualBlocks.MetricCard metricCard, global::System.Collections.Generic.IEnumerable<double> values, double? minimum = null, double? maximum = null, global::ChartForgeX.Primitives.ChartColor? color = null, global::ChartForgeX.Primitives.ChartColor? mutedColor = null, int? highlightIndex = null)
        => (metricCard ?? throw new System.ArgumentNullException(nameof(metricCard))).SetMiniBars(values, minimum, maximum, color, mutedColor, highlightIndex);

    /// <summary>Replaces compact sparkline values rendered inside the metric card.</summary>
    public static global::ChartForgeX.VisualBlocks.MetricCard MiniSparkline(this global::ChartForgeX.VisualBlocks.MetricCard metricCard, global::System.Collections.Generic.IEnumerable<double> values, double? minimum = null, double? maximum = null, global::ChartForgeX.Primitives.ChartColor? color = null, global::ChartForgeX.Primitives.ChartColor? fillColor = null)
        => (metricCard ?? throw new System.ArgumentNullException(nameof(metricCard))).SetMiniSparkline(values, minimum, maximum, color, fillColor);

    /// <summary>Replaces optional secondary sparkline values rendered with the primary sparkline.</summary>
    public static global::ChartForgeX.VisualBlocks.MetricCard SecondaryMiniSparkline(this global::ChartForgeX.VisualBlocks.MetricCard metricCard, global::System.Collections.Generic.IEnumerable<double> values, global::ChartForgeX.Primitives.ChartColor? color = null)
        => (metricCard ?? throw new System.ArgumentNullException(nameof(metricCard))).SetSecondaryMiniSparkline(values, color);
}
