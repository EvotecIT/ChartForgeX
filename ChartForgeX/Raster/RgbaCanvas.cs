using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal sealed partial class RgbaCanvas {
    private const int DefaultScale = 2;
    private static readonly TrueTypeFont? DefaultOutlineFont = TrueTypeFont.TryLoadDefault();
    private readonly TrueTypeFont? _outlineFont;
    private readonly int _scale;
    private readonly int _supersamplingScale;
    private readonly int _outputScale;
    private readonly int _pixelWidth, _pixelHeight;

    public int Width { get; }
    public int Height { get; }
    public int OutputWidth => Width * _outputScale;
    public int OutputHeight => Height * _outputScale;
    public byte[] Pixels { get; }

    public RgbaCanvas(int width, int height) : this(width, height, DefaultScale, null) { }

    public RgbaCanvas(int width, int height, int scale) : this(width, height, scale, null) { }

    public RgbaCanvas(int width, int height, int scale, TrueTypeFont? outlineFont) : this(width, height, scale, outlineFont, 1) { }

    public RgbaCanvas(int width, int height, int scale, TrueTypeFont? outlineFont, int outputScale) {
        Width = width;
        Height = height;
        _supersamplingScale = Math.Max(1, scale);
        _outputScale = Math.Max(1, outputScale);
        _scale = _supersamplingScale * _outputScale;
        _outlineFont = outlineFont ?? DefaultOutlineFont;
        _pixelWidth = width * _scale;
        _pixelHeight = height * _scale;
        Pixels = new byte[_pixelWidth * _pixelHeight * 4];
    }

    public void Clear(ChartColor color) {
        for (var i = 0; i < Pixels.Length; i += 4) {
            Pixels[i] = color.R;
            Pixels[i + 1] = color.G;
            Pixels[i + 2] = color.B;
            Pixels[i + 3] = color.A;
        }
    }

    public void FillRect(double x, double y, double width, double height, ChartColor color) {
        FillRectPixels(x * _scale, y * _scale, width * _scale, height * _scale, color);
    }

    public void FillRoundedRect(double x, double y, double width, double height, double radius, ChartColor color) {
        FillRoundedRectPixels(x * _scale, y * _scale, width * _scale, height * _scale, radius * _scale, color, color);
    }

    public void FillRoundedRectVerticalGradient(double x, double y, double width, double height, double radius, ChartColor topColor, ChartColor bottomColor) {
        FillRoundedRectPixels(x * _scale, y * _scale, width * _scale, height * _scale, radius * _scale, topColor, bottomColor);
    }

    public void StrokeRect(double x, double y, double width, double height, ChartColor color, int thickness = 1) {
        DrawLine(x, y, x + width, y, color, thickness);
        DrawLine(x + width, y, x + width, y + height, color, thickness);
        DrawLine(x + width, y + height, x, y + height, color, thickness);
        DrawLine(x, y + height, x, y, color, thickness);
    }

    public void StrokeRoundedRect(double x, double y, double width, double height, double radius, ChartColor color, double thickness = 1) {
        StrokeRoundedRectPixels(x * _scale, y * _scale, width * _scale, height * _scale, radius * _scale, color, Math.Max(1, thickness * _scale));
    }

    public void DrawLine(double x0, double y0, double x1, double y1, ChartColor color, double thickness) {
        DrawLinePixels(x0 * _scale, y0 * _scale, x1 * _scale, y1 * _scale, Math.Max(1, thickness * _scale), color);
    }

    public void DrawDashedLine(double x0, double y0, double x1, double y1, ChartColor color, double thickness, double dash = 6, double gap = 5) {
        var dx = x1 - x0;
        var dy = y1 - y0;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length <= 0.000001) {
            DrawLine(x0, y0, x1, y1, color, thickness);
            return;
        }

        var ux = dx / length;
        var uy = dy / length;
        for (var offset = 0.0; offset < length; offset += dash + gap) {
            var end = Math.Min(length, offset + dash);
            DrawLine(x0 + ux * offset, y0 + uy * offset, x0 + ux * end, y0 + uy * end, color, thickness);
        }
    }

    public void DrawCircle(double cx, double cy, double radius, ChartColor color) {
        DrawSoftCirclePixels(cx * _scale, cy * _scale, radius * _scale, color);
    }

    public void DrawCircleOutline(double cx, double cy, double radius, ChartColor color, double thickness = 1) {
        DrawArc(cx, cy, radius, 0, Math.PI * 2, color, thickness);
    }

    public void DrawArc(double cx, double cy, double radius, double startAngle, double endAngle, ChartColor color, double thickness) {
        DrawArc(cx, cy, radius, startAngle, endAngle, color, thickness, RasterLineCap.Round);
    }

    internal void DrawArc(double cx, double cy, double radius, double startAngle, double endAngle, ChartColor color, double thickness, RasterLineCap lineCap) {
        var fullCircle = Math.Abs(endAngle - startAngle) >= Math.PI * 2 - 0.000001;
        var start = fullCircle ? 0 : NormalizeAngle(startAngle);
        var end = fullCircle ? Math.PI * 2 : NormalizeAngle(endAngle);
        DrawArcPixels(cx * _scale, cy * _scale, radius * _scale, start, end, color, Math.Max(1, thickness * _scale), lineCap);
    }

    public void FillPolygon(IReadOnlyList<ChartPoint> points, ChartColor color) {
        if (points.Count < 3) return;
        var scaled = new List<ChartPoint>(points.Count);
        foreach (var point in points) scaled.Add(new ChartPoint(point.X * _scale, point.Y * _scale));
        FillPolygonPixels(scaled, color, color);
    }

    public void FillCompoundPolygon(IReadOnlyList<List<ChartPoint>> rings, ChartColor color) {
        if (rings.Count == 0) return;
        var scaled = new List<List<ChartPoint>>(rings.Count);
        foreach (var ring in rings) {
            if (ring.Count < 3) continue;
            var scaledRing = new List<ChartPoint>(ring.Count);
            foreach (var point in ring) scaledRing.Add(new ChartPoint(point.X * _scale, point.Y * _scale));
            scaled.Add(scaledRing);
        }

        FillContoursPixels(scaled, color, RasterFillRule.EvenOdd);
    }

    public void FillPolygonVerticalGradient(IReadOnlyList<ChartPoint> points, ChartColor topColor, ChartColor bottomColor) {
        if (points.Count < 3) return;
        var scaled = new List<ChartPoint>(points.Count);
        foreach (var point in points) scaled.Add(new ChartPoint(point.X * _scale, point.Y * _scale));
        FillPolygonPixels(scaled, topColor, bottomColor);
    }

    public void FillRingSlice(double cx, double cy, double outerRadius, double innerRadius, double startAngle, double endAngle, ChartColor color) {
        FillRingSlicePixels(cx * _scale, cy * _scale, outerRadius * _scale, innerRadius * _scale, startAngle, endAngle, color);
    }

    public void DrawTextTiny(double x, double y, string text, ChartColor color, int scale = 2) {
        var font = _outlineFont;
        if (font != null && font.Draw(this, x, y, text, color, OutlineFontSize(scale))) return;

        var cursor = (int)Math.Round(x * _scale);
        var glyphScale = Math.Max(1, scale * _scale);
        foreach (var ch in text) {
            DrawGlyph(cursor, (int)Math.Round(y * _scale), ch, color, glyphScale);
            cursor += TinyFont.AdvanceFor(ch) * glyphScale;
        }
    }

    public void DrawText(double x, double y, string text, ChartColor color, double fontSize) {
        var font = _outlineFont;
        if (font != null && font.Draw(this, x, y, text, color, Math.Max(1, fontSize))) return;
        DrawTextTiny(x, y, text, color, FallbackScaleForFontSize(fontSize));
    }

    public void DrawTextEmphasized(double x, double y, string text, ChartColor color, double fontSize) {
        if (string.IsNullOrEmpty(text) || color.A == 0) return;
        DrawText(x, y, text, color, fontSize);
        DrawText(x + EmphasisOffset(fontSize), y, text, color, fontSize);
    }

    public void DrawImage(int x, int y, int width, int height, byte[] rgba) {
        if (rgba == null) throw new ArgumentNullException(nameof(rgba));
        if (width <= 0 || height <= 0) return;
        if (rgba.Length < width * height * 4) throw new ArgumentException("RGBA buffer is smaller than the requested image dimensions.", nameof(rgba));
        var targetX0 = x * _scale;
        var targetY0 = y * _scale;
        for (var yy = 0; yy < height; yy++) for (var xx = 0; xx < width; xx++) {
            var dx = targetX0 + xx;
            var dy = targetY0 + yy;
            if (dx < 0 || dy < 0 || dx >= _pixelWidth || dy >= _pixelHeight) continue;
            var source = (yy * width + xx) * 4;
            var alpha = rgba[source + 3];
            if (alpha == 0) continue;
            BlendPixel(dx, dy, ChartColor.FromRgba(rgba[source], rgba[source + 1], rgba[source + 2], alpha));
        }
    }

    public void DrawImageScaled(int x, int y, int destinationWidth, int destinationHeight, int sourceWidth, int sourceHeight, byte[] rgba) {
        if (rgba == null) throw new ArgumentNullException(nameof(rgba));
        if (destinationWidth <= 0 || destinationHeight <= 0 || sourceWidth <= 0 || sourceHeight <= 0) return;
        if (rgba.Length < sourceWidth * sourceHeight * 4) throw new ArgumentException("RGBA buffer is smaller than the requested source dimensions.", nameof(rgba));
        DrawImageScaled(x, y, destinationWidth, destinationHeight, sourceWidth, sourceHeight, rgba, 0, 0, sourceWidth, sourceHeight);
    }

    public void DrawImageScaled(int x, int y, int destinationWidth, int destinationHeight, int sourceWidth, int sourceHeight, byte[] rgba, double sourceX, double sourceY, double sourceRectWidth, double sourceRectHeight) {
        if (rgba == null) throw new ArgumentNullException(nameof(rgba));
        if (destinationWidth <= 0 || destinationHeight <= 0 || sourceWidth <= 0 || sourceHeight <= 0 || sourceRectWidth <= 0 || sourceRectHeight <= 0) return;
        if (rgba.Length < sourceWidth * sourceHeight * 4) throw new ArgumentException("RGBA buffer is smaller than the requested source dimensions.", nameof(rgba));
        var scaledDestinationWidth = destinationWidth * _scale;
        var scaledDestinationHeight = destinationHeight * _scale;
        if (sourceX == 0 && sourceY == 0 && Math.Abs(sourceRectWidth - sourceWidth) < 0.000001 && Math.Abs(sourceRectHeight - sourceHeight) < 0.000001 &&
            scaledDestinationWidth == sourceWidth && scaledDestinationHeight == sourceHeight) {
            DrawImage(x, y, sourceWidth, sourceHeight, rgba);
            return;
        }

        var targetX0 = x * _scale;
        var targetY0 = y * _scale;
        for (var dy = 0; dy < scaledDestinationHeight; dy++) for (var dx = 0; dx < scaledDestinationWidth; dx++) {
            var targetX = targetX0 + dx;
            var targetY = targetY0 + dy;
            if (targetX < 0 || targetY < 0 || targetX >= _pixelWidth || targetY >= _pixelHeight) continue;
            var color = SampleImageBilinear(
                rgba,
                sourceWidth,
                sourceHeight,
                sourceX + (dx + 0.5) * sourceRectWidth / scaledDestinationWidth - 0.5,
                sourceY + (dy + 0.5) * sourceRectHeight / scaledDestinationHeight - 0.5);
            if (color.A == 0) continue;
            BlendPixel(targetX, targetY, color);
        }
    }

    public void DrawTextRotated(double anchorX, double anchorY, string text, ChartColor color, double fontSize, double degrees, double originX, double originY) {
        DrawTextRotatedCore(anchorX, anchorY, text, color, fontSize, degrees, originX, originY, false);
    }

    public void DrawTextRotatedEmphasized(double anchorX, double anchorY, string text, ChartColor color, double fontSize, double degrees, double originX, double originY) {
        DrawTextRotatedCore(anchorX, anchorY, text, color, fontSize, degrees, originX, originY, true);
    }

    private void DrawTextRotatedCore(double anchorX, double anchorY, string text, ChartColor color, double fontSize, double degrees, double originX, double originY, bool emphasized) {
        if (string.IsNullOrEmpty(text) || color.A == 0) return;
        if (Math.Abs(degrees) < 0.001) {
            if (emphasized) DrawTextEmphasized(anchorX - originX, anchorY - originY, text, color, fontSize);
            else DrawText(anchorX - originX, anchorY - originY, text, color, fontSize);
            return;
        }

        var padding = Math.Max(4, fontSize * 0.45);
        var textWidth = emphasized ? MeasureTextEmphasizedWidth(text, fontSize, _outlineFont) : MeasureTextWidth(text, fontSize, _outlineFont);
        var textHeight = MeasureTextHeight(fontSize, _outlineFont);
        var buffer = new RgbaCanvas((int)Math.Ceiling(textWidth + padding * 2), (int)Math.Ceiling(textHeight + padding * 2), _scale, _outlineFont);
        if (emphasized) buffer.DrawTextEmphasized(padding, padding, text, color, fontSize);
        else buffer.DrawText(padding, padding, text, color, fontSize);

        var radians = degrees * Math.PI / 180.0;
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);

        for (var sy = 0; sy < buffer._pixelHeight; sy++) for (var sx = 0; sx < buffer._pixelWidth; sx++) {
            var source = (sy * buffer._pixelWidth + sx) * 4;
            var alpha = buffer.Pixels[source + 3];
            if (alpha == 0) continue;

            var localX = (sx + 0.5) / _scale - padding - originX;
            var localY = (sy + 0.5) / _scale - padding - originY;
            var destX = anchorX + localX * cos - localY * sin;
            var destY = anchorY + localX * sin + localY * cos;
            BlendPixel(
                (int)Math.Round(destX * _scale),
                (int)Math.Round(destY * _scale),
                ChartColor.FromRgba(buffer.Pixels[source], buffer.Pixels[source + 1], buffer.Pixels[source + 2], alpha));
        }
    }

    public void DrawTextTinyRotated(double anchorX, double anchorY, string text, ChartColor color, int scale, double degrees, double originX, double originY) {
        if (string.IsNullOrEmpty(text) || color.A == 0) return;
        if (Math.Abs(degrees) < 0.001) {
            DrawTextTiny(anchorX - originX, anchorY - originY, text, color, scale);
            return;
        }

        var padding = Math.Max(3, scale * 3);
        var textWidth = MeasureTextTinyWidth(text, scale, _outlineFont);
        var textHeight = MeasureTextTinyHeight(scale, _outlineFont);
        var buffer = new RgbaCanvas((int)Math.Ceiling(textWidth + padding * 2), (int)Math.Ceiling(textHeight + padding * 2), _scale, _outlineFont);
        buffer.DrawTextTiny(padding, padding, text, color, scale);

        var radians = degrees * Math.PI / 180.0;
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);

        for (var sy = 0; sy < buffer._pixelHeight; sy++) for (var sx = 0; sx < buffer._pixelWidth; sx++) {
            var source = (sy * buffer._pixelWidth + sx) * 4;
            var alpha = buffer.Pixels[source + 3];
            if (alpha == 0) continue;

            var localX = (sx + 0.5) / _scale - padding - originX;
            var localY = (sy + 0.5) / _scale - padding - originY;
            var destX = anchorX + localX * cos - localY * sin;
            var destY = anchorY + localX * sin + localY * cos;
            BlendPixel(
                (int)Math.Round(destX * _scale),
                (int)Math.Round(destY * _scale),
                ChartColor.FromRgba(buffer.Pixels[source], buffer.Pixels[source + 1], buffer.Pixels[source + 2], alpha));
        }
    }

    public static double MeasureTextTinyWidth(string text, int scale) {
        return MeasureTextTinyWidth(text, scale, null);
    }

    public static double MeasureTextTinyWidth(string text, int scale, TrueTypeFont? outlineFont) {
        var font = outlineFont ?? DefaultOutlineFont;
        if (font != null) return font.Measure(text, OutlineFontSize(scale));

        var width = 0;
        foreach (var ch in text) width += TinyFont.AdvanceFor(ch);
        return width * Math.Max(1, scale);
    }

    public static double MeasureTextWidth(string text, double fontSize, TrueTypeFont? outlineFont) {
        var font = outlineFont ?? DefaultOutlineFont;
        if (font != null) return font.Measure(text, Math.Max(1, fontSize));
        return MeasureTextTinyWidth(text, FallbackScaleForFontSize(fontSize), null);
    }

    public static double MeasureTextEmphasizedWidth(string text, double fontSize, TrueTypeFont? outlineFont) => string.IsNullOrEmpty(text) ? 0 : MeasureTextWidth(text, fontSize, outlineFont) + EmphasisOffset(fontSize);

    public static double MeasureTextHeight(double fontSize, TrueTypeFont? outlineFont) {
        var font = outlineFont ?? DefaultOutlineFont;
        if (font != null) return font.LineHeight(Math.Max(1, fontSize));
        return TinyFont.Height * FallbackScaleForFontSize(fontSize);
    }

    public static double MeasureTextTinyHeight(int scale) {
        return MeasureTextTinyHeight(scale, null);
    }

    public static double MeasureTextTinyHeight(int scale, TrueTypeFont? outlineFont) {
        var font = outlineFont ?? DefaultOutlineFont;
        return font != null ? OutlineFontSize(scale) : TinyFont.Height * Math.Max(1, scale);
    }

    private static double OutlineFontSize(int scale) => TinyFont.Height * Math.Max(1, scale) * 1.45;
    private static int FallbackScaleForFontSize(double fontSize) => Math.Max(1, (int)Math.Round(Math.Max(1, fontSize) / OutlineFontSize(1)));
    private static double EmphasisOffset(double fontSize) => Math.Max(0.24, Math.Min(0.58, fontSize * 0.025));

    public byte[] ToOutputPixels() {
        if (_scale == 1) return Pixels;
        var output = new byte[OutputWidth * OutputHeight * 4];
        var samples = _supersamplingScale * _supersamplingScale;

        for (var y = 0; y < OutputHeight; y++) for (var x = 0; x < OutputWidth; x++) {
            var sumA = 0;
            var sumR = 0;
            var sumG = 0;
            var sumB = 0;

            for (var sy = 0; sy < _supersamplingScale; sy++) for (var sx = 0; sx < _supersamplingScale; sx++) {
                var src = ((y * _supersamplingScale + sy) * _pixelWidth + x * _supersamplingScale + sx) * 4;
                var alpha = Pixels[src + 3];
                sumA += alpha;
                sumR += Pixels[src] * alpha;
                sumG += Pixels[src + 1] * alpha;
                sumB += Pixels[src + 2] * alpha;
            }

            var dst = (y * OutputWidth + x) * 4;
            if (sumA == 0) continue;
            output[dst] = (byte)(sumR / sumA);
            output[dst + 1] = (byte)(sumG / sumA);
            output[dst + 2] = (byte)(sumB / sumA);
            output[dst + 3] = (byte)(sumA / samples);
        }

        return output;
    }

    internal void FillContours(IReadOnlyList<List<ChartPoint>> contours, ChartColor color, RasterFillRule fillRule = RasterFillRule.EvenOdd) {
        if (contours.Count == 0) return;
        var scaled = new List<List<ChartPoint>>(contours.Count);
        foreach (var contour in contours) {
            if (contour.Count < 3) continue;
            var points = new List<ChartPoint>(contour.Count);
            foreach (var point in contour) points.Add(new ChartPoint(point.X * _scale, point.Y * _scale));
            scaled.Add(points);
        }

        FillContoursPixels(scaled, color, fillRule);
    }

    private void FillRectPixels(double x, double y, double width, double height, ChartColor color) {
        var x1 = Math.Max(0, (int)Math.Floor(x)); var y1 = Math.Max(0, (int)Math.Floor(y));
        var x2 = Math.Min(_pixelWidth, (int)Math.Ceiling(x + width)); var y2 = Math.Min(_pixelHeight, (int)Math.Ceiling(y + height));
        for (var yy = y1; yy < y2; yy++) for (var xx = x1; xx < x2; xx++) BlendPixel(xx, yy, color);
    }

    private void FillRoundedRectPixels(double x, double y, double width, double height, double radius, ChartColor topColor, ChartColor bottomColor) {
        var feather = 1.0;
        var x1 = Math.Max(0, (int)Math.Floor(x - feather)); var y1 = Math.Max(0, (int)Math.Floor(y - feather));
        var x2 = Math.Min(_pixelWidth, (int)Math.Ceiling(x + width + feather)); var y2 = Math.Min(_pixelHeight, (int)Math.Ceiling(y + height + feather));
        for (var yy = y1; yy < y2; yy++) for (var xx = x1; xx < x2; xx++) {
            var color = GradientColor(topColor, bottomColor, (yy + 0.5 - y) / Math.Max(0.000001, height));
            var distance = RoundedRectSignedDistance(xx + 0.5, yy + 0.5, x, y, width, height, radius);
            if (distance <= 0) {
                BlendPixel(xx, yy, color);
            } else if (distance < feather) {
                BlendPixel(xx, yy, WithOpacity(color, feather - distance));
            }
        }
    }

    private void StrokeRoundedRectPixels(double x, double y, double width, double height, double radius, ChartColor color, double thickness) {
        if (width <= 0 || height <= 0 || thickness <= 0) return;
        var feather = 1.0;
        var x1 = Math.Max(0, (int)Math.Floor(x - feather)); var y1 = Math.Max(0, (int)Math.Floor(y - feather));
        var x2 = Math.Min(_pixelWidth, (int)Math.Ceiling(x + width + feather)); var y2 = Math.Min(_pixelHeight, (int)Math.Ceiling(y + height + feather));
        var strokeRadius = thickness / 2.0;
        var centerInset = strokeRadius;
        var centerWidth = Math.Max(0.000001, width - thickness);
        var centerHeight = Math.Max(0.000001, height - thickness);
        var centerRadius = Math.Max(0, radius - strokeRadius);

        for (var yy = y1; yy < y2; yy++) for (var xx = x1; xx < x2; xx++) {
            var px = xx + 0.5;
            var py = yy + 0.5;
            var distance = Math.Abs(RoundedRectSignedDistance(px, py, x + centerInset, y + centerInset, centerWidth, centerHeight, centerRadius));
            if (distance <= strokeRadius) {
                BlendPixel(xx, yy, color);
            } else if (distance < strokeRadius + feather) {
                BlendPixel(xx, yy, WithOpacity(color, strokeRadius + feather - distance));
            }
        }
    }

    private static double RoundedRectSignedDistance(double px, double py, double x, double y, double width, double height, double radius) {
        if (width <= 0 || height <= 0) return double.PositiveInfinity;
        radius = Math.Max(0, Math.Min(radius, Math.Min(width, height) / 2));
        var halfWidth = width / 2.0;
        var halfHeight = height / 2.0;
        var qx = Math.Abs(px - (x + halfWidth)) - (halfWidth - radius);
        var qy = Math.Abs(py - (y + halfHeight)) - (halfHeight - radius);
        var outsideX = Math.Max(qx, 0);
        var outsideY = Math.Max(qy, 0);
        var outside = Math.Sqrt(outsideX * outsideX + outsideY * outsideY);
        var inside = Math.Min(Math.Max(qx, qy), 0);
        return outside + inside - radius;
    }

    private void DrawCirclePixels(double cx, double cy, double radius, ChartColor color) {
        var r2 = radius * radius;
        var x1 = Math.Max(0, (int)Math.Floor(cx - radius)); var y1 = Math.Max(0, (int)Math.Floor(cy - radius));
        var x2 = Math.Min(_pixelWidth - 1, (int)Math.Ceiling(cx + radius)); var y2 = Math.Min(_pixelHeight - 1, (int)Math.Ceiling(cy + radius));
        for (var y = y1; y <= y2; y++) for (var x = x1; x <= x2; x++) {
            var dx = x + .5 - cx; var dy = y + .5 - cy;
            if (dx * dx + dy * dy <= r2) BlendPixel(x, y, color);
        }
    }

    private void DrawLinePixels(double x0, double y0, double x1, double y1, double thickness, ChartColor color) {
        if (thickness <= 0 || color.A == 0) return;

        var radius = Math.Max(0.5, thickness / 2.0);
        var feather = 1.0;
        var minX = Math.Max(0, (int)Math.Floor(Math.Min(x0, x1) - radius - feather));
        var minY = Math.Max(0, (int)Math.Floor(Math.Min(y0, y1) - radius - feather));
        var maxX = Math.Min(_pixelWidth - 1, (int)Math.Ceiling(Math.Max(x0, x1) + radius + feather));
        var maxY = Math.Min(_pixelHeight - 1, (int)Math.Ceiling(Math.Max(y0, y1) + radius + feather));
        var vx = x1 - x0;
        var vy = y1 - y0;
        var lengthSquared = vx * vx + vy * vy;

        if (lengthSquared <= 0.000001) {
            DrawSoftCirclePixels(x0, y0, radius, color);
            return;
        }

        for (var y = minY; y <= maxY; y++) for (var x = minX; x <= maxX; x++) {
            var px = x + 0.5;
            var py = y + 0.5;
            var t = ((px - x0) * vx + (py - y0) * vy) / lengthSquared;
            t = Math.Max(0, Math.Min(1, t));
            var closestX = x0 + vx * t;
            var closestY = y0 + vy * t;
            var dx = px - closestX;
            var dy = py - closestY;
            var distance = Math.Sqrt(dx * dx + dy * dy);
            if (distance <= radius) {
                BlendPixel(x, y, color);
            } else if (distance < radius + feather) {
                BlendPixel(x, y, WithOpacity(color, radius + feather - distance));
            }
        }
    }

    private void DrawArcPixels(double cx, double cy, double radius, double startAngle, double endAngle, ChartColor color, double thickness, RasterLineCap lineCap) {
        if (radius <= 0 || thickness <= 0 || color.A == 0) return;
        var strokeRadius = Math.Max(0.5, thickness / 2.0);
        var feather = 1.0;
        var outer = radius + strokeRadius + feather;
        var x1 = Math.Max(0, (int)Math.Floor(cx - outer));
        var y1 = Math.Max(0, (int)Math.Floor(cy - outer));
        var x2 = Math.Min(_pixelWidth - 1, (int)Math.Ceiling(cx + outer));
        var y2 = Math.Min(_pixelHeight - 1, (int)Math.Ceiling(cy + outer));
        var fullCircle = ArcSweep(startAngle, endAngle) >= Math.PI * 2 - 0.000001;

        for (var y = y1; y <= y2; y++) for (var x = x1; x <= x2; x++) {
            var dx = x + 0.5 - cx;
            var dy = y + 0.5 - cy;
            var distanceFromCenter = Math.Sqrt(dx * dx + dy * dy);
            var distance = Math.Abs(distanceFromCenter - radius);
            if (!fullCircle && !AngleInArc(Math.Atan2(dy, dx), startAngle, endAngle)) continue;
            if (distance <= strokeRadius) {
                BlendPixel(x, y, color);
            } else if (distance < strokeRadius + feather) {
                BlendPixel(x, y, WithOpacity(color, strokeRadius + feather - distance));
            }
        }

        if (!fullCircle && lineCap == RasterLineCap.Round) {
            DrawSoftCirclePixels(cx + Math.Cos(startAngle) * radius, cy + Math.Sin(startAngle) * radius, strokeRadius, color);
            DrawSoftCirclePixels(cx + Math.Cos(endAngle) * radius, cy + Math.Sin(endAngle) * radius, strokeRadius, color);
        }
    }

    private void DrawSoftCirclePixels(double cx, double cy, double radius, ChartColor color) {
        var feather = 1.0;
        var x1 = Math.Max(0, (int)Math.Floor(cx - radius - feather));
        var y1 = Math.Max(0, (int)Math.Floor(cy - radius - feather));
        var x2 = Math.Min(_pixelWidth - 1, (int)Math.Ceiling(cx + radius + feather));
        var y2 = Math.Min(_pixelHeight - 1, (int)Math.Ceiling(cy + radius + feather));
        for (var y = y1; y <= y2; y++) for (var x = x1; x <= x2; x++) {
            var dx = x + 0.5 - cx;
            var dy = y + 0.5 - cy;
            var distance = Math.Sqrt(dx * dx + dy * dy);
            if (distance <= radius) {
                BlendPixel(x, y, color);
            } else if (distance < radius + feather) {
                BlendPixel(x, y, WithOpacity(color, radius + feather - distance));
            }
        }
    }

    private void FillPolygonPixels(IReadOnlyList<ChartPoint> points, ChartColor topColor, ChartColor bottomColor) {
        if (points.Count < 3) return;
        var minX = double.PositiveInfinity;
        var maxX = double.NegativeInfinity;
        var minY = double.PositiveInfinity;
        var maxY = double.NegativeInfinity;
        foreach (var point in points) {
            minX = Math.Min(minX, point.X);
            maxX = Math.Max(maxX, point.X);
            minY = Math.Min(minY, point.Y);
            maxY = Math.Max(maxY, point.Y);
        }

        var feather = 1.0;
        var xStart = Math.Max(0, (int)Math.Floor(minX - feather));
        var xEnd = Math.Min(_pixelWidth - 1, (int)Math.Ceiling(maxX + feather));
        var yStart = Math.Max(0, (int)Math.Floor(minY - feather));
        var yEnd = Math.Min(_pixelHeight - 1, (int)Math.Ceiling(maxY + feather));

        for (var y = yStart; y <= yEnd; y++) for (var x = xStart; x <= xEnd; x++) {
            var px = x + 0.5;
            var py = y + 0.5;
            var color = GradientColor(topColor, bottomColor, (py - minY) / Math.Max(0.000001, maxY - minY));
            if (PointInPolygon(px, py, points)) {
                BlendPixel(x, y, color);
            } else {
                var distance = Math.Sqrt(DistanceToPolygonSquared(px, py, points));
                if (distance < feather) BlendPixel(x, y, WithOpacity(color, feather - distance));
            }
        }
    }

    private static bool PointInPolygon(double px, double py, IReadOnlyList<ChartPoint> points) {
        var inside = false;
        for (int i = 0, j = points.Count - 1; i < points.Count; j = i++) {
            var a = points[i];
            var b = points[j];
            if ((a.Y > py) != (b.Y > py) && px < (b.X - a.X) * (py - a.Y) / (b.Y - a.Y) + a.X) inside = !inside;
        }

        return inside;
    }

    private static double DistanceToPolygonSquared(double px, double py, IReadOnlyList<ChartPoint> points) {
        var min = double.PositiveInfinity;
        for (var i = 0; i < points.Count; i++) {
            var a = points[i];
            var b = points[(i + 1) % points.Count];
            min = Math.Min(min, DistanceToSegmentSquared(px, py, a.X, a.Y, b.X, b.Y));
        }

        return min;
    }

    private static double DistanceToSegmentSquared(double px, double py, double x0, double y0, double x1, double y1) {
        var vx = x1 - x0;
        var vy = y1 - y0;
        var lengthSquared = vx * vx + vy * vy;
        if (lengthSquared <= 0.000001) {
            var dx = px - x0;
            var dy = py - y0;
            return dx * dx + dy * dy;
        }

        var t = ((px - x0) * vx + (py - y0) * vy) / lengthSquared;
        t = Math.Max(0, Math.Min(1, t));
        var closestX = x0 + vx * t;
        var closestY = y0 + vy * t;
        var cx = px - closestX;
        var cy = py - closestY;
        return cx * cx + cy * cy;
    }

    private void FillContoursPixels(IReadOnlyList<List<ChartPoint>> contours, ChartColor color, RasterFillRule fillRule) {
        if (contours.Count == 0) return;
        ScanFillSpans(contours, fillRule, (y, _, left, right) => {
            var xStart = Math.Max(0, (int)Math.Floor(left));
            var xEnd = Math.Min(_pixelWidth - 1, (int)Math.Ceiling(right));
            for (var x = xStart; x <= xEnd; x++) {
                var coverage = Math.Min(x + 1.0, right) - Math.Max(x, left);
                if (coverage > 0) BlendPixel(x, y, coverage >= 1 ? color : WithOpacity(color, coverage));
            }
        });
    }

    private void FillRingSlicePixels(double cx, double cy, double outerRadius, double innerRadius, double startAngle, double endAngle, ChartColor color) {
        var feather = 1.0;
        var x1 = Math.Max(0, (int)Math.Floor(cx - outerRadius - feather));
        var y1 = Math.Max(0, (int)Math.Floor(cy - outerRadius - feather));
        var x2 = Math.Min(_pixelWidth - 1, (int)Math.Ceiling(cx + outerRadius + feather));
        var y2 = Math.Min(_pixelHeight - 1, (int)Math.Ceiling(cy + outerRadius + feather));
        var fullCircle = Math.Abs(endAngle - startAngle) >= Math.PI * 2 - 0.000001;
        startAngle = fullCircle ? 0 : NormalizeAngle(startAngle);
        endAngle = fullCircle ? Math.PI * 2 : NormalizeAngle(endAngle);
        var offsets = new[] { 0.25, 0.75 };
        var samples = offsets.Length * offsets.Length;

        for (var y = y1; y <= y2; y++) for (var x = x1; x <= x2; x++) {
            var covered = 0;
            foreach (var oy in offsets) foreach (var ox in offsets) {
                var dx = x + ox - cx;
                var dy = y + oy - cy;
                var distance = Math.Sqrt(dx * dx + dy * dy);
                if (distance > outerRadius || distance < innerRadius) continue;
                if (fullCircle || AngleInArc(Math.Atan2(dy, dx), startAngle, endAngle)) covered++;
            }

            if (covered == samples) {
                BlendPixel(x, y, color);
            } else if (covered > 0) {
                BlendPixel(x, y, WithOpacity(color, covered / (double)samples));
            }
        }
    }

    private static double NormalizeAngle(double angle) {
        var twoPi = Math.PI * 2;
        angle %= twoPi;
        if (angle < 0) angle += twoPi;
        return angle;
    }

    private static double ArcSweep(double startAngle, double endAngle) {
        var sweep = endAngle - startAngle;
        if (sweep < 0) sweep += Math.PI * 2;
        return sweep;
    }

    private static bool AngleInArc(double angle, double startAngle, double endAngle) {
        angle = NormalizeAngle(angle);
        if (Math.Abs(ArcSweep(startAngle, endAngle) - Math.PI * 2) < 0.000001) return true;
        return startAngle <= endAngle ? angle >= startAngle && angle <= endAngle : angle >= startAngle || angle <= endAngle;
    }

    private void DrawGlyph(int x, int y, char ch, ChartColor color, int scale) {
        var glyph = TinyFont.GetBitmap(ch);
        var thickness = Math.Max(1.4, scale * 0.8);
        var radius = thickness / 2.0;
        for (var row = 0; row < TinyFont.Height; row++) {
            for (var col = 0; col < TinyFont.Width; col++) {
                if (!GlyphCell(glyph, row, col)) continue;
                var cx = x + (col + 0.5) * scale;
                var cy = y + (row + 0.5) * scale;
                var connected = false;
                if (GlyphCell(glyph, row, col + 1)) {
                    DrawStrokePixels(cx, cy, x + (col + 1.5) * scale, cy, thickness, color);
                    connected = true;
                }

                if (GlyphCell(glyph, row + 1, col)) {
                    DrawStrokePixels(cx, cy, cx, y + (row + 1.5) * scale, thickness, color);
                    connected = true;
                }

                if (GlyphCell(glyph, row + 1, col + 1) && !GlyphCell(glyph, row, col + 1) && !GlyphCell(glyph, row + 1, col)) {
                    DrawStrokePixels(cx, cy, x + (col + 1.5) * scale, y + (row + 1.5) * scale, thickness, color);
                    connected = true;
                }

                if (GlyphCell(glyph, row + 1, col - 1) && !GlyphCell(glyph, row, col - 1) && !GlyphCell(glyph, row + 1, col)) {
                    DrawStrokePixels(cx, cy, x + (col - 0.5) * scale, y + (row + 1.5) * scale, thickness, color);
                    connected = true;
                }

                if (!connected) DrawSoftCirclePixels(cx, cy, radius, color);
            }
        }
    }

    private static bool GlyphCell(byte[] glyph, int row, int col) {
        if (row < 0 || row >= TinyFont.Height || col < 0 || col >= TinyFont.Width) return false;
        return ((glyph[row] >> (TinyFont.Width - 1 - col)) & 1) == 1;
    }

    private void DrawStrokePixels(double x0, double y0, double x1, double y1, double thickness, ChartColor color) {
        DrawLinePixels(x0, y0, x1, y1, thickness, color);
    }

    private static ChartColor WithOpacity(ChartColor color, double opacity) {
        opacity = Math.Max(0, Math.Min(1, opacity));
        return ChartColor.FromRgba(color.R, color.G, color.B, (byte)Math.Round(color.A * opacity));
    }

    private static ChartColor SampleImageBilinear(byte[] rgba, int width, int height, double x, double y) {
        var x0 = Math.Max(0, Math.Min(width - 1, (int)Math.Floor(x)));
        var y0 = Math.Max(0, Math.Min(height - 1, (int)Math.Floor(y)));
        var x1 = Math.Min(width - 1, x0 + 1);
        var y1 = Math.Min(height - 1, y0 + 1);
        var fx = Math.Max(0, Math.Min(1, x - x0));
        var fy = Math.Max(0, Math.Min(1, y - y0));
        var top = MixSample(ReadSample(rgba, width, x0, y0), ReadSample(rgba, width, x1, y0), fx);
        var bottom = MixSample(ReadSample(rgba, width, x0, y1), ReadSample(rgba, width, x1, y1), fx);
        return ToColor(MixSample(top, bottom, fy));
    }

    private static ImageSample ReadSample(byte[] rgba, int width, int x, int y) {
        var offset = (y * width + x) * 4;
        var alpha = rgba[offset + 3] / 255.0;
        return new ImageSample(rgba[offset] * alpha, rgba[offset + 1] * alpha, rgba[offset + 2] * alpha, alpha);
    }

    private static ImageSample MixSample(ImageSample left, ImageSample right, double amount) {
        var inverse = 1 - amount;
        return new ImageSample(
            left.Red * inverse + right.Red * amount,
            left.Green * inverse + right.Green * amount,
            left.Blue * inverse + right.Blue * amount,
            left.Alpha * inverse + right.Alpha * amount);
    }

    private static ChartColor ToColor(ImageSample sample) {
        if (sample.Alpha <= 0.000001) return ChartColor.Transparent;
        return ChartColor.FromRgba(
            ClampByte(sample.Red / sample.Alpha),
            ClampByte(sample.Green / sample.Alpha),
            ClampByte(sample.Blue / sample.Alpha),
            ClampByte(sample.Alpha * 255));
    }

    private static byte ClampByte(double value) => (byte)Math.Round(Math.Max(0, Math.Min(255, value)));

    private readonly struct ImageSample {
        public readonly double Red;
        public readonly double Green;
        public readonly double Blue;
        public readonly double Alpha;

        public ImageSample(double red, double green, double blue, double alpha) {
            Red = red;
            Green = green;
            Blue = blue;
            Alpha = alpha;
        }
    }

    private static ChartColor GradientColor(ChartColor topColor, ChartColor bottomColor, double amount) {
        amount = Math.Max(0, Math.Min(1, amount));
        return ChartColor.FromRgba(
            (byte)Math.Round(topColor.R + (bottomColor.R - topColor.R) * amount),
            (byte)Math.Round(topColor.G + (bottomColor.G - topColor.G) * amount),
            (byte)Math.Round(topColor.B + (bottomColor.B - topColor.B) * amount),
            (byte)Math.Round(topColor.A + (bottomColor.A - topColor.A) * amount));
    }

    private void BlendPixel(int x, int y, ChartColor src) {
        if (x < 0 || y < 0 || x >= _pixelWidth || y >= _pixelHeight || src.A == 0) return;
        var i = (y * _pixelWidth + x) * 4;
        if (src.A == 255) { Pixels[i] = src.R; Pixels[i + 1] = src.G; Pixels[i + 2] = src.B; Pixels[i + 3] = 255; return; }
        var sa = src.A / 255.0;
        var da = Pixels[i + 3] / 255.0;
        var oa = sa + da * (1 - sa);
        if (oa <= 0) return;
        Pixels[i] = (byte)((src.R * sa + Pixels[i] * da * (1 - sa)) / oa);
        Pixels[i + 1] = (byte)((src.G * sa + Pixels[i + 1] * da * (1 - sa)) / oa);
        Pixels[i + 2] = (byte)((src.B * sa + Pixels[i + 2] * da * (1 - sa)) / oa);
        Pixels[i + 3] = (byte)(oa * 255);
    }
}
