using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal sealed partial class RgbaCanvas {
    private bool _hasClip;
    private int _clipLeft;
    private int _clipTop;
    private int _clipRight;
    private int _clipBottom;
    private List<PolygonClip>? _polygonClips;

    /// <summary>
    /// Restricts subsequent paint operations to pixels whose centers fall inside the supplied logical bounds.
    /// </summary>
    internal void SetClipBounds(double x, double y, double width, double height) {
        if (double.IsNaN(x) || double.IsInfinity(x) || double.IsNaN(y) || double.IsInfinity(y) ||
            double.IsNaN(width) || double.IsInfinity(width) || double.IsNaN(height) || double.IsInfinity(height) ||
            width < 0 || height < 0) {
            throw new ArgumentOutOfRangeException(nameof(width), "Clip bounds must be finite and non-negative.");
        }

        _clipLeft = Math.Max(0, (int)Math.Ceiling(x * _scale - 0.5));
        _clipTop = Math.Max(0, (int)Math.Ceiling(y * _scale - 0.5));
        _clipRight = Math.Min(_pixelWidth, (int)Math.Ceiling((x + width) * _scale - 0.5));
        _clipBottom = Math.Min(_pixelHeight, (int)Math.Ceiling((y + height) * _scale - 0.5));
        _hasClip = true;
    }

    internal IDisposable PushPolygonClip(IReadOnlyList<ChartPoint> points) {
        if (points == null) throw new ArgumentNullException(nameof(points));
        if (points.Count < 3) throw new ArgumentException("Polygon clips require at least three points.", nameof(points));
        var clip = new PolygonClip(points, _scale);
        _polygonClips ??= new List<PolygonClip>();
        _polygonClips.Add(clip);
        return new PolygonClipScope(this, _polygonClips.Count);
    }

    private bool IsInsideClip(int x, int y) {
        if (_hasClip && (x < _clipLeft || x >= _clipRight || y < _clipTop || y >= _clipBottom)) return false;
        if (_polygonClips == null) return true;
        foreach (var clip in _polygonClips) if (!clip.Contains(x + 0.5, y + 0.5)) return false;
        return true;
    }

    private void PopPolygonClip(int depth) {
        if (_polygonClips == null || depth != _polygonClips.Count) throw new InvalidOperationException("Polygon clip scopes must be disposed in reverse order.");
        _polygonClips.RemoveAt(depth - 1);
    }

    public void FillRectClippedToRoundedRect(double x, double y, double width, double height, double clipX, double clipY, double clipWidth, double clipHeight, double clipRadius, ChartColor color) {
        FillRectClippedToRoundedRectPixels(x * _scale, y * _scale, width * _scale, height * _scale, clipX * _scale, clipY * _scale, clipWidth * _scale, clipHeight * _scale, clipRadius * _scale, color);
    }

    private void FillRectClippedToRoundedRectPixels(double x, double y, double width, double height, double clipX, double clipY, double clipWidth, double clipHeight, double clipRadius, ChartColor color) {
        if (width <= 0 || height <= 0 || clipWidth <= 0 || clipHeight <= 0 || color.A == 0) return;
        var feather = 1.0;
        var left = Math.Max(x, clipX);
        var top = Math.Max(y, clipY);
        var right = Math.Min(x + width, clipX + clipWidth);
        var bottom = Math.Min(y + height, clipY + clipHeight);
        var x1 = Math.Max(0, (int)Math.Floor(left - feather));
        var y1 = Math.Max(0, (int)Math.Floor(top - feather));
        var x2 = Math.Min(_pixelWidth, (int)Math.Ceiling(right + feather));
        var y2 = Math.Min(_pixelHeight, (int)Math.Ceiling(bottom + feather));
        for (var yy = y1; yy < y2; yy++) for (var xx = x1; xx < x2; xx++) {
            var px = xx + 0.5;
            var py = yy + 0.5;
            if (px < x || py < y || px >= x + width || py >= y + height) continue;
            var distance = RoundedRectSignedDistance(px, py, clipX, clipY, clipWidth, clipHeight, clipRadius);
            if (distance <= 0) {
                BlendPixel(xx, yy, color);
            } else if (distance < feather) {
                BlendPixel(xx, yy, WithOpacity(color, feather - distance));
            }
        }
    }

    private sealed class PolygonClipScope : IDisposable {
        private RgbaCanvas? _canvas;
        private readonly int _depth;

        public PolygonClipScope(RgbaCanvas canvas, int depth) {
            _canvas = canvas;
            _depth = depth;
        }

        public void Dispose() {
            if (_canvas == null) return;
            _canvas.PopPolygonClip(_depth);
            _canvas = null;
        }
    }

    private readonly struct PolygonClip {
        private readonly ChartPoint[] _points;
        private readonly double _left;
        private readonly double _top;
        private readonly double _right;
        private readonly double _bottom;

        public PolygonClip(IReadOnlyList<ChartPoint> points, int scale) {
            _points = new ChartPoint[points.Count];
            _left = double.PositiveInfinity;
            _top = double.PositiveInfinity;
            _right = double.NegativeInfinity;
            _bottom = double.NegativeInfinity;
            for (var index = 0; index < points.Count; index++) {
                var point = points[index];
                if (double.IsNaN(point.X) || double.IsInfinity(point.X) || double.IsNaN(point.Y) || double.IsInfinity(point.Y)) throw new ArgumentOutOfRangeException(nameof(points), "Polygon clip points must be finite.");
                var scaled = new ChartPoint(point.X * scale, point.Y * scale);
                _points[index] = scaled;
                _left = Math.Min(_left, scaled.X);
                _top = Math.Min(_top, scaled.Y);
                _right = Math.Max(_right, scaled.X);
                _bottom = Math.Max(_bottom, scaled.Y);
            }
        }

        public bool Contains(double x, double y) {
            if (x < _left || x > _right || y < _top || y > _bottom) return false;
            var inside = false;
            for (int current = 0, previous = _points.Length - 1; current < _points.Length; previous = current++) {
                var first = _points[current];
                var second = _points[previous];
                if ((first.Y > y) == (second.Y > y)) continue;
                var crossingX = (second.X - first.X) * (y - first.Y) / (second.Y - first.Y) + first.X;
                if (x < crossingX) inside = !inside;
            }
            return inside;
        }
    }
}
