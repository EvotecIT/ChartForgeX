using System;
using System.Threading;
using ChartForgeX.Effects;

namespace ChartForgeX.Raster;

/// <summary>Standards luminance from unassociated channels, kept separate from the selected legacy canvas luma policy.</summary>
internal static class RasterMaskCoverage {
    internal static int Coverage16(byte[] mask, int offset, VisualMaskMode mode, bool linearRgb = false) {
        var alpha = mask[offset + 3] * 257;
        if (mode == VisualMaskMode.Alpha) return alpha;
        return RasterEffectPixels.Multiply(Luminance16(mask, offset, linearRgb), alpha);
    }

    /// <summary>Native unassociated luminance; alpha multiplication remains the caller's separate coverage choice.</summary>
    internal static int Luminance16(byte[] mask, int offset, bool linearRgb = false) {
        var luminance = linearRgb
            ? .2126 * RasterColorTransfer.Linear(mask[offset]) + .7152 * RasterColorTransfer.Linear(mask[offset + 1]) + .0722 * RasterColorTransfer.Linear(mask[offset + 2])
            : (.2126 * mask[offset] + .7152 * mask[offset + 1] + .0722 * mask[offset + 2]) / 255;
        var luminance16 = (int)Math.Floor(Math.Max(0, Math.Min(1, luminance)) * 65535 + .5);
        return luminance16;
    }

    /// <summary>Materializes native mask coverage into alpha-only RGBA for a typed SVG mask input.</summary>
    /// <remarks>The source bitmap is never mutated. Luminance retains the native coefficients;
    /// SVG standards luminance is not substituted. Output quantization is explicit and happens
    /// before a browser multiplies its own source coverage, so profile pixels can differ.</remarks>
    internal static RgbaImage MaterializeAlpha(RgbaImage image, VisualMaskMode mode, int phaseX, int phaseY,
        bool dither, CancellationToken cancellation) {
        cancellation.ThrowIfCancellationRequested();
        var plan = RasterEffectPlan.Mask(new RasterEffectRegion(0, 0, image.Width, image.Height),
            image, 0, 0, mode);
        // An implicit opaque-white source needs no array: reserve the retained operand,
        // one output array and the same bounded per-pixel mask work through the existing owner.
        RasterEffectPreflight.ValidateCapture(plan, 0, 0, 0, out _, out _);
        var pixels = new byte[checked(image.Width * image.Height * 4)];
        for (var y = 0; y < image.Height; y++) {
            cancellation.ThrowIfCancellationRequested();
            for (var x = 0; x < image.Width; x++) {
                var at = (y * image.Width + x) * 4;
                var coverage = Coverage16(image.Pixels, at, mode);
                var alpha = RasterEffectPixels.QuantizeMaskAlpha(coverage,
                    unchecked(phaseX + x), unchecked(phaseY + y), dither);
                if (alpha == 0) continue;
                pixels[at] = pixels[at + 1] = pixels[at + 2] = 255;
                pixels[at + 3] = alpha;
            }
        }
        cancellation.ThrowIfCancellationRequested();
        return new RgbaImage(image.Width, image.Height, pixels);
    }

}
