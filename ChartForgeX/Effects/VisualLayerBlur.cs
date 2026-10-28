using ChartForgeX.Raster;

namespace ChartForgeX.Effects;

/// <summary>Immutable opt-in blur for one complete captured prepared visual; it does not enable a filter on existing scenes.</summary>
/// <remarks>Lengths are logical units. Capture multiplies them by its output density and rounds once to 1/256 device pixel.
/// Every nonzero target uses the existing deterministic three-box Gaussian approximation, not an exact Gaussian convolution.
/// Capture rejects a density-scaled target above 128 device pixels before rendering. Exterior samples are transparent black.</remarks>
public sealed class VisualLayerBlur {
    /// <summary>Creates an explicit blur. A null vertical target uses the horizontal target.</summary>
    /// <param name="sigmaX">Finite nonnegative horizontal target, at most 128 logical units before the capture-density check.</param>
    /// <param name="sigmaY">Optional independent vertical target under the same policy.</param>
    /// <param name="orderedDither">Whether final RGBA8 conversion uses the existing deterministic layer-local Bayer pattern.</param>
    public VisualLayerBlur(double sigmaX, double? sigmaY = null, bool orderedDither = true) {
        _ = RasterEffectPlan.SigmaQ8(sigmaX, nameof(sigmaX));
        _ = RasterEffectPlan.SigmaQ8(sigmaY ?? sigmaX, nameof(sigmaY));
        SigmaX = sigmaX; SigmaY = sigmaY ?? sigmaX; OrderedDither = orderedDither;
    }
    /// <summary>Gets the horizontal standard-deviation target in logical units.</summary>
    public double SigmaX { get; }
    /// <summary>Gets the vertical standard-deviation target in logical units.</summary>
    public double SigmaY { get; }
    /// <summary>Gets whether the final effect conversion uses deterministic ordered dither.</summary>
    public bool OrderedDither { get; }
    internal int HorizontalQ8(int density) => RasterEffectPlan.SigmaQ8(SigmaX, nameof(SigmaX), density);
    internal int VerticalQ8(int density) => RasterEffectPlan.SigmaQ8(SigmaY, nameof(SigmaY), density);
}
