using System;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.Effects;

/// <summary>Immutable alpha shadow for a captured viewport or a retained native surface.</summary>
/// <remarks>Offsets and signed spread use integer logical units, multiplied once by capture density.
/// Positive spread dilates source alpha; negative spread erodes it with transparent exterior samples.
/// Gaussian targets use the existing three-box approximation and one final sRGB RGBA8 conversion.</remarks>
public sealed class VisualDropShadow {
    /// <summary>Creates a literal alpha shadow. No existing scene or theme opts into it automatically.</summary>
    /// <param name="color">Resolved shadow tint; its alpha multiplies captured source alpha once.</param>
    /// <param name="offsetX">Signed horizontal logical-pixel offset.</param>
    /// <param name="offsetY">Signed vertical logical-pixel offset.</param>
    /// <param name="sigmaX">Finite nonnegative horizontal target, at most 128 device pixels after density conversion.</param>
    /// <param name="sigmaY">Optional vertical target; null uses sigmaX.</param>
    /// <param name="spread">Signed integer logical radius; dilation when positive, erosion when negative.</param>
    /// <param name="opacity">Independent finite 0..1 tint opacity; applied once before final byte conversion.</param>
    /// <param name="relativeRegion">Optional positive shadow-only region relative to source geometry, excluding stroke. Original source is retained.</param>
    /// <param name="orderedDither">Whether final RGBA8 conversion uses the existing layer-local Bayer pattern.</param>
    public VisualDropShadow(ChartColor color, int offsetX, int offsetY, double sigmaX, double? sigmaY = null,
        int spread = 0, bool orderedDither = true, double opacity = 1, ChartRect? relativeRegion = null) {
        _ = RasterEffectPlan.SigmaQ8(sigmaX, nameof(sigmaX));
        _ = RasterEffectPlan.SigmaQ8(sigmaY ?? sigmaX, nameof(sigmaY));
        if (spread == int.MinValue) throw new ArgumentOutOfRangeException(nameof(spread));
        if (!Finite(opacity) || opacity < 0 || opacity > 1) throw new ArgumentOutOfRangeException(nameof(opacity));
        if (relativeRegion is ChartRect region && (!Finite(region.X) || !Finite(region.Y) ||
            !Finite(region.Width) || !Finite(region.Height) || region.Width <= 0 || region.Height <= 0)) {
            throw new ArgumentOutOfRangeException(nameof(relativeRegion));
        }
        Opacity = opacity;
        RelativeRegion = relativeRegion;
        Color = color; OffsetX = offsetX; OffsetY = offsetY; SigmaX = sigmaX; SigmaY = sigmaY ?? sigmaX;
        Spread = spread; OrderedDither = orderedDither;
    }
    /// <summary>Gets the literal shadow color.</summary>
    public ChartColor Color { get; }
    /// <summary>Gets the signed horizontal logical offset.</summary>
    public int OffsetX { get; }
    /// <summary>Gets the signed vertical logical offset.</summary>
    public int OffsetY { get; }
    /// <summary>Gets the horizontal logical Gaussian target.</summary>
    public double SigmaX { get; }
    /// <summary>Gets the vertical logical Gaussian target.</summary>
    public double SigmaY { get; }
    /// <summary>Gets the signed logical alpha-spread radius.</summary>
    public int Spread { get; }
    /// <summary>Gets the final ordered-dither policy.</summary>
    public bool OrderedDither { get; }
    /// <summary>Gets the independent finite tint-opacity multiplier. One retains the existing byte-alpha path.</summary>
    public double Opacity { get; }
    /// <summary>Gets optional shadow-only clipping in fractions of the source surface geometry box; null retains propagated support.</summary>
    public ChartRect? RelativeRegion { get; }

    internal ChartRect? ResolveRegion(ChartRect bounds) {
        if (!(RelativeRegion is ChartRect region)) return null;
        return new ChartRect(bounds.X + region.X * bounds.Width, bounds.Y + region.Y * bounds.Height,
            region.Width * bounds.Width, region.Height * bounds.Height);
    }

    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    internal int HorizontalQ8(int density) => RasterEffectPlan.SigmaQ8(SigmaX, nameof(SigmaX), density);
    internal int VerticalQ8(int density) => RasterEffectPlan.SigmaQ8(SigmaY, nameof(SigmaY), density);
}
