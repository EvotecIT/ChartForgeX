using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.SvgRaster;

internal static partial class SvgRasterRenderer {
    /// <summary>One existing contour coverage owner clips the generated field, without sampling an intermediate bitmap.</summary>
    private static void ClipProceduralRegion(RasterEffectResult generated, SvgRasterFilterPlan plan,
        SvgRasterFilterContext context) {
        var contour = plan.ProceduralClipContour!;
        var image = generated.Image;
        context.Cancellation.ThrowIfCancellationRequested();
        // Declared pixel storage was reserved before source-free generation. Geometry/scanline scratch
        // remains with the incumbent native contour owner, exactly as ordinary SVG clips and masks.
        var coverage = new RgbaCanvas(image.Width, image.Height, 1);
        coverage.FillPolygon(contour, ChartColor.White);
        context.Cancellation.ThrowIfCancellationRequested();
        for (var y = 0; y < image.Height; y++) {
            context.Cancellation.ThrowIfCancellationRequested();
            for (var x = 0; x < image.Width; x++) {
                var offset = (y * image.Width + x) * 4;
                // Same typed alpha-only mask conversion, in place on the newly owned generated pixels.
                // Nonzero alpha retains source unassociated RGB without premultiply/unmultiply loss.
                RasterEffectPixels.WriteMasked(image.Pixels, offset, image.Pixels, offset,
                    coverage.Pixels[offset + 3] * 257, x, y, false);
            }
        }
    }
}
