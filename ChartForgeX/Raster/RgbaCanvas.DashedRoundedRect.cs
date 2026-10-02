using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal sealed partial class RgbaCanvas {
    /// <summary>
    /// Strokes a rounded rectangle with a dash pattern, inside its bounds as <see cref="StrokeRoundedRect"/> does. Each
    /// pixel is covered once, so a translucent colour stays even at the corners. The dashes start after the top-left
    /// corner of the stroke's centre line and run clockwise, where an SVG <c>rect</c> with the same centre line starts
    /// its <c>stroke-dasharray</c>.
    /// </summary>
    /// <param name="x">Left edge of the outer bounds.</param>
    /// <param name="y">Top edge of the outer bounds.</param>
    /// <param name="width">Width of the outer bounds.</param>
    /// <param name="height">Height of the outer bounds.</param>
    /// <param name="radius">Outer corner radius; the centre line uses it less half the thickness.</param>
    /// <param name="color">Stroke colour.</param>
    /// <param name="thickness">Stroke width.</param>
    /// <param name="dash">Dash length along the centre line.</param>
    /// <param name="gap">Gap length along the centre line.</param>
    public void StrokeRoundedRectDashed(double x, double y, double width, double height, double radius, ChartColor color, double thickness, double dash, double gap) {
        if (dash <= 0 || gap <= 0) {
            StrokeRoundedRect(x, y, width, height, radius, color, thickness);
            return;
        }

        var strokeWidth = Math.Max(1, thickness * _scale);
        var strokeRadius = strokeWidth / 2.0;
        var left = x * _scale + strokeRadius;
        var top = y * _scale + strokeRadius;
        var pathWidth = width * _scale - strokeWidth;
        var pathHeight = height * _scale - strokeWidth;
        if (pathWidth <= 0 || pathHeight <= 0 || color.A == 0) return;
        var pathRadius = Math.Max(0, Math.Min(radius * _scale - strokeRadius, Math.Min(pathWidth, pathHeight) / 2));
        var path = new DashPath(left, top, pathWidth, pathHeight, pathRadius);
        var dashLength = dash * _scale;
        var period = dashLength + gap * _scale;
        const double feather = 1.0;
        var x1 = Math.Max(0, (int)Math.Floor(left - strokeRadius - feather));
        var y1 = Math.Max(0, (int)Math.Floor(top - strokeRadius - feather));
        var x2 = Math.Min(_pixelWidth, (int)Math.Ceiling(left + pathWidth + strokeRadius + feather));
        var y2 = Math.Min(_pixelHeight, (int)Math.Ceiling(top + pathHeight + strokeRadius + feather));
        for (var yy = y1; yy < y2; yy++) {
            for (var xx = x1; xx < x2; xx++) {
                var px = xx + 0.5;
                var py = yy + 0.5;
                var distance = Math.Abs(RoundedRectSignedDistance(px, py, left, top, pathWidth, pathHeight, pathRadius));
                var across = Math.Min(1, strokeRadius + feather - distance);
                if (across <= 0) continue;
                var position = path.Position(px, py) % period;
                // A pixel just before a dash starts belongs to that dash's leading edge, so both ends fade alike.
                if (position > period - 0.5) position -= period;
                var along = Math.Min(1, Math.Min(position + 0.5, dashLength - position + 0.5));
                if (along <= 0) continue;
                BlendPixel(xx, yy, WithOpacity(color, across * along));
            }
        }
    }

    /// <summary>The centre line of a rounded rectangle, measured clockwise from the end of the top-left corner.</summary>
    private readonly struct DashPath {
        private readonly double _left;
        private readonly double _top;
        private readonly double _right;
        private readonly double _bottom;
        private readonly double _radius;
        private readonly double _horizontal;
        private readonly double _vertical;
        private readonly double _arc;

        public DashPath(double left, double top, double width, double height, double radius) {
            _left = left;
            _top = top;
            _right = left + width;
            _bottom = top + height;
            _radius = radius;
            _horizontal = width - radius * 2;
            _vertical = height - radius * 2;
            _arc = Math.PI / 2 * radius;
        }

        /// <summary>Returns the distance along the centre line to the point on it nearest to a pixel.</summary>
        public double Position(double px, double py) {
            var innerLeft = _left + _radius;
            var innerRight = _right - _radius;
            var innerTop = _top + _radius;
            var innerBottom = _bottom - _radius;
            var cx = Math.Max(innerLeft, Math.Min(innerRight, px));
            var cy = Math.Max(innerTop, Math.Min(innerBottom, py));
            var dx = px - cx;
            var dy = py - cy;
            if (dx == 0 && dy == 0) {
                // Deep inside: take the nearest side.
                var toTop = py - _top;
                var toRight = _right - px;
                var toBottom = _bottom - py;
                var toLeft = px - _left;
                var nearest = Math.Min(Math.Min(toTop, toBottom), Math.Min(toLeft, toRight));
                if (nearest == toTop) dy = -1;
                else if (nearest == toRight) dx = 1;
                else if (nearest == toBottom) dy = 1;
                else dx = -1;
            }

            if (dx == 0) return dy < 0 ? cx - innerLeft : _horizontal + _arc + _vertical + _arc + (innerRight - cx);
            if (dy == 0) return dx > 0 ? _horizontal + _arc + (cy - innerTop) : _horizontal * 2 + _arc * 3 + _vertical + (innerBottom - cy);
            var angle = Math.Atan2(dy, dx);
            if (dx > 0 && dy < 0) return _horizontal + (angle + Math.PI / 2) * _radius;
            if (dx > 0) return _horizontal + _arc + _vertical + angle * _radius;
            if (dy > 0) return _horizontal * 2 + _arc * 2 + _vertical + (angle - Math.PI / 2) * _radius;
            return _horizontal * 2 + _arc * 3 + _vertical * 2 + (angle + Math.PI) * _radius;
        }
    }
}
