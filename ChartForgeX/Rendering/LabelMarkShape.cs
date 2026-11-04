using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>Filled and stroked contours for precise label-to-mark collision tests.</summary>
internal sealed class LabelMarkShape {
    private readonly IReadOnlyList<List<ChartPoint>> _contours;
    private readonly bool _filled;
    private readonly double _stroke;
    private readonly ChartRect? _clip;
    internal LabelMarkShape(IReadOnlyList<List<ChartPoint>> contours, bool filled, double stroke, ChartRect? clip = null) {
        _contours = contours; _filled = filled; _stroke = Math.Max(0, stroke); _clip = clip;
        var left = double.PositiveInfinity; var top = double.PositiveInfinity;
        var right = double.NegativeInfinity; var bottom = double.NegativeInfinity;
        foreach (var contour in contours) foreach (var point in contour) {
            left = Math.Min(left, point.X); right = Math.Max(right, point.X); top = Math.Min(top, point.Y); bottom = Math.Max(bottom, point.Y);
        }
        var bounds = double.IsInfinity(left) ? default : new ChartRect(left - _stroke / 2, top - _stroke / 2, right - left + _stroke, bottom - top + _stroke);
        Bounds = clip.HasValue ? Intersection(bounds, clip.Value) : bounds;
    }
    internal ChartRect Bounds { get; }
    internal bool IsFilled => _filled;
    internal LabelMarkShape WithClip(ChartRect clip) => new(_contours, _filled, _stroke, clip);
    internal ChartPoint LeaderExit(ChartPoint start, ChartPoint end) {
        if (!_filled || !Inside(start) || Inside(end)) return start;
        foreach (var contour in _contours) for (var i = 0; i < contour.Count; i++)
            if (LabelLeaderGeometry.Intersects(start, start, contour[i], contour[(i + 1) % contour.Count], .1)) return start;
        var ratio = 1d;
        foreach (var contour in _contours) {
            for (var i = 0; i < contour.Count; i++) {
                var next = (i + 1) % contour.Count;
                var hit = LabelLeaderGeometry.IntersectionRatio(start, end, contour[i], contour[next]);
                if (hit.HasValue) ratio = Math.Min(ratio, hit.Value);
            }
        }
        return new ChartPoint(start.X + (end.X - start.X) * ratio, start.Y + (end.Y - start.Y) * ratio);
    }
    internal bool IntersectsLeader(ChartPoint start, ChartPoint end) {
        if (_clip.HasValue) {
            var x1 = start.X; var y1 = start.Y; var x2 = end.X; var y2 = end.Y; var clip = _clip.Value;
            if (!ChartPatternLineGeometry.ClipLineToRect(ref x1, ref y1, ref x2, ref y2, clip.Left, clip.Top, clip.Right, clip.Bottom)) return false;
            start = new ChartPoint(x1, y1); end = new ChartPoint(x2, y2);
        }
        if (_filled && (Inside(start) || Inside(end))) return true;
        foreach (var contour in _contours) {
            for (var i = 1; i < contour.Count; i++)
                if (LabelLeaderGeometry.Intersects(start, end, contour[i - 1], contour[i], .5 + _stroke / 2)) return true;
            if (_filled && contour.Count > 2 && LabelLeaderGeometry.Intersects(start, end, contour[contour.Count - 1], contour[0], .5 + _stroke / 2)) return true;
        }
        return false;
    }
    internal bool Contains(ChartRect box) {
        if (!_filled || !LabelPlacementService.Contains(Bounds, box) || !Inside(new ChartPoint(box.Left, box.Top)) || !Inside(new ChartPoint(box.Right, box.Top))
            || !Inside(new ChartPoint(box.Left, box.Bottom)) || !Inside(new ChartPoint(box.Right, box.Bottom))) return false;
        // A concave boundary or a hole can cross the text even when all four corners fit.
        var interior = new ChartRect(box.X + 0.0001, box.Y + 0.0001, Math.Max(0, box.Width - 0.0002), Math.Max(0, box.Height - 0.0002));
        foreach (var contour in _contours) {
            for (var i = 1; i < contour.Count; i++) if (SegmentHits(contour[i - 1], contour[i], interior)) return false;
            if (contour.Count > 2 && SegmentHits(contour[contour.Count - 1], contour[0], interior)) return false;
        }
        return true;
    }
    internal bool Intersects(ChartRect box) {
        if (_clip.HasValue) { box = Intersection(box, _clip.Value); if (box.Width <= 0 || box.Height <= 0) return false; }
        var strokeBox = new ChartRect(box.X - _stroke / 2, box.Y - _stroke / 2, box.Width + _stroke, box.Height + _stroke);
        foreach (var contour in _contours) {
            for (var i = 1; i < contour.Count; i++) if (SegmentHits(contour[i - 1], contour[i], strokeBox)) return true;
            if (_filled && contour.Count > 2 && SegmentHits(contour[contour.Count - 1], contour[0], strokeBox)) return true;
        }
        return _filled && (Inside(new ChartPoint(box.Left, box.Top)) || Inside(new ChartPoint(box.Right, box.Top))
            || Inside(new ChartPoint(box.Left, box.Bottom)) || Inside(new ChartPoint(box.Right, box.Bottom)));
    }

    /// <summary>Translations at which this contour's ink meets a fixed rectangle; disconnected paths remain separate.</summary>
    internal IEnumerable<CartesianXInterval> HorizontalTranslations(ChartRect box, string reason) {
        var top = box.Top - _stroke / 2; var bottom = box.Bottom + _stroke / 2;
        foreach (var contour in _contours) {
            var filledLeft = double.PositiveInfinity; var filledRight = double.NegativeInfinity;
            var count = contour.Count + (_filled && contour.Count > 2 ? 1 : 0);
            for (var i = 1; i < count; i++) {
                var a = contour[i - 1]; var b = contour[i % contour.Count];
                var t0 = 0d; var t1 = 1d; var dx = b.X - a.X; var dy = b.Y - a.Y;
                if (!Clip(-dy, a.Y - top, ref t0, ref t1) || !Clip(dy, bottom - a.Y, ref t0, ref t1)) continue;
                if (_clip.HasValue && (!Clip(-dx, a.X - _clip.Value.Left + _stroke / 2, ref t0, ref t1)
                    || !Clip(dx, _clip.Value.Right + _stroke / 2 - a.X, ref t0, ref t1)
                    || !Clip(-dy, a.Y - _clip.Value.Top + _stroke / 2, ref t0, ref t1)
                    || !Clip(dy, _clip.Value.Bottom + _stroke / 2 - a.Y, ref t0, ref t1))) continue;
                var left = Math.Min(a.X + dx * t0, a.X + dx * t1); var right = Math.Max(a.X + dx * t0, a.X + dx * t1);
                if (_filled) { filledLeft = Math.Min(filledLeft, left); filledRight = Math.Max(filledRight, right); }
                else yield return new CartesianXInterval(box.Left - _stroke / 2 - right, box.Right + _stroke / 2 - left, reason);
            }
            if (_filled && filledLeft <= filledRight)
                yield return new CartesianXInterval(box.Left - _stroke / 2 - filledRight, box.Right + _stroke / 2 - filledLeft, reason);
        }
    }
    private static ChartRect Intersection(ChartRect a, ChartRect b) => new(Math.Max(a.Left, b.Left), Math.Max(a.Top, b.Top),
        Math.Max(0, Math.Min(a.Right, b.Right) - Math.Max(a.Left, b.Left)), Math.Max(0, Math.Min(a.Bottom, b.Bottom) - Math.Max(a.Top, b.Top)));
    private bool Inside(ChartPoint point) {
        var inside = false;
        foreach (var contour in _contours) {
            if (contour.Count < 3) continue;
            var previous = contour[contour.Count - 1];
            foreach (var next in contour) {
                if ((next.Y > point.Y) != (previous.Y > point.Y)
                    && point.X < (previous.X - next.X) * (point.Y - next.Y) / (previous.Y - next.Y) + next.X) inside = !inside;
                previous = next;
            }
        }
        return inside;
    }
    private static bool SegmentHits(ChartPoint a, ChartPoint b, ChartRect box) {
        if (Math.Max(a.X, b.X) < box.Left || Math.Min(a.X, b.X) > box.Right || Math.Max(a.Y, b.Y) < box.Top || Math.Min(a.Y, b.Y) > box.Bottom) return false;
        var t0 = 0d; var t1 = 1d;
        var dx = b.X - a.X; var dy = b.Y - a.Y;
        return Clip(-dx, a.X - box.Left, ref t0, ref t1) && Clip(dx, box.Right - a.X, ref t0, ref t1)
            && Clip(-dy, a.Y - box.Top, ref t0, ref t1) && Clip(dy, box.Bottom - a.Y, ref t0, ref t1);
    }
    private static bool Clip(double p, double q, ref double t0, ref double t1) {
        if (Math.Abs(p) < 0.00000001) return q >= 0;
        var ratio = q / p;
        if (p < 0) { if (ratio > t1) return false; t0 = Math.Max(t0, ratio); }
        else { if (ratio < t0) return false; t1 = Math.Min(t1, ratio); }
        return true;
    }
}
