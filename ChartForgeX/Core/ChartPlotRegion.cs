using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>A passive, solid background rectangle bounded by Cartesian data coordinates.</summary>
/// <remarks>Null bounds use the final axis domain. Regions never expand that domain. Reversal changes
/// their painted position without changing authored bounds. Time coordinates use OLE Automation dates.
/// Bounds follow physical X and Y: horizontal charts use value coordinates for X and linear category slots for Y.</remarks>
public sealed class ChartPlotRegion {
    /// <summary>Creates a region. A missing bound extends to that end of the selected axis domain.</summary>
    /// <param name="color">The solid background color.</param>
    /// <param name="xMinimum">The inclusive lower X bound, or null for the domain minimum.</param>
    /// <param name="xMaximum">The inclusive upper X bound, or null for the domain maximum.</param>
    /// <param name="yMinimum">The inclusive lower Y bound, or null for the domain minimum.</param>
    /// <param name="yMaximum">The inclusive upper Y bound, or null for the domain maximum.</param>
    /// <param name="label">An optional interior caption retained in semantic metadata.</param>
    /// <param name="opacity">The fill opacity from zero through one.</param>
    /// <param name="yAxis">The Y axis that owns the region's coordinates.</param>
    /// <param name="showLabel">Whether to paint the caption when it fits within the visible region.</param>
    public ChartPlotRegion(ChartColor color, double? xMinimum = null, double? xMaximum = null,
        double? yMinimum = null, double? yMaximum = null, string label = "", double opacity = .14,
        ChartAxisSide yAxis = ChartAxisSide.Primary, bool showLabel = true) {
        ValidateBounds(xMinimum, xMaximum, nameof(xMinimum), nameof(xMaximum));
        ValidateBounds(yMinimum, yMaximum, nameof(yMinimum), nameof(yMaximum));
        ChartGuards.UnitInterval(opacity, nameof(opacity));
        if (!Enum.IsDefined(typeof(ChartAxisSide), yAxis)) throw new ArgumentOutOfRangeException(nameof(yAxis));
        Color = color; XMinimum = xMinimum; XMaximum = xMaximum; YMinimum = yMinimum; YMaximum = yMaximum;
        Label = label ?? throw new ArgumentNullException(nameof(label)); Opacity = opacity; YAxis = yAxis; ShowLabel = showLabel;
    }

    /// <summary>Gets the solid fill color.</summary>
    public ChartColor Color { get; }
    /// <summary>Gets the authored lower X bound, or null for the domain minimum.</summary>
    public double? XMinimum { get; }
    /// <summary>Gets the authored upper X bound, or null for the domain maximum.</summary>
    public double? XMaximum { get; }
    /// <summary>Gets the authored lower Y bound, or null for the domain minimum.</summary>
    public double? YMinimum { get; }
    /// <summary>Gets the authored upper Y bound, or null for the domain maximum.</summary>
    public double? YMaximum { get; }
    /// <summary>Gets the original caption, including when hidden, shortened or outside the domain.</summary>
    public string Label { get; }
    /// <summary>Gets the fill opacity.</summary>
    public double Opacity { get; }
    /// <summary>Gets the Y axis that owns these data coordinates.</summary>
    public ChartAxisSide YAxis { get; }
    /// <summary>Gets whether to paint an interior caption when space permits.</summary>
    public bool ShowLabel { get; }

    private static void ValidateBounds(double? minimum, double? maximum, string minimumName, string maximumName) {
        if (minimum.HasValue) ChartGuards.Finite(minimum.Value, minimumName);
        if (maximum.HasValue) ChartGuards.Finite(maximum.Value, maximumName);
        if (minimum.HasValue && maximum.HasValue && minimum.Value >= maximum.Value)
            throw new ArgumentOutOfRangeException(maximumName, "Region maximum must be greater than its minimum.");
    }
}
