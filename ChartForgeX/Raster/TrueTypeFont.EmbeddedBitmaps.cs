using System;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Raster;

internal sealed partial class TrueTypeFont {
    private BitmapGlyph? EmbeddedGlyph(ushort glyph, double outputPixelSize)
        => _variation?.HasNonzeroCoordinates == true ? null : _monochromeBitmaps?.Exact(glyph, outputPixelSize);

    /// <summary>Uses authored coverage at an exact strike size, retaining the shaped outline advance.</summary>
    private bool DrawEmbeddedGlyph(RgbaCanvas canvas, ushort glyph, double x, double baseline, double scale, ChartColor foreground) {
        var bitmap = EmbeddedGlyph(glyph, Math.Abs(scale) * UnitsPerEm * canvas.FontStrikeScale);
        if (bitmap == null) return false;
        var image = bitmap.Image; var bounds = bitmap.Bounds;
        var density = canvas.FontStrikeScale;
        var left = Math.Floor((x + bounds.X * scale) * density + 0.5) / density;
        var top = Math.Floor((baseline - (bounds.Y + bounds.Height) * scale) * density + 0.5) / density;
        // Coverage cells already describe output pixels. Image filtering would blur authored stems.
        for (var y = 0; y < image.Height; y++) for (var column = 0; column < image.Width; column++) {
            var alpha = (byte)((image.Pixels[(y * image.Width + column) * 4 + 3] * foreground.A + 127) / 255);
            if (alpha != 0) canvas.FillRect(left + column / density, top + y / density, 1 / density, 1 / density,
                new ChartColor(foreground.R, foreground.G, foreground.B, alpha));
        }
        return true;
    }
}
