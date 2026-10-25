using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>A passive, solid background rectangle bounded by Cartesian data coordinates.</summary>
/// <remarks>Null bounds use the final axis domain. Regions never expand that domain. Reversal changes
/// their painted position without changing authored bounds. Time coordinates use OLE Automation dates.
/// Bounds follow physical X and Y: horizontal charts use value coordinates for X and linear category slots for Y.</remarks>
public sealed class ChartPlotRegion {
    /// <summary>Creates a region whose color follows the active render theme's categorical palette.</summary>
    /// <param name="paletteIndex">The zero-based palette index. Larger indices wrap to the active palette length.</param>
    /// <param name="xMinimum">The inclusive lower X bound, or null for the domain minimum.</param>
    /// <param name="xMaximum">The inclusive upper X bound, or null for the domain maximum.</param>
    /// <param name="yMinimum">The inclusive lower Y bound, or null for the domain minimum.</param>
    /// <param name="yMaximum">The inclusive upper Y bound, or null for the domain maximum.</param>
    /// <param name="label">An optional interior caption retained in semantic metadata.</param>
    /// <param name="opacity">The fill opacity from zero through one.</param>
    /// <param name="yAxis">The Y axis that owns the region's coordinates.</param>
    /// <param name="showLabel">Whether to paint the caption when it fits within the visible region.</param>
    public ChartPlotRegion(int paletteIndex, double? xMinimum = null, double? xMaximum = null,
        double? yMinimum = null, double? yMaximum = null, string label = "", double opacity = .14,
        ChartAxisSide yAxis = ChartAxisSide.Primary, bool showLabel = true)
        : this(default(ChartColor), xMinimum, xMaximum, yMinimum, yMaximum, label, opacity, yAxis, showLabel) {
        if (paletteIndex < 0) throw new ArgumentOutOfRangeException(nameof(paletteIndex));
        PaletteIndex = paletteIndex;
        Color = null;
    }

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

    /// <summary>Gets the authored literal fill color, or null when <see cref="PaletteIndex"/> selects a theme color.</summary>
    public ChartColor? Color { get; }
    /// <summary>Gets the categorical palette index resolved at preparation, or null for a literal color.</summary>
    public int? PaletteIndex { get; }
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

    /// <summary>Gets an immutable border override, or null for no border. A missing color or width uses the theme axis stroke.</summary>
    /// <remarks>The border encloses the visible clipped rectangle and paints inward, without expanding the data domain or hit targets.</remarks>
    public ChartStrokeStyle? Border { get; }
    /// <summary>Gets the screen-relative caption anchor. Auto retains center-first collision-aware placement.</summary>
    public ChartPlotRegionLabelPosition LabelPosition { get; }

    /// <summary>Returns a detached definition with the supplied border; null removes the border.</summary>
    /// <param name="border">Immutable color, opacity, width and dash overrides.</param>
    /// <returns>A new region preserving every authored bound, fill and caption.</returns>
    public ChartPlotRegion WithBorder(ChartStrokeStyle? border) => new ChartPlotRegion(this, border, LabelPosition);

    /// <summary>Returns a detached definition with one deliberate caption anchor.</summary>
    /// <param name="position">The screen-relative anchor inside the clipped region. Explicit anchors shorten or omit rather than relocate.</param>
    /// <returns>A new region preserving every authored bound, fill and border.</returns>
    public ChartPlotRegion WithLabelPosition(ChartPlotRegionLabelPosition position) {
        if (!Enum.IsDefined(typeof(ChartPlotRegionLabelPosition), position)) throw new ArgumentOutOfRangeException(nameof(position));
        return new ChartPlotRegion(this, Border, position);
    }

    private ChartPlotRegion(ChartPlotRegion source, ChartStrokeStyle? border, ChartPlotRegionLabelPosition position) {
        Color = source.Color; PaletteIndex = source.PaletteIndex;
        XMinimum = source.XMinimum; XMaximum = source.XMaximum; YMinimum = source.YMinimum; YMaximum = source.YMaximum;
        Label = source.Label; Opacity = source.Opacity; YAxis = source.YAxis; ShowLabel = source.ShowLabel;
        Border = border; LabelPosition = position;
    }

    private static void ValidateBounds(double? minimum, double? maximum, string minimumName, string maximumName) {
        if (minimum.HasValue) ChartGuards.Finite(minimum.Value, minimumName);
        if (maximum.HasValue) ChartGuards.Finite(maximum.Value, maximumName);
        if (minimum.HasValue && maximum.HasValue && minimum.Value >= maximum.Value)
            throw new ArgumentOutOfRangeException(maximumName, "Region maximum must be greater than its minimum.");
    }
}
