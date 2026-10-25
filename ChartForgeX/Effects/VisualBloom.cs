using System;
using ChartForgeX.Raster;

namespace ChartForgeX.Effects;

/// <summary>Immutable finite gamma-sRGB luminance-ramp, blur and Screen bloom.</summary>
/// <remarks>Unassociated gamma-sRGB luminance uses the existing native mask coefficients. Bright-pass alpha
/// and premultiplied channels remain16-bit until one final conversion. This is not linear-light or HDR bloom.</remarks>
public sealed class VisualBloom {
    /// <summary>Creates a finite bloom with explicitly authored brightness threshold and contribution opacity.</summary>
    /// <param name="sigmaX">Logical horizontal three-box Gaussian target.</param>
    /// <param name="threshold">Required normalized luminance threshold in [0,1], resolved to native16-bit coverage.</param>
    /// <param name="strength">Required contribution opacity in [0,1], resolved to native16-bit coverage.</param>
    /// <param name="sigmaY">Optional vertical target; null uses sigmaX.</param>
    /// <param name="orderedDither">Uses the existing single final RGBA8 quantizer.</param>
    public VisualBloom(double sigmaX, double threshold, double strength, double? sigmaY = null, bool orderedDither = true) {
        _ = RasterEffectPlan.SigmaQ8(sigmaX, nameof(sigmaX));
        _ = RasterEffectPlan.SigmaQ8(sigmaY ?? sigmaX, nameof(sigmaY));
        Threshold16 = Coverage(threshold, nameof(threshold));
        Strength16 = Coverage(strength, nameof(strength));
        SigmaX = sigmaX;
        SigmaY = sigmaY ?? sigmaX;
        Threshold = threshold;
        Strength = strength;
        OrderedDither = orderedDither;
    }
    /// <summary>Gets the logical horizontal Gaussian target.</summary>
    public double SigmaX { get; }
    /// <summary>Gets the logical vertical Gaussian target.</summary>
    public double SigmaY { get; }
    /// <summary>Gets the required authored gamma-sRGB threshold.</summary>
    public double Threshold { get; }
    /// <summary>Gets the required authored contribution opacity.</summary>
    public double Strength { get; }
    /// <summary>Gets the final native RGBA dither policy.</summary>
    public bool OrderedDither { get; }
    internal int Threshold16 { get; }
    internal int Strength16 { get; }
    internal int HorizontalQ8(int density) => RasterEffectPlan.SigmaQ8(SigmaX, nameof(SigmaX), density);
    internal int VerticalQ8(int density) => RasterEffectPlan.SigmaQ8(SigmaY, nameof(SigmaY), density);
    private static int Coverage(double value, string parameter) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0 || value > 1) throw new ArgumentOutOfRangeException(parameter);
        return (int)Math.Floor(value * 65535 + .5);
    }
}
