using ChartForgeX.Accessibility;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using PowerForge.Fluent;
using System.Collections.Generic;
using System.Globalization;
using System;

namespace ChartForgeX.VisualBlocks;

/// <summary>Hand-written fluent methods of <see cref="ChartTable"/> that do more than set one property.</summary>
public static partial class ChartTableFluent {
    /// <summary>Adds columns from header text.</summary>
    public static global::ChartForgeX.VisualBlocks.ChartTable Columns(this global::ChartForgeX.VisualBlocks.ChartTable chartTable, params string[] headers)
        => (chartTable ?? throw new System.ArgumentNullException(nameof(chartTable))).SetColumns(headers);
}
