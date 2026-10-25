using System;
using System.Threading;

namespace ChartForgeX.Raster;

internal static partial class RasterEffects {
    private static int OriginalOffset(RgbaImage source, RasterEffectRegion selected, long x, long y) =>
        x >= selected.X && y >= selected.Y && x < (long)selected.X + selected.Width &&
        y < (long)selected.Y + selected.Height && x >= 0 && y >= 0 && x < source.Width && y < source.Height
            ? checked(((int)y * source.Width + (int)x) * 4) : -1;

    private static bool HasBrightSamples(RgbaImage source, RasterEffectPlan plan, CancellationToken cancellation) {
        var region = plan.SourceRegion;
        for (var y = 0; y < region.Height; y++) {
            cancellation.ThrowIfCancellationRequested();
            for (var x = 0; x < region.Width; x++) {
                var input = OriginalOffset(source, region, (long)region.X + x, (long)region.Y + y);
                if (input >= 0 && source.Pixels[input + 3] != 0 &&
                    RasterMaskCoverage.Luminance16(source.Pixels, input) > plan.BloomThreshold16) return true;
            }
        }
        return false;
    }

    private static void CaptureBrightPass(RgbaImage source, RasterEffectPlan plan, ushort[] target,
        CancellationToken cancellation) {
        var region = plan.OutputRegion;
        var denominator = 65535 - plan.BloomThreshold16;
        for (var y = 0; y < region.Height; y++) {
            cancellation.ThrowIfCancellationRequested();
            for (var x = 0; x < region.Width; x++) {
                var input = OriginalOffset(source, plan.SourceRegion, (long)region.X + x, (long)region.Y + y);
                if (input < 0) continue;
                var luminance = RasterMaskCoverage.Luminance16(source.Pixels, input);
                if (luminance <= plan.BloomThreshold16) continue;
                var weight = (int)(((long)(luminance - plan.BloomThreshold16) * 65535 + denominator / 2) / denominator);
                var originalAlpha = source.Pixels[input + 3] * 257;
                var alpha = RasterEffectPixels.Multiply(originalAlpha, weight);
                var output = (y * region.Width + x) * 4;
                target[output] = (ushort)RasterEffectPixels.Multiply(
                    RasterEffectPixels.Premultiply(source.Pixels[input], originalAlpha), weight);
                target[output + 1] = (ushort)RasterEffectPixels.Multiply(
                    RasterEffectPixels.Premultiply(source.Pixels[input + 1], originalAlpha), weight);
                target[output + 2] = (ushort)RasterEffectPixels.Multiply(
                    RasterEffectPixels.Premultiply(source.Pixels[input + 2], originalAlpha), weight);
                target[output + 3] = (ushort)alpha;
            }
        }
    }

    private static RasterEffectResult ExecuteBackdrop(RgbaImage source, RasterEffectPlan plan,
        bool alphaDither, CancellationToken cancellation) {
        var region = plan.SourceRegion;
        var mask = plan.Operand!.Value;
        int Coverage(int x, int y) {
            var mx = (long)x - plan.OperandX;
            var my = (long)y - plan.OperandY;
            return mx < 0 || my < 0 || mx >= mask.Width || my >= mask.Height ? 0
                : RasterMaskCoverage.Coverage16(mask.Pixels, ((int)my * mask.Width + (int)mx) * 4, plan.MaskMode);
        }
        var hasCoverage = false;
        for (var y = 0; y < region.Height && !hasCoverage; y++) {
            cancellation.ThrowIfCancellationRequested();
            for (var x = 0; x < region.Width; x++) {
                if (Coverage(region.X + x, region.Y + y) > 0) { hasCoverage = true; break; }
            }
        }
        if (!hasCoverage) return new RasterEffectResult(source, region);
        var filtered = ExecutePlan(source, plan.BackdropBlur!, plan.BackdropBlurDither, cancellation);
        var output = new byte[checked(region.Width * region.Height * 4)];
        for (var y = 0; y < region.Height; y++) {
            cancellation.ThrowIfCancellationRequested();
            for (var x = 0; x < region.Width; x++) {
                var at = (y * region.Width + x) * 4;
                var original = OriginalOffset(source, region, region.X + x, region.Y + y);
                var coverage = Coverage(region.X + x, region.Y + y);
                var blurred = checked(((region.Y + y - filtered.Region.Y) * filtered.Image.Width +
                    region.X + x - filtered.Region.X) * 4);
                if (coverage == 0) { Buffer.BlockCopy(source.Pixels, original, output, at, 4); continue; }
                if (coverage == 65535) { Buffer.BlockCopy(filtered.Image.Pixels, blurred, output, at, 4); continue; }
                RasterEffectPixels.ReplaceCoverage(output, at, source.Pixels, original,
                    filtered.Image.Pixels, blurred, coverage, region.X + x, region.Y + y, alphaDither);
            }
        }
        return new RasterEffectResult(new RgbaImage(region.Width, region.Height, output), region);
    }
}
