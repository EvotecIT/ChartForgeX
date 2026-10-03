using System;
using ChartForgeX.Typography;

namespace ChartForgeX.Raster;

/// <summary>Samples COLR fills in the paint's own design grid, including arbitrary color-line intervals.</summary>
internal sealed class ColorPaintBrush {
    private readonly ColorGlyphPaint _paint;
    private readonly LinearRgba[] _colors;
    internal ColorPaintBrush(ColorGlyphPaint paint, ColorFontData font, Primitives.ChartColor foreground, LinearRgba[]? colors = null) {
        _paint = paint; _colors = colors ?? new LinearRgba[paint.Stops.Length];
        if (colors == null) for (var i = 0; i < _colors.Length; i++) _colors[i] = LinearRgba.From(font.Color(paint.Stops[i].PaletteIndex, foreground), paint.Stops[i].Alpha);
    }
    internal LinearRgba[] Colors => _colors;
    internal LinearRgba Sample(double x, double y) {
        var g = _paint.Geometry; double amount;
        if (_paint.Kind == ColorPaintKind.Linear) {
            var vx = g[2] - g[0]; var vy = g[3] - g[1]; var wx = g[4] - g[0]; var wy = g[5] - g[1];
            var determinant = vx * wy - vy * wx; if (Math.Abs(determinant) < 0.000000001) return default;
            amount = ((x - g[0]) * wy - (y - g[1]) * wx) / determinant;
        } else if (_paint.Kind == ColorPaintKind.Radial) {
            var dx = g[3] - g[0]; var dy = g[4] - g[1]; var dr = g[5] - g[2];
            if (dx == 0 && dy == 0 && dr == 0 || g[2] == 0 && g[5] == 0) return default;
            var px = x - g[0]; var py = y - g[1]; var a = dx * dx + dy * dy - dr * dr;
            var b = -2 * (px * dx + py * dy + g[2] * dr); var c = px * px + py * py - g[2] * g[2];
            if (Math.Abs(a) < 0.000000001) {
                if (Math.Abs(b) < 0.000000001) return default; amount = -c / b;
                if (g[2] + amount * dr <= 0) return default;
            } else {
                var discriminant = b * b - 4 * a * c; if (discriminant < 0) return default;
                var root = Math.Sqrt(discriminant); var first = (-b + root) / (2 * a); var second = (-b - root) / (2 * a);
                amount = double.NegativeInfinity;
                if (g[2] + first * dr > 0) amount = first;
                if (g[2] + second * dr > 0) amount = Math.Max(amount, second);
                if (double.IsNegativeInfinity(amount)) return default;
            }
        } else {
            var angle = Math.Atan2(y - g[1], x - g[0]); if (angle < 0) angle += Math.PI * 2;
            if (g[2] == g[3]) return _paint.Extend == 0 && _colors.Length != 0 ? angle < g[2] ? _colors[0] : _colors[_colors.Length - 1] : default;
            amount = (angle - g[2]) / (g[3] - g[2]);
        }
        return Ramp(amount);
    }
    private LinearRgba Ramp(double amount) {
        var stops = _paint.Stops; if (stops.Length == 0 || double.IsNaN(amount)) return default;
        var first = stops[0].Offset; var last = stops[stops.Length - 1].Offset; var span = last - first;
        if (span == 0) return _paint.Extend == 0 ? amount < first ? _colors[0] : _colors[_colors.Length - 1] : default;
        if (_paint.Extend != 0) {
            var offset = (amount - first) / span;
            if (double.IsInfinity(offset)) return default;
            if (_paint.Extend == 1) offset -= Math.Floor(offset);
            else { offset -= Math.Floor(offset / 2) * 2; if (offset > 1) offset = 2 - offset; }
            amount = first + offset * span;
        }
        if (amount < first) return _colors[0];
        var low = 0; var high = stops.Length - 1;
        while (low <= high) { var middle = (low + high) / 2; if (stops[middle].Offset <= amount) low = middle + 1; else high = middle - 1; }
        if (low >= stops.Length) return _colors[_colors.Length - 1];
        var previous = Math.Max(0, high); var length = stops[low].Offset - stops[previous].Offset;
        return length <= 0 ? _colors[low] : LinearRgba.Mix(_colors[previous], _colors[low], (amount - stops[previous].Offset) / length);
    }
}
