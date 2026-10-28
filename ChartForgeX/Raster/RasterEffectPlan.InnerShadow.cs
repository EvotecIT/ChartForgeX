using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal sealed partial class RasterEffectPlan {
    internal bool IsInnerShadow { get; }

    /// <summary>Working alpha support reuses morphology/offset/blur; output remains original-sized.</summary>
    internal static RasterEffectPlan InnerShadow(RasterEffectRegion source, int sigmaX, int sigmaY,
        int offsetX, int offsetY, ChartColor color, int insetSpread, double opacity = 1) {
        if (insetSpread == int.MinValue) throw new ArgumentOutOfRangeException(nameof(insetSpread));
        return new RasterEffectPlan(source, sigmaX, sigmaY, offsetX, offsetY, color,
            spreadRadius: -insetSpread, shadowOpacity: opacity, innerShadow: true);
    }
}
