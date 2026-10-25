using ChartForgeX.Effects;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal sealed partial class RgbaCanvas {
    /// <summary>Propagates completed typed effect samples through the incumbent SVG layer compositors.</summary>
    internal bool HasCapturedEffectPixels { get; set; }

    /// <summary>Uses the common typed source-over conversion once for an already completed object.</summary>
    private void BlendCapturedPixel(int x, int y, ChartColor source) {
        if (x < 0 || y < 0 || x >= _pixelWidth || y >= _pixelHeight || !IsInsideClip(x, y) || source.A == 0) return;
        var output = (y * _pixelWidth + x) * 4;
        if (source.A == 255 || Pixels[output + 3] == 0) {
            // A copy into transparency preserves unassociated RGB and alpha without a
            // lossy premultiply/unmultiply conversion, including very small alpha bytes.
            Pixels[output] = source.R;
            Pixels[output + 1] = source.G;
            Pixels[output + 2] = source.B;
            Pixels[output + 3] = source.A;
            return;
        }
        RasterEffectPixels.Composite(Pixels, output, source.R, source.G, source.B, source.A,
            Pixels[output], Pixels[output + 1], Pixels[output + 2], Pixels[output + 3],
            VisualBlendMode.Normal, x, y, false);
    }
}
