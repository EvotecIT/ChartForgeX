using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.SvgRaster;
using ChartForgeX.Typography;

namespace ChartForgeX.Raster;

internal sealed partial class TrueTypeFont {
    private bool DrawColorGlyph(RgbaCanvas canvas, ushort glyph, double x, double baseline, double scale, bool italic, ChartColor foreground) {
        if (_colors == null) return false;
        var matrix = new SvgRasterMatrix(scale, 0, italic ? ObliqueShear * scale : 0, -scale, x, baseline);
        var paint = _colors.Paint(glyph);
        if (paint != null) try {
            var work = 8192; var ink = PaintInk(paint, SvgRasterMatrix.Identity, ref work, 0);
            if (ink.Valid) {
                var visits = 8192; var passes = PaintPasses(paint, ref visits, 0);
                var density = (double)canvas.DeviceScale;
                while (true) {
                    var device = SvgRasterMatrix.Scale(density, density).Multiply(matrix);
                    var box = TransformBox(ink.Rectangle, device); if (!box.HasValue) return true;
                    var left = (int)Math.Min(canvas.Width * density, Math.Max(0, Math.Floor(box.Value.X) - 1));
                    var top = (int)Math.Min(canvas.Height * density, Math.Max(0, Math.Floor(box.Value.Y) - 1));
                    var right = (int)Math.Max(0, Math.Min(canvas.Width * density, Math.Ceiling(box.Value.X + box.Value.Width) + 1));
                    var bottom = (int)Math.Max(0, Math.Min(canvas.Height * density, Math.Ceiling(box.Value.Y + box.Value.Height) + 1));
                    if (right <= left || bottom <= top) return true;
                    var width = right - left; var height = bottom - top; var area = (long)width * height;
                    if (area <= 1024 * 1024 && area * passes <= ColorPaintRenderer.MaximumOperations) {
                        var context = new ColorPaintRenderer(this, _colors, width, height, foreground);
                        var pixels = context.Render(paint, SvgRasterMatrix.Translate(-left, -top).Multiply(device), 0);
                        if (density == canvas.DeviceScale) canvas.DrawDeviceColors(left, top, width, height, pixels, foreground.A / 255.0);
                        else {
                            var rgba = new byte[width * height * 4];
                            for (var i = 0; i < pixels.Length; i++) { var color = pixels[i].ToColor(foreground.A / 255.0);
                                rgba[i * 4] = color.R; rgba[i * 4 + 1] = color.G; rgba[i * 4 + 2] = color.B; rgba[i * 4 + 3] = color.A; }
                            canvas.DrawImageTransformed(width, height, rgba, 1 / density, 0, 0, 1 / density, left / density, top / density);
                        }
                        return true;
                    }
                    // Reduce internal antialias sampling before abandoning a valid colour glyph.
                    if (density <= 1) throw new FontLayoutException();
                    density = Math.Max(1, density / 2);
                }
            }
        } catch (FontLayoutException) { /* Invalid or excessive optional paints fall back to usable monochrome outlines. */ }
        var bitmap = _colors.Bitmaps.Nearest(glyph, Math.Abs(scale) * UnitsPerEm * canvas.FontStrikeScale);
        if (bitmap == null) return false;
        var bounds = BitmapBounds(bitmap, glyph); var image = bitmap.Image; var pixelsRgba = image.Pixels;
        if (foreground.A != 255) { pixelsRgba = (byte[])pixelsRgba.Clone(); for (var i = 3; i < pixelsRgba.Length; i += 4) pixelsRgba[i] = (byte)((pixelsRgba[i] * foreground.A + 127) / 255); }
        var placement = matrix.Multiply(new SvgRasterMatrix(bounds.Width / image.Width, 0, 0, -bounds.Height / image.Height, bounds.X, bounds.Y + bounds.Height));
        canvas.DrawImageTransformed(image.Width, image.Height, pixelsRgba, placement.A, placement.B, placement.C, placement.D, placement.E, placement.F);
        if (bitmap.DrawOutline) DrawOutlineGlyph(canvas, glyph, x, baseline, scale, italic, foreground, null);
        return true;
    }

    /// <summary>Isolated linear-light groups, with masks and transforms shared with the outline raster owner.</summary>
    private sealed class ColorPaintRenderer {
        private readonly TrueTypeFont _face;
        private readonly ColorFontData _font;
        private readonly int _width, _height, _length;
        private readonly ChartColor _foreground;
        private readonly Dictionary<ColorPaintStop[], LinearRgba[]> _lines = new();
        internal const int MaximumOperations = 16 * 1024 * 1024;
        private int _remaining = MaximumOperations;
        internal ColorPaintRenderer(TrueTypeFont face, ColorFontData font, int width, int height, ChartColor foreground) {
            _face = face; _font = font; _width = width; _height = height; _length = width * height; _foreground = foreground;
        }
        private void Charge() { _remaining -= _length; if (_remaining < 0) throw new FontLayoutException(); }
        private LinearRgba[] Surface() { Charge(); return new LinearRgba[_length]; }
        internal LinearRgba[] Render(ColorGlyphPaint paint, SvgRasterMatrix matrix, int depth) {
            if (depth > 64) throw new FontLayoutException();
            switch (paint.Kind) {
                case ColorPaintKind.Empty: return Surface();
                case ColorPaintKind.Transform: return Render(paint.Children[0], matrix.Multiply(paint.Transform), depth + 1);
                case ColorPaintKind.Layers:
                    var layers = Surface();
                    foreach (var child in paint.Children) { var next = Render(child, matrix, depth + 1); Charge(); for (var i = 0; i < _length; i++) layers[i] = LinearRgba.Over(next[i], layers[i]); }
                    return layers;
                case ColorPaintKind.Composite:
                    var source = Render(paint.Children[0], matrix, depth + 1); var backdrop = Render(paint.Children[1], matrix, depth + 1);
                    Charge(); for (var i = 0; i < _length; i++) backdrop[i] = LinearRgba.Composite(source[i], backdrop[i], paint.CompositeMode);
                    return backdrop;
                case ColorPaintKind.Glyph: case ColorPaintKind.Clip:
                    var contents = Render(paint.Children[0], matrix, depth + 1); Charge();
                    var mask = new RgbaCanvas(_width, _height, 1, null, 1, useDefaultOutlineFont: false);
                    if (paint.Kind == ColorPaintKind.Glyph)
                        mask.FillTextContours(_face.ReadGlyphContours(paint.Glyph, new FontTransform(matrix.A, matrix.C, matrix.B, matrix.D, matrix.E, matrix.F), 0), ChartColor.White, 0);
                    else {
                        var b = paint.Clip;
                        mask.FillPolygon(new[] { matrix.Transform(new ChartPoint(b.X, b.Y)), matrix.Transform(new ChartPoint(b.X + b.Width, b.Y)),
                            matrix.Transform(new ChartPoint(b.X + b.Width, b.Y + b.Height)), matrix.Transform(new ChartPoint(b.X, b.Y + b.Height)) }, ChartColor.White);
                    }
                    for (var i = 0; i < _length; i++) contents[i] = contents[i].Opacity(mask.Pixels[i * 4 + 3] / 255.0);
                    return contents;
                default:
                    var fill = Surface();
                    if (paint.Kind == ColorPaintKind.Solid) {
                        var color = LinearRgba.From(_font.Color(paint.PaletteIndex, _foreground), paint.Alpha);
                        for (var i = 0; i < _length; i++) fill[i] = color;
                    } else if (matrix.TryInvert(out var inverse)) {
                        _lines.TryGetValue(paint.Stops, out var colors);
                        var brush = new ColorPaintBrush(paint, _font, _foreground, colors); _lines[paint.Stops] = brush.Colors;
                        for (var y = 0; y < _height; y++) for (var x = 0; x < _width; x++) {
                            var point = inverse.Transform(new ChartPoint(x + 0.5, y + 0.5)); fill[y * _width + x] = brush.Sample(point.X, point.Y);
                        }
                    }
                    return fill;
            }
        }
    }
}
