using System;
using System.Threading;

namespace ChartForgeX.Raster;

internal static partial class RasterEffects {
    private static RasterEffectResult ExecutePixels(RgbaImage source, RasterEffectPlan plan, bool dither, CancellationToken cancellation) {
        var region = plan.ResultRegion;
        cancellation.ThrowIfCancellationRequested();
        var pixels = new byte[checked(region.Width * region.Height * 4)];
        var operand = plan.Operand;
        for (var y = 0; y < region.Height; y++) {
            cancellation.ThrowIfCancellationRequested();
            for (var x = 0; x < region.Width; x++) {
                var output = (y * region.Width + x) * 4;
                var sx = (long)region.X + x; var sy = (long)region.Y + y;
                var input = sx >= 0 && sy >= 0 && sx < source.Width && sy < source.Height ? ((int)sy * source.Width + (int)sx) * 4 : -1;
                var ox = sx - plan.OperandX; var oy = sy - plan.OperandY;
                var other = operand.HasValue && ox >= 0 && oy >= 0 && ox < operand.Value.Width && oy < operand.Value.Height
                    ? ((int)oy * operand.Value.Width + (int)ox) * 4 : -1;
                if (plan.PixelOperation == RasterPixelOperation.Mask) {
                    if (input >= 0 && other >= 0) RasterEffectPixels.WriteMasked(pixels, output, source.Pixels, input,
                        RasterMaskCoverage.Coverage16(operand!.Value.Pixels, other, plan.MaskMode), unchecked(region.X + x + plan.QuantizeOriginX), unchecked(region.Y + y + plan.QuantizeOriginY), dither);
                    continue;
                }
                if (plan.PixelOperation == RasterPixelOperation.Blend && other < 0) {
                    if (input >= 0) Buffer.BlockCopy(source.Pixels, input, pixels, output, 4);
                    continue;
                }
                byte ReadSource(int channel) => input < 0 ? (byte)0 : source.Pixels[input + channel];
                if (plan.PixelOperation == RasterPixelOperation.PorterDuff) {
                    if (other < 0 && plan.CompositeOperator == ChartForgeX.Effects.VisualCompositeOperator.SourceOver) {
                        if (input >= 0) Buffer.BlockCopy(source.Pixels, input, pixels, output, 4);
                        continue; // Exact parity with the incumbent Normal blend outside the source bitmap.
                    }
                    // Transparent source outside its placed extent participates in EVERY destination-frame pixel.
                    // In/out/atop/copy/clear must not reuse the ordinary blend's outside-source skip above.
                    byte ReadOperand(int channel) => other < 0 ? (byte)0 : operand!.Value.Pixels[other + channel];
                    RasterEffectPixels.Composite(pixels, output, ReadOperand(0), ReadOperand(1), ReadOperand(2), ReadOperand(3),
                        ReadSource(0), ReadSource(1), ReadSource(2), ReadSource(3), plan.CompositeOperator,
                        unchecked(region.X + x), unchecked(region.Y + y), dither);
                } else if (plan.PixelOperation == RasterPixelOperation.FloodPorterDuff) {
                    var flood = plan.Flood;
                    RasterEffectPixels.Composite(pixels, output, ReadSource(0), ReadSource(1), ReadSource(2), ReadSource(3),
                        flood.R, flood.G, flood.B, flood.A, plan.CompositeOperator,
                        unchecked(region.X + x), unchecked(region.Y + y), dither);
                } else if (plan.PixelOperation == RasterPixelOperation.FloodBlend) {
                    var flood = plan.Flood;
                    RasterEffectPixels.Composite(pixels, output, ReadSource(0), ReadSource(1), ReadSource(2), ReadSource(3),
                        flood.R, flood.G, flood.B, flood.A, plan.BlendMode, unchecked(region.X + x), unchecked(region.Y + y), dither);
                } else {
                    var top = operand!.Value.Pixels;
                    RasterEffectPixels.Composite(pixels, output, top[other], top[other + 1], top[other + 2], top[other + 3],
                        ReadSource(0), ReadSource(1), ReadSource(2), ReadSource(3), plan.BlendMode, unchecked(region.X + x), unchecked(region.Y + y), dither);
                }
            }
        }
        return new RasterEffectResult(new RgbaImage(region.Width, region.Height, pixels), region);
    }
}
