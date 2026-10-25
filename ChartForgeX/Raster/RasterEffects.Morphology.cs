using System;
using System.Threading;

namespace ChartForgeX.Raster;

internal static partial class RasterEffects {
    /// <summary>Spreads original byte alpha before exact RGBA16 expansion, without an intermediate blur quantization.</summary>
    private static void CaptureSpreadAlpha(RgbaImage source, RasterEffectPlan plan, ushort[] target, CancellationToken cancellation) {
        var region = plan.MorphologyRegion; var selected = plan.SourceRegion;
        cancellation.ThrowIfCancellationRequested();
        var alpha = new byte[checked(region.Width * region.Height)];
        for (var y = 0; y < region.Height; y++) {
            cancellation.ThrowIfCancellationRequested(); var sy = (long)region.Y + y;
            if (sy < selected.Y || sy >= (long)selected.Y + selected.Height || sy < 0 || sy >= source.Height) continue;
            for (var x = 0; x < region.Width; x++) {
                var sx = (long)region.X + x;
                if (sx < selected.X || sx >= (long)selected.X + selected.Width || sx < 0 || sx >= source.Width) continue;
                alpha[y * region.Width + x] = source.Pixels[checked(((int)sy * source.Width + (int)sx) * 4 + 3)];
            }
        }
        var radius = Math.Abs(plan.SpreadRadius);
        var filtered = RasterAlphaExtrema.Filter(alpha, region.Width, region.Height, 1, 0, radius, radius,
            plan.SpreadRadius > 0, cancellation);
        var output = plan.OutputRegion;
        for (var y = 0; y < output.Height; y++) {
            cancellation.ThrowIfCancellationRequested(); var sy = (long)output.Y + y - plan.OffsetY - region.Y;
            if (sy < 0 || sy >= region.Height) continue;
            for (var x = 0; x < output.Width; x++) {
                var sx = (long)output.X + x - plan.OffsetX - region.X;
                if (sx >= 0 && sx < region.Width) target[y * output.Width + x] = (ushort)(filtered[(int)sy * region.Width + (int)sx] * 257);
            }
        }
    }
}
