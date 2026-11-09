using System;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

/// <summary>Styles the grid lines and alternating bands drawn at one Cartesian axis's ticks.</summary>
/// <remarks>Unset values inherit the chart's <see cref="ChartGridLineStyle"/> and the theme grid colour. The chart's
/// <c>ShowGrid</c> switch still hides every grid. Minor lines follow <see cref="ChartAxis.MinorTickCount"/>.</remarks>
public sealed class ChartAxisGridStyle {
    private double? _opacity;
    private double? _strokeWidth;
    private double? _dash;
    private double? _gap;
    private double? _minorOpacity;
    private double _bandOpacity = .06;

    /// <summary>Gets or sets whether this axis draws grid lines, or null to follow the chart's horizontal and vertical line policy.</summary>
    public bool? ShowLines { get; set; }

    /// <summary>Gets or sets the line colour, or null for the theme grid colour.</summary>
    public ChartColor? Color { get; set; }

    /// <summary>Gets or sets major line opacity from zero to one, or null for the chart policy.</summary>
    public double? Opacity { get => _opacity; set { if (value.HasValue) ChartGuards.UnitInterval(value.Value, nameof(value)); _opacity = value; } }

    /// <summary>Gets or sets the line width in drawing units, or null for the chart policy.</summary>
    public double? StrokeWidth {
        get => _strokeWidth;
        set {
            if (value.HasValue && (!ChartMath.IsFinite(value.Value) || value.Value <= 0))
                throw new ArgumentOutOfRangeException(nameof(value), value, "Grid stroke width must be finite and greater than zero.");
            _strokeWidth = value;
        }
    }

    /// <summary>Gets the dash length, or null for the chart policy. Zero draws solid lines.</summary>
    public double? Dash => _dash;

    /// <summary>Gets the gap between dashes, or null for the chart policy.</summary>
    public double? Gap => _gap;

    /// <summary>Gets or sets minor line opacity, or null for half the major line opacity.</summary>
    public double? MinorOpacity { get => _minorOpacity; set { if (value.HasValue) ChartGuards.UnitInterval(value.Value, nameof(value)); _minorOpacity = value; } }

    /// <summary>Gets or sets the fill of every other interval between adjacent major ticks, or null for no bands.</summary>
    /// <remarks>The first interval after the lowest visible tick is filled, then every second one.</remarks>
    public ChartColor? BandColor { get; set; }

    /// <summary>Gets or sets band opacity from zero to one. Default: 0.06.</summary>
    public double BandOpacity { get => _bandOpacity; set { ChartGuards.UnitInterval(value, nameof(value)); _bandOpacity = value; } }

    /// <summary>Shows or hides this axis's grid lines regardless of the chart's line policy; null restores that policy.</summary>
    public ChartAxisGridStyle WithLines(bool? visible = true) { ShowLines = visible; return this; }

    /// <summary>Sets the line colour; null restores the theme grid colour.</summary>
    public ChartAxisGridStyle WithColor(ChartColor? color) { Color = color; return this; }

    /// <summary>Sets major line opacity; null restores the chart policy.</summary>
    public ChartAxisGridStyle WithOpacity(double? opacity) { Opacity = opacity; return this; }

    /// <summary>Sets the line width; null restores the chart policy.</summary>
    public ChartAxisGridStyle WithStrokeWidth(double? width) { StrokeWidth = width; return this; }

    /// <summary>Sets dash and gap lengths. A zero dash draws solid lines even when the chart grid is dashed.</summary>
    public ChartAxisGridStyle WithDash(double dash, double gap) {
        if (!ChartMath.IsFinite(dash) || dash < 0) throw new ArgumentOutOfRangeException(nameof(dash), dash, "Grid dash length must be finite and non-negative.");
        if (!ChartMath.IsFinite(gap) || gap < 0) throw new ArgumentOutOfRangeException(nameof(gap), gap, "Grid dash gap must be finite and non-negative.");
        _dash = dash; _gap = gap; return this;
    }

    /// <summary>Restores the chart's dash policy.</summary>
    public ChartAxisGridStyle WithInheritedDash() { _dash = null; _gap = null; return this; }

    /// <summary>Sets minor line opacity; null uses half the major line opacity.</summary>
    public ChartAxisGridStyle WithMinorOpacity(double? opacity) { MinorOpacity = opacity; return this; }

    /// <summary>Fills every other interval between adjacent major ticks; a null colour removes the bands.</summary>
    public ChartAxisGridStyle WithBands(ChartColor? color, double opacity = .06) { BandOpacity = opacity; BandColor = color; return this; }

    /// <summary>Creates an independent copy.</summary>
    public ChartAxisGridStyle Clone() => (ChartAxisGridStyle)MemberwiseClone();
}
