using PowerForge.Fluent;
using System;

namespace ChartForgeX.Core;

/// <summary>Hand-written fluent methods of <see cref="ChartGridLineStyle"/> that do more than set one property.</summary>
public static partial class ChartGridLineStyleFluent {
    /// <summary>
    /// Sets the dash and gap lengths. Use zero dash for solid lines.
    /// </summary>
    public static global::ChartForgeX.Core.ChartGridLineStyle Dash(this global::ChartForgeX.Core.ChartGridLineStyle chartGridLineStyle, double dash, double gap)
        => (chartGridLineStyle ?? throw new System.ArgumentNullException(nameof(chartGridLineStyle))).SetDash(dash, gap);
}
