using System;
using System.Threading;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.Composition;

public sealed partial class ImageComposition {
    /// <summary>Blurs all current pixels, including text, using the shared bounded three-box Gaussian approximation.</summary>
    /// <param name="sigmaX">Horizontal standard-deviation target in stored device pixels, from zero through 128.</param>
    /// <param name="sigmaY">Vertical target in stored device pixels; null uses the horizontal target.</param>
    /// <param name="cancellationToken">Cancels before the final pixel replacement.</param>
    /// <returns>The current composition, retaining its dimensions.</returns>
    /// <remarks>Targets are rounded to 1/256 device pixel. Three boxes approximate the target, including small sigma;
    /// this is not an exact Gaussian convolution. Exterior samples are transparent black and the result is clipped to
    /// the fixed composition frame. Device units do not scale again on internally scaled compositions. New effect pixels
    /// use gamma-encoded sRGB premultiplied 16-bit and one final ordered-dither conversion; zero sigma preserves original bytes.
    /// Rejection or cancellation before commit leaves the original pixels unchanged. Do not mutate this composition concurrently.
    /// SVG can use the existing VisualCanvas.AddImage snapshot to embed these computed pixels; no SVG filter is required.</remarks>
    public ImageComposition Blur(double sigmaX, double? sigmaY = null, CancellationToken cancellationToken = default) {
        var x = EffectSigma(sigmaX, nameof(sigmaX)); var y = EffectSigma(sigmaY ?? sigmaX, nameof(sigmaY)); cancellationToken.ThrowIfCancellationRequested();
        if (x == 0 && y == 0) return this;
        var source = _canvas.BorrowDeviceSurfaceForEffects();
        var plan = RasterEffectPlan.Blur(new RasterEffectRegion(0, 0, source.Width, source.Height), x, y);
        var batch = EffectBatch(source, plan, 20, finalBuffer: true);
        var filtered = RasterEffects.Execute(batch, 0, true, cancellationToken);
        CommitEffect(CropEffect(source, filtered, cancellationToken), cancellationToken);
        return this;
    }

    /// <summary>Draws a tinted, blurred alpha shadow beneath the complete current source exactly once.</summary>
    /// <param name="color">Resolved literal tint, including opacity applied once to the captured source alpha.</param>
    /// <param name="offsetX">Signed horizontal offset in stored device pixels.</param>
    /// <param name="offsetY">Signed vertical offset in stored device pixels.</param>
    /// <param name="sigmaX">Horizontal three-box Gaussian-approximation target in stored device pixels, from zero through 128.</param>
    /// <param name="sigmaY">Vertical target; null uses the horizontal target.</param>
    /// <param name="cancellationToken">Cancels before the final pixel replacement.</param>
    /// <returns>The current composition, retaining its dimensions.</returns>
    /// <remarks>The whole source, including text, is captured as one atomic unit. Shadow support outside the fixed frame
    /// is clipped. The shadow and unchanged captured source are composited in that order in the shared premultiplied 16-bit raster owner before one final quantization;
    /// the source is not drawn twice. The blur, device-unit, rounding and cancellation policies match Blur. An opaque
    /// background is part of the captured source; create a transparent composition when shadowing a floating mark or panel.
    /// Work and all pixel arrays are preflighted together; this is not a guarantee about encoder allocations or process memory.
    /// The bounded effect frame permits at most 8192 samples per axis and 16,777,216 samples, with a 256 MiB pixel-array
    /// budget and cumulative per-call pass/composition work limit; requests beyond those limits fail before commit.</remarks>
    public ImageComposition DrawShadow(ChartColor color, int offsetX, int offsetY, double sigmaX, double? sigmaY = null,
        CancellationToken cancellationToken = default) {
        var x = EffectSigma(sigmaX, nameof(sigmaX)); var y = EffectSigma(sigmaY ?? sigmaX, nameof(sigmaY)); cancellationToken.ThrowIfCancellationRequested();
        if (color.A == 0) return this;
        var source = _canvas.BorrowDeviceSurfaceForEffects();
        var plan = RasterEffectPlan.ShadowUnderSource(new RasterEffectRegion(0, 0, source.Width, source.Height), x, y, offsetX, offsetY, color);
        if ((long)plan.OutputRegion.X >= source.Width || (long)plan.OutputRegion.Y >= source.Height
            || (long)plan.OutputRegion.X + plan.OutputRegion.Width <= 0 || (long)plan.OutputRegion.Y + plan.OutputRegion.Height <= 0) return this;
        var batch = EffectBatch(source, plan, 8, finalBuffer: false);
        var shadow = RasterEffects.Execute(batch, 0, true, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        CommitEffect(shadow.Image.Pixels, cancellationToken); return this;
    }

    private static RasterEffectPreflight EffectBatch(RgbaImage source, RasterEffectPlan plan, long extraVisitsPerPixel, bool finalBuffer) =>
        RasterEffectPreflight.Prepare(new[] { plan }, new[] { source }, additionalPixelBytes: finalBuffer ? checked((long)source.Width * source.Height * 4) : 0,
            additionalWorkSamples: checked((long)source.Width * source.Height * extraVisitsPerPixel));

    private void CommitEffect(byte[] final, CancellationToken cancellation) {
        cancellation.ThrowIfCancellationRequested();
        // No cancellation point occurs inside this final replacement: cancellation cannot expose a partly changed source.
        Buffer.BlockCopy(final, 0, _canvas.Pixels, 0, final.Length);
    }

    private static int EffectSigma(double sigma, string parameter) => RasterEffectPlan.SigmaQ8(sigma, parameter);
}
