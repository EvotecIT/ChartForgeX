using System;
using ChartForgeX.Effects;

namespace ChartForgeX.Raster;

internal static partial class RasterEffects {
    /// <summary>Reads the complement once and uses the shared 16-bit SourceAtop owner without an inverse bitmap.</summary>
    private static void WriteInnerShadow(byte[] target, int output, RgbaImage source, RasterEffectPlan plan,
        long x, long y, int blurredAlpha, bool dither) {
        var at = OriginalOffset(source, plan.SourceRegion, x, y);
        if (at < 0) return;
        var originalAlpha = source.Pixels[at + 3] * 257;
        var color = plan.Shadow!.Value;
        var alpha = RasterEffectPixels.Multiply(65535 - blurredAlpha, color.A * 257);
        if (plan.ShadowOpacity != 1) alpha = (int)Math.Floor(alpha * plan.ShadowOpacity + .5);
        if (alpha == 0 || originalAlpha == 0) {
            Buffer.BlockCopy(source.Pixels, at, target, output, 4);
            return;
        }
        RasterEffectPixels.CompositePremultiplied(target, output,
            RasterEffectPixels.Premultiply(color.R, alpha), RasterEffectPixels.Premultiply(color.G, alpha),
            RasterEffectPixels.Premultiply(color.B, alpha), alpha,
            RasterEffectPixels.Premultiply(source.Pixels[at], originalAlpha),
            RasterEffectPixels.Premultiply(source.Pixels[at + 1], originalAlpha),
            RasterEffectPixels.Premultiply(source.Pixels[at + 2], originalAlpha), originalAlpha,
            VisualCompositeOperator.SourceAtop, unchecked((int)x), unchecked((int)y), dither);
    }
}
