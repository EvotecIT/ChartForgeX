using System;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.Effects;

/// <summary>Immutable outer alpha-band morphology outline of a complete captured source.</summary>
/// <remarks>The square kernel dilates alpha once. Only the new coverage beyond original alpha is tinted,
/// then merged additively; a half-transparent interior is not painted twice. This is not a vector path stroke.</remarks>
public sealed class VisualOutline {
    /// <summary>Creates an outer band in logical coordinates. Zero radius or transparent color is an exact no-op.</summary>
    /// <param name="color">Literal band color; its alpha multiplies only the new outer coverage.</param>
    /// <param name="radius">Finite nonnegative logical radius, mapping to a whole device sample at capture density.</param>
    /// <param name="orderedDither">Uses the existing final RGBA8 Bayer quantizer.</param>
    public VisualOutline(ChartColor color, double radius, bool orderedDither = true) {
        if (double.IsNaN(radius) || double.IsInfinity(radius) || radius < 0) throw new ArgumentOutOfRangeException(nameof(radius));
        Color = color;
        Radius = radius;
        OrderedDither = orderedDither;
    }
    /// <summary>Gets the resolved literal band color.</summary>
    public ChartColor Color { get; }
    /// <summary>Gets the outer logical morphology radius.</summary>
    public double Radius { get; }
    /// <summary>Gets the final native RGBA dither policy.</summary>
    public bool OrderedDither { get; }
    internal int DeviceRadius(int density) => RasterEffectGrid.Coordinate(Radius, density, nameof(Radius));
}
