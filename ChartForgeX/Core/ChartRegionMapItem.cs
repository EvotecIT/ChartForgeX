using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Represents one region value in a regional map chart.
/// </summary>
public readonly struct ChartRegionMapItem {
    /// <summary>
    /// Gets the region code.
    /// </summary>
    public readonly string Region;

    /// <summary>
    /// Gets the region value.
    /// </summary>
    public readonly double Value;

    /// <summary>
    /// Gets the optional region color.
    /// </summary>
    public readonly ChartColor? Color;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartRegionMapItem"/> struct.
    /// </summary>
    /// <param name="region">The region code or name. Casing is preserved so custom map identifiers can remain case-sensitive.</param>
    /// <param name="value">The region value. Negative values are not allowed.</param>
    /// <param name="color">An optional color for this region.</param>
    public ChartRegionMapItem(string region, double value, ChartColor? color = null) {
        if (string.IsNullOrWhiteSpace(region)) throw new ArgumentException("Region codes must not be empty.", nameof(region));
        ChartGuards.Finite(value, nameof(value));
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Region map values must be zero or greater.");
        Region = region.Trim();
        Value = value;
        Color = color;
    }
}
