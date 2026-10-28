using System;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.Effects;

/// <summary>Immutable finite inset alpha shadow preserving the original source's viewport and alpha.</summary>
/// <remarks>Positive spread thickens the inset by eroding source alpha before the existing Gaussian boxes.
/// The complementary shifted field is tinted SourceAtop the original source in gamma-sRGB16.
/// Source exterior alpha is zero, so complementary exterior coverage is one.</remarks>
public sealed class VisualInnerShadow {
    /// <summary>Creates an explicit inner shadow; ordinary source and theme defaults are unchanged.</summary>
    /// <param name="color">Resolved literal tint, whose alpha multiplies complementary coverage.</param>
    /// <param name="offsetX">Signed horizontal logical-pixel field offset.</param>
    /// <param name="offsetY">Signed vertical logical-pixel field offset.</param>
    /// <param name="sigmaX">Logical horizontal Gaussian target; at most 128 device pixels.</param>
    /// <param name="sigmaY">Optional vertical target; null uses sigmaX.</param>
    /// <param name="spread">Signed logical inset radius; positive erodes source alpha and thickens the inset.</param>
    /// <param name="orderedDither">Uses the existing final RGB quantizer; source alpha stays exact.</param>
    /// <param name="opacity">Finite 0..1 raw tint multiplier applied before final byte conversion.</param>
    public VisualInnerShadow(ChartColor color, int offsetX, int offsetY, double sigmaX, double? sigmaY = null,
        int spread = 0, bool orderedDither = true, double opacity = 1) {
        _ = RasterEffectPlan.SigmaQ8(sigmaX, nameof(sigmaX));
        _ = RasterEffectPlan.SigmaQ8(sigmaY ?? sigmaX, nameof(sigmaY));
        if (spread == int.MinValue) throw new ArgumentOutOfRangeException(nameof(spread));
        if (double.IsNaN(opacity) || double.IsInfinity(opacity) || opacity < 0 || opacity > 1)
            throw new ArgumentOutOfRangeException(nameof(opacity));
        Color = color;
        OffsetX = offsetX;
        OffsetY = offsetY;
        SigmaX = sigmaX;
        SigmaY = sigmaY ?? sigmaX;
        Spread = spread;
        OrderedDither = orderedDither;
        Opacity = opacity;
    }

    /// <summary>Gets the resolved literal tint.</summary>
    public ChartColor Color { get; }
    /// <summary>Gets the signed horizontal logical offset.</summary>
    public int OffsetX { get; }
    /// <summary>Gets the signed vertical logical offset.</summary>
    public int OffsetY { get; }
    /// <summary>Gets the horizontal logical Gaussian target.</summary>
    public double SigmaX { get; }
    /// <summary>Gets the vertical logical Gaussian target.</summary>
    public double SigmaY { get; }
    /// <summary>Gets signed inset spread; positive erodes the source field.</summary>
    public int Spread { get; }
    /// <summary>Gets the final ordered RGB dither policy.</summary>
    public bool OrderedDither { get; }
    /// <summary>Gets raw tint opacity, independent of byte color alpha.</summary>
    public double Opacity { get; }

    internal int HorizontalQ8(int density) => RasterEffectPlan.SigmaQ8(SigmaX, nameof(SigmaX), density);
    internal int VerticalQ8(int density) => RasterEffectPlan.SigmaQ8(SigmaY, nameof(SigmaY), density);
    internal int DeviceSpread(int density) {
        var radius = checked(Spread * density);
        if (radius == int.MinValue) throw new ArgumentOutOfRangeException(nameof(Spread));
        return radius;
    }
}
