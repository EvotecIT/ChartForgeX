using System;
using System.Threading;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.Effects;

/// <summary>One finite immutable four-channel procedural source, independent of SourceGraphic.</summary>
/// <remarks>The authored octave count is retained; output processes at most nine octaves for the 8-bit
/// resolution policy. Four channels share one seeded stream of normalized-square gradients with zero-vector
/// retry. Native output samples physical cell centers and quantizes unassociated channels. NativeFilters
/// delegates phase, precision and intermediate alpha conversion to the browser; byte identity is not promised.</remarks>
public sealed partial class VisualTurbulence : IVisualRenderable {
    /// <summary>Creates explicit procedural channel policy.</summary>
    /// <param name="baseFrequencyX">Finite nonnegative horizontal logical frequency.</param>
    /// <param name="baseFrequencyY">Vertical frequency; null selects the horizontal value.</param>
    /// <param name="numberOfOctaves">Nonnegative authored count; zero is a real constant-field case.</param>
    /// <param name="seed">Raw finite seed, normalized without an overflowing double-to-integer conversion.</param>
    /// <param name="type">Turbulence uses absolute octave contributions; FractalNoise uses signed contributions.</param>
    /// <param name="stitchTiles">Adjusts frequency and lattice wrapping to the actual logical tile extent.</param>
    /// <param name="colorSpace">Native explicit working space; SVG resolves its inherited default separately.</param>
    public VisualTurbulence(double baseFrequencyX, double? baseFrequencyY = null, int numberOfOctaves = 1,
        double seed = 0, VisualTurbulenceType type = VisualTurbulenceType.Turbulence,
        bool stitchTiles = false, VisualFilterColorSpace colorSpace = VisualFilterColorSpace.Srgb) {
        ValidateFrequency(baseFrequencyX, nameof(baseFrequencyX));
        ValidateFrequency(baseFrequencyY ?? baseFrequencyX, nameof(baseFrequencyY));
        if (numberOfOctaves < 0) throw new ArgumentOutOfRangeException(nameof(numberOfOctaves));
        if (double.IsNaN(seed) || double.IsInfinity(seed)) throw new ArgumentOutOfRangeException(nameof(seed));
        if (type != VisualTurbulenceType.Turbulence && type != VisualTurbulenceType.FractalNoise)
            throw new ArgumentOutOfRangeException(nameof(type));
        if (colorSpace != VisualFilterColorSpace.Srgb && colorSpace != VisualFilterColorSpace.LinearRgb)
            throw new ArgumentOutOfRangeException(nameof(colorSpace));
        BaseFrequencyX = baseFrequencyX; BaseFrequencyY = baseFrequencyY ?? baseFrequencyX;
        NumberOfOctaves = numberOfOctaves; Seed = seed; Type = type;
        StitchTiles = stitchTiles; ColorSpace = colorSpace;
    }
    /// <summary>Gets the authored horizontal frequency.</summary>
    public double BaseFrequencyX { get; }
    /// <summary>Gets the authored vertical frequency.</summary>
    public double BaseFrequencyY { get; }
    /// <summary>Gets the authored count, before the documented 8-bit output-resolution policy.</summary>
    public int NumberOfOctaves { get; }
    /// <summary>Gets the original finite seed.</summary>
    public double Seed { get; }
    /// <summary>Gets the finite octave accumulation law.</summary>
    public VisualTurbulenceType Type { get; }
    /// <summary>Gets whether the tile boundary is periodic in logical coordinates.</summary>
    public bool StitchTiles { get; }
    /// <summary>Gets the selected channel space; alpha is always coverage.</summary>
    public VisualFilterColorSpace ColorSpace { get; }

    /// <summary>Generates a detached mutable RGBA image through the common effect/preflight owner.</summary>
    /// <param name="size">Positive logical tile extent exactly aligned to the selected device grid.</param>
    /// <param name="pixelsPerUnit">Native density from one through sixteen, subject to existing dimension and work caps.</param>
    /// <param name="cancellationToken">Checked before buffers, during setup and each row; never retained.</param>
    /// <returns>Caller-owned unassociated sRGB bytes sampled at actual physical cell centers.</returns>
    /// <remarks>There is no SourceGraphic or implicit capture. Logical boundary periodicity does not mean
    /// the first and last pixel centers have equal values. Encoders and later caller copies have their own budgets.</remarks>
    public RgbaImage Generate(VisualSize size, int pixelsPerUnit = 1, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        var plan = RasterEffectPlan.CreateTurbulence(this, size, pixelsPerUnit, cancellationToken);
        var preflight = RasterEffectPreflight.PrepareSourceFree(plan);
        return RasterEffects.Generate(preflight, 0, cancellationToken).Image;
    }
    internal bool SameSourcePolicy(VisualTurbulence other) => other != null &&
        BaseFrequencyX == other.BaseFrequencyX && BaseFrequencyY == other.BaseFrequencyY && Seed == other.Seed &&
        NumberOfOctaves == other.NumberOfOctaves && Type == other.Type && StitchTiles == other.StitchTiles && ColorSpace == other.ColorSpace;

    private static void ValidateFrequency(double value, string parameter) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0) throw new ArgumentOutOfRangeException(parameter);
    }
}

/// <summary>Finite SVG procedural accumulation forms.</summary>
public enum VisualTurbulenceType {
    /// <summary>Absolute noise contributions.</summary>
    Turbulence = 0,
    /// <summary>Signed noise contributions, mapped to normalized output.</summary>
    FractalNoise = 1
}
