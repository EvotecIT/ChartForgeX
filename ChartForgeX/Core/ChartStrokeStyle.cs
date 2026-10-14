using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>Defines immutable stroke overrides for chart outlines, guides and reference lines.</summary>
public sealed class ChartStrokeStyle {
    private readonly double[]? _dash;

    /// <summary>Gets the authored stroke color. Null retains the mark's theme or series color.</summary>
    public ChartColor? Color { get; }

    /// <summary>Gets the stroke width from zero through 1024 chart pixels. Null retains the theme width; zero hides the stroke.</summary>
    public double? Width { get; }

    /// <summary>Gets the opacity multiplier applied to the stroke color's alpha channel.</summary>
    public double Opacity { get; }

    /// <summary>Gets alternating dash/gap lengths from 0.25 through 1024 chart pixels. Null retains the default pattern; an empty list is solid.</summary>
    public IReadOnlyList<double>? Dash { get; }

    /// <summary>Creates stroke overrides without changing the source geometry or caption.</summary>
    /// <param name="color">The optional authored color.</param>
    /// <param name="width">The optional width from zero through 1024 chart pixels.</param>
    /// <param name="opacity">The opacity multiplier from zero through one.</param>
    /// <param name="dash">Optional dash/gap lengths from 0.25 through 1024 chart pixels; an empty sequence selects a solid stroke.</param>
    public ChartStrokeStyle(ChartColor? color = null, double? width = null, double opacity = 1, IEnumerable<double>? dash = null) {
        if (width.HasValue) {
            ChartGuards.Finite(width.Value, nameof(width));
            if (width.Value < 0 || width.Value > 1024) throw new ArgumentOutOfRangeException(nameof(width), width, "Stroke width must be from zero through 1024 chart pixels.");
        }
        ChartGuards.UnitInterval(opacity, nameof(opacity));
        _dash = dash?.ToArray();
        if (_dash != null) foreach (var length in _dash) {
            ChartGuards.Finite(length, nameof(dash));
            if (length < .25 || length > 1024) throw new ArgumentOutOfRangeException(nameof(dash), "Dash lengths must be from 0.25 through 1024 chart pixels.");
        }
        Color = color; Width = width; Opacity = opacity;
        Dash = _dash == null ? null : Array.AsReadOnly(_dash);
    }

    internal double[] ResolveDash(double[] fallback) => _dash ?? fallback;
}
