using System;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal sealed partial class RasterEffectPlan {
    internal bool IsOutline { get; }
    internal int BloomThreshold16 { get; } = -1;
    internal int BloomStrength16 { get; } = 65535;
    internal bool IsBloom => BloomThreshold16 >= 0;
    internal RasterEffectPlan? BackdropBlur { get; }
    internal bool BackdropBlurDither { get; }

    /// <summary>Reuses the shared alpha-extrema and source-union support; only the final band membership differs.</summary>
    internal static RasterEffectPlan Outline(RasterEffectRegion source, ChartColor color, int radius) {
        if (radius < 0) throw new ArgumentOutOfRangeException(nameof(radius));
        return new(source, 0, 0, 0, 0, color, compositeSource: true, unionSource: true,
            spreadRadius: radius, outline: true);
    }

    /// <summary>Bright-pass RGBA16 and the incumbent Gaussian boxes share one allocation and quantization plan.</summary>
    internal static RasterEffectPlan Bloom(RasterEffectRegion source, int x, int y, int threshold, int strength) {
        if (threshold < 0 || threshold > 65535) throw new ArgumentOutOfRangeException(nameof(threshold));
        if (strength < 0 || strength > 65535) throw new ArgumentOutOfRangeException(nameof(strength));
        return new(source, x, y, 0, 0, null, bloomThreshold: threshold, bloomStrength: strength);
    }

    private RasterEffectPlan(RasterEffectRegion source, RgbaImage coverage, int x, int y,
        VisualMaskMode mode, RasterEffectPlan blur, bool blurDither)
        : this(source, RasterPixelOperation.Mask, coverage, x, y, mode, default, default) {
        BackdropBlur = blur;
        BackdropBlurDither = blurDither;
        // Include the zero-coverage scan, original/final copies and both coverage/channel visits.
        WorkSamples = checked(blur.WorkSamples + Pixels * 112);
    }

    /// <summary>A finite destination blur followed by one coverage replacement, never a second preflight or graph.</summary>
    internal static RasterEffectPlan BlurRegion(RasterEffectRegion source, RgbaImage coverage,
        int x, int y, VisualMaskMode mode, int sigmaX, int sigmaY, bool blurDither) {
        ValidateMaskMode(mode);
        return new(source, coverage, x, y, mode, Blur(source, sigmaX, sigmaY), blurDither);
    }
}
