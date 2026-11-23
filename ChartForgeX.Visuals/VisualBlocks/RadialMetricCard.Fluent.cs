using ChartForgeX.Accessibility;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using PowerForge.Fluent;
using System.Collections.Generic;
using System.Globalization;
using System;

namespace ChartForgeX.VisualBlocks;

/// <summary>Hand-written fluent methods of <see cref="RadialMetricCard"/> that do more than set one property.</summary>
public static partial class RadialMetricCardFluent {
    /// <summary>Replaces all radial layers.</summary>
    public static global::ChartForgeX.VisualBlocks.RadialMetricCard Layers(this global::ChartForgeX.VisualBlocks.RadialMetricCard radialMetricCard, global::System.Collections.Generic.IEnumerable<global::ChartForgeX.Core.ChartRadialLayer> layers)
        => (radialMetricCard ?? throw new System.ArgumentNullException(nameof(radialMetricCard))).SetLayers(layers);

    /// <summary>Replaces all radial layers using a fluent layer collection builder.</summary>
    public static global::ChartForgeX.VisualBlocks.RadialMetricCard Layers(this global::ChartForgeX.VisualBlocks.RadialMetricCard radialMetricCard, global::System.Func<global::ChartForgeX.Core.ChartRadialLayers, global::ChartForgeX.Core.ChartRadialLayers> configure)
        => (radialMetricCard ?? throw new System.ArgumentNullException(nameof(radialMetricCard))).SetLayers(configure);
}
