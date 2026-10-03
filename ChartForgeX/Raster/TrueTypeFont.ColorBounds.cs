using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.SvgRaster;
using ChartForgeX.Typography;

namespace ChartForgeX.Raster;

internal sealed partial class TrueTypeFont {
    private Dictionary<ushort, ColorInk>? _colorInk;
    internal bool IsColorGlyph(ushort glyph) => TryColorInk(glyph, out _);
    private bool TryColorInk(ushort glyph, out ChartRect? rectangle) {
        rectangle = null; if (_colors == null) return false;
        lock (_root._viewLock) {
            _root._colorInk ??= new Dictionary<ushort, ColorInk>();
            if (_root._colorInk.TryGetValue(glyph, out var cached)) { rectangle = cached.Rectangle; return cached.Valid; }
            var paint = _colors.Paint(glyph); var bounds = ColorInk.Empty; var valid = false;
            if (paint != null) try { var work = 8192; bounds = PaintInk(paint, SvgRasterMatrix.Identity, ref work, 0); valid = bounds.Valid; } catch (FontLayoutException) { }
            if (!valid) foreach (var image in _colors.Bitmaps.All(glyph)) {
                bounds = ColorInk.Union(bounds, new ColorInk(BitmapBounds(image, glyph)));
                if (image.DrawOutline) bounds = ColorInk.Union(bounds, new ColorInk(OutlineInk(glyph)));
                valid = true;
            }
            var result = new ColorInk(valid, bounds.Rectangle); _root._colorInk[glyph] = result; rectangle = result.Rectangle; return valid;
        }
    }
    private ChartRect BitmapBounds(BitmapGlyph image, ushort glyph) {
        var bounds = image.Bounds; var outline = image.OutlineAnchor ? OutlineInk(glyph) : null;
        return outline.HasValue ? new ChartRect(bounds.X + outline.Value.X, bounds.Y + outline.Value.Y, bounds.Width, bounds.Height) : bounds;
    }
    private ColorInk PaintInk(ColorGlyphPaint paint, SvgRasterMatrix matrix, ref int work, int depth) {
        if (--work < 0 || depth > 64) throw new FontLayoutException();
        switch (paint.Kind) {
            case ColorPaintKind.Empty: return ColorInk.Empty;
            case ColorPaintKind.Solid: case ColorPaintKind.Linear: case ColorPaintKind.Radial: case ColorPaintKind.Sweep: return ColorInk.Unbounded;
            case ColorPaintKind.Transform: return PaintInk(paint.Children[0], matrix.Multiply(paint.Transform), ref work, depth + 1);
            case ColorPaintKind.Glyph:
                return ColorInk.Intersect(new ColorInk(TransformBox(OutlineInk(paint.Glyph), matrix)), PaintInk(paint.Children[0], matrix, ref work, depth + 1));
            case ColorPaintKind.Clip:
                return ColorInk.Intersect(new ColorInk(TransformBox(paint.Clip, matrix)), PaintInk(paint.Children[0], matrix, ref work, depth + 1));
            case ColorPaintKind.Layers:
                var union = ColorInk.Empty;
                foreach (var child in paint.Children) union = ColorInk.Union(union, PaintInk(child, matrix, ref work, depth + 1));
                return union;
            default:
                var source = PaintInk(paint.Children[0], matrix, ref work, depth + 1); var backdrop = PaintInk(paint.Children[1], matrix, ref work, depth + 1);
                switch (paint.CompositeMode) {
                    case 0: return ColorInk.Empty;
                    case 1: case 7: return source;
                    case 2: case 8: return backdrop;
                    case 5: case 6: return ColorInk.Intersect(source, backdrop);
                    default: return ColorInk.Union(source, backdrop);
                }
        }
    }
    private static ChartRect? TransformBox(ChartRect? box, SvgRasterMatrix matrix) {
        if (!box.HasValue) return null;
        var b = box.Value; if (b.Width <= 0 || b.Height <= 0) return null;
        var p0 = matrix.Transform(new ChartPoint(b.X, b.Y)); var p1 = matrix.Transform(new ChartPoint(b.X + b.Width, b.Y));
        var p2 = matrix.Transform(new ChartPoint(b.X, b.Y + b.Height)); var p3 = matrix.Transform(new ChartPoint(b.X + b.Width, b.Y + b.Height));
        var left = Math.Min(Math.Min(p0.X, p1.X), Math.Min(p2.X, p3.X)); var right = Math.Max(Math.Max(p0.X, p1.X), Math.Max(p2.X, p3.X));
        var bottom = Math.Min(Math.Min(p0.Y, p1.Y), Math.Min(p2.Y, p3.Y)); var top = Math.Max(Math.Max(p0.Y, p1.Y), Math.Max(p2.Y, p3.Y));
        if (!Finite(left) || !Finite(right) || !Finite(bottom) || !Finite(top)) throw new FontLayoutException();
        return new ChartRect(left, bottom, right - left, top - bottom);
    }
    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    private readonly struct ColorInk {
        internal ColorInk(ChartRect? rectangle) : this(true, rectangle) { }
        internal ColorInk(bool valid, ChartRect? rectangle) { Valid = valid; Rectangle = rectangle; }
        internal readonly bool Valid;
        internal readonly ChartRect? Rectangle;
        internal static ColorInk Empty => new(true, null);
        internal static ColorInk Unbounded => new(false, null);
        internal static ColorInk Union(ColorInk a, ColorInk b) {
            if (!a.Valid || !b.Valid) return Unbounded;
            if (!a.Rectangle.HasValue) return b; if (!b.Rectangle.HasValue) return a;
            var x = Math.Min(a.Rectangle.Value.X, b.Rectangle.Value.X); var y = Math.Min(a.Rectangle.Value.Y, b.Rectangle.Value.Y);
            return new ColorInk(new ChartRect(x, y, Math.Max(a.Rectangle.Value.X + a.Rectangle.Value.Width, b.Rectangle.Value.X + b.Rectangle.Value.Width) - x,
                Math.Max(a.Rectangle.Value.Y + a.Rectangle.Value.Height, b.Rectangle.Value.Y + b.Rectangle.Value.Height) - y));
        }
        internal static ColorInk Intersect(ColorInk a, ColorInk b) {
            if (!a.Valid) return b; if (!b.Valid) return a;
            if (!a.Rectangle.HasValue || !b.Rectangle.HasValue) return Empty;
            var x = Math.Max(a.Rectangle.Value.X, b.Rectangle.Value.X); var y = Math.Max(a.Rectangle.Value.Y, b.Rectangle.Value.Y);
            var width = Math.Min(a.Rectangle.Value.X + a.Rectangle.Value.Width, b.Rectangle.Value.X + b.Rectangle.Value.Width) - x;
            var height = Math.Min(a.Rectangle.Value.Y + a.Rectangle.Value.Height, b.Rectangle.Value.Y + b.Rectangle.Value.Height) - y;
            return width <= 0 || height <= 0 ? Empty : new ColorInk(new ChartRect(x, y, width, height));
        }
    }
}
