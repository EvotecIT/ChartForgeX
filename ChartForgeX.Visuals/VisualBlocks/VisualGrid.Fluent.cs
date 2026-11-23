using ChartForgeX.Accessibility;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using PowerForge.Fluent;
using System.Collections.Generic;
using System.Globalization;
using System;

namespace ChartForgeX.VisualBlocks;

/// <summary>Hand-written fluent methods of <see cref="VisualGrid"/> that do more than set one property.</summary>
public static partial class VisualGridFluent {
    /// <summary>Sets a fixed panel size.</summary>
    public static global::ChartForgeX.VisualBlocks.VisualGrid PanelSize(this global::ChartForgeX.VisualBlocks.VisualGrid visualGrid, int width, int height)
        => (visualGrid ?? throw new System.ArgumentNullException(nameof(visualGrid))).SetPanelSize(width, height);
}
