using System;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    /// <summary>Gets or sets whether the embedding host draws the frame. Removes the chart surface and outer padding.</summary>
    public bool HostOwnsFrame { get; set; }
    /// <summary>Gets or sets whether a Graphite panel uses the compact 15 px title.</summary>
    public bool IsPanel { get; set; }
    /// <summary>Gets or sets optional line-marker visibility. Null uses the theme default (last point for Graphite).</summary>
    public ChartLineMarkerMode? LineMarkerMode { get; set; }
    /// <summary>Gets or sets the maximum displayed pie or donut slices, including an aggregate Other slice.</summary>
    public int MaximumPieSlices { get; set; } = 6;
}

/// <summary>Controls optional markers without removing the full series data or interaction targets.</summary>
public enum ChartLineMarkerMode {
    /// <summary>Draws a marker at each retained point.</summary>
    All,
    /// <summary>Draws only the final marker.</summary>
    Last,
    /// <summary>Draws no optional markers.</summary>
    None
}
