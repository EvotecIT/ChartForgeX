using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>Fits each painted extent to its own coordinate while preserving automatic axis bounds.</summary>
internal sealed class VisualAxisPaintBounds {
    private readonly List<ChartPoint> _extents = new List<ChartPoint>();

    internal void Include(double value, double radius) {
        if (ChartMath.IsFinite(value) && ChartMath.IsFinite(radius) && radius > 0)
            _extents.Add(new ChartPoint(value, radius));
    }

    internal bool HasFullWidthConflict(double pixels) {
        double? center = null;
        foreach (var extent in _extents) {
            if (extent.Y * 2 != pixels) continue;
            if (center.HasValue && center.Value != extent.X) return true;
            center = extent.X;
        }
        return false;
    }

    internal bool TryExpand(double minimum, double maximum, double pixels, bool fixedMinimum, bool fixedMaximum,
        out double expandedMinimum, out double expandedMaximum) {
        expandedMinimum = minimum; expandedMaximum = maximum;
        if (_extents.Count == 0 || !(pixels > 0) || !(maximum > minimum) || fixedMinimum && fixedMaximum) return false;
        var fullWidthConflict = !fixedMinimum && !fixedMaximum && HasFullWidthConflict(pixels);
        var low = maximum - minimum;
        if (TryPosition(low, out _)) return false;
        var high = low;
        var found = false;
        for (var pass = 0; pass < 64; pass++) {
            high *= 2;
            if (!ChartMath.IsFinite(high)) return false;
            if (TryPosition(high, out _)) { found = true; break; }
        }
        if (!found) return false;
        // Feasibility is monotone in span. A bounded search avoids a quadratic
        // comparison of every pair of differently sized marks.
        for (var pass = 0; pass < 56; pass++) {
            var middle = low + (high - low) / 2;
            if (TryPosition(middle, out _)) high = middle; else low = middle;
        }
        if (!TryPosition(high, out var position)) return false;
        expandedMinimum = fixedMinimum ? minimum : Math.Min(minimum, position);
        expandedMaximum = fixedMaximum ? maximum : Math.Max(maximum, position + high);
        return expandedMinimum < minimum || expandedMaximum > maximum;

        bool TryPosition(double span, out double position) {
            var lower = fixedMinimum ? minimum : maximum - span;
            var upper = fixedMaximum ? maximum - span : minimum;
            foreach (var extent in _extents) {
                // An uncontainable mark clips independently; it must not cancel other marks' gutters.
                if (extent.Y * 2 > pixels || fullWidthConflict && extent.Y * 2 == pixels) continue;
                var fraction = extent.Y / pixels;
                // An authored bound is a clipping boundary, so only the other side expands.
                if (!fixedMaximum) lower = Math.Max(lower, extent.X - (1 - fraction) * span);
                if (!fixedMinimum) upper = Math.Min(upper, extent.X - fraction * span);
            }
            position = upper;
            return lower <= upper;
        }
    }
}
