using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using PowerForge.Fluent;
using System;

namespace ChartForgeX.Core;

/// <summary>Hand-written fluent methods of <see cref="ChartAxisGridStyle"/> that do more than set one property.</summary>
public static partial class ChartAxisGridStyleFluent {
    /// <summary>Sets dash and gap lengths. A zero dash draws solid lines even when the chart grid is dashed.</summary>
    public static global::ChartForgeX.Core.ChartAxisGridStyle Dash(this global::ChartForgeX.Core.ChartAxisGridStyle chartAxisGridStyle, double dash, double gap)
        => (chartAxisGridStyle ?? throw new System.ArgumentNullException(nameof(chartAxisGridStyle))).SetDash(dash, gap);
}
