using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using PowerForge.Fluent;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ChartForgeX.Core;

/// <summary>Hand-written fluent methods of <see cref="ChartGrid"/> that do more than set one property.</summary>
public static partial class ChartGridFluent {
    /// <summary>
    /// Sets a fixed panel size for composed SVG and PNG grid exports.
    /// </summary>
    /// <param name="chartGrid">The instance to configure.</param>
    /// <param name="width">The panel width in pixels.</param>
    /// <param name="height">The panel height in pixels.</param>
    /// <returns>The current chart grid.</returns>
    public static global::ChartForgeX.Core.ChartGrid PanelSize(this global::ChartForgeX.Core.ChartGrid chartGrid, int width, int height)
        => (chartGrid ?? throw new System.ArgumentNullException(nameof(chartGrid))).SetPanelSize(width, height);
}
