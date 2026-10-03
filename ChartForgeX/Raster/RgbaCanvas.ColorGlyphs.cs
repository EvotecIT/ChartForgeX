using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal sealed partial class RgbaCanvas {
    internal int DeviceScale => _scale;
    internal int DeviceWidth => _pixelWidth;
    internal int DeviceHeight => _pixelHeight;
    /// <summary>Final output pixel density, retained by temporary text buffers independently of antialiasing.</summary>
    internal double FontStrikeScale { get; set; }
    internal FontGlyphPaintMode GlyphPaintMode { get; set; }
    /// <summary>Composes a colour-glyph surface already sampled on this canvas's device grid.</summary>
    internal void DrawDeviceColors(int x, int y, int width, int height, LinearRgba[] pixels, double opacity) {
        for (var yy = 0; yy < height; yy++) for (var xx = 0; xx < width; xx++) {
            var color = pixels[yy * width + xx].ToColor(opacity);
            if (color.A != 0) BlendPixel(x + xx, y + yy, color);
        }
    }
}

/// <summary>Separate fixed font palettes from an SVG paint applied to ordinary text.</summary>
internal enum FontGlyphPaintMode { All, MonochromeOnly, ColourOnly }
