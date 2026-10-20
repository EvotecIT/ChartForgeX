using System;
using System.Collections.Generic;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>Fits each painted extent to its own coordinate while preserving automatic axis bounds.</summary>
internal sealed class VisualAxisPaintBounds {
    // At a shared radius, only the extreme coordinates can constrain either side.
    // Ordinary line/scatter series therefore retain two coordinates per marker size.
    private readonly Dictionary<double, CoordinateBounds> _extents = new Dictionary<double, CoordinateBounds>();

    internal void Include(double value, double radius) {
        if (!ChartMath.IsFinite(value) || !ChartMath.IsFinite(radius) || radius <= 0) return;
        if (_extents.TryGetValue(radius, out var bounds))
            _extents[radius] = new CoordinateBounds(Math.Min(bounds.Minimum, value), Math.Max(bounds.Maximum, value));
        else _extents.Add(radius, new CoordinateBounds(value, value));
    }

    internal bool HasFullWidthConflict(double pixels) {
        foreach (var extent in _extents) {
            if (extent.Key * 2 == pixels && extent.Value.Minimum != extent.Value.Maximum) return true;
        }
        return false;
    }

    internal bool TryExpand(double minimum, double maximum, double pixels, bool fixedMinimum, bool fixedMaximum,
        out double expandedMinimum, out double expandedMaximum) {
        expandedMinimum = minimum; expandedMaximum = maximum;
        if (_extents.Count == 0 || !(pixels > 0) || !(maximum > minimum) || fixedMinimum && fixedMaximum) return false;
        var fullWidthConflict = !fixedMinimum && !fixedMaximum && HasFullWidthConflict(pixels);
        var low = maximum - minimum;
        // Verify the existing range in the same pixel coordinates as the paint. The equivalent
        // source-space feasibility equations can disagree by one rounding step at a fitted edge.
        if (ContainsPaint() || TryPosition(low, out _)) return false;
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

        bool ContainsPaint() {
            if (!ChartMath.IsFinite(minimum) || !ChartMath.IsFinite(maximum)) return false;
            foreach (var extent in _extents) {
                if (extent.Key * 2 > pixels || fullWidthConflict && extent.Key * 2 == pixels) continue;
                var first = ChartMath.Normalize(extent.Value.Minimum, minimum, maximum) * pixels;
                var last = ChartMath.Normalize(extent.Value.Maximum, minimum, maximum) * pixels;
                if (!ChartMath.IsFinite(first) || !ChartMath.IsFinite(last)
                    || !fixedMinimum && first - extent.Key < 0 || !fixedMaximum && last + extent.Key > pixels) return false;
            }
            return true;
        }

        bool TryPosition(double span, out double position) {
            var lower = fixedMinimum ? minimum : maximum - span;
            var upper = fixedMaximum ? maximum - span : minimum;
            foreach (var extent in _extents) {
                // An uncontainable mark clips independently; it must not cancel other marks' gutters.
                if (extent.Key * 2 > pixels || fullWidthConflict && extent.Key * 2 == pixels) continue;
                var fraction = extent.Key / pixels;
                // An authored bound is a clipping boundary, so only the other side expands.
                if (!fixedMaximum) lower = Math.Max(lower, extent.Value.Maximum - (1 - fraction) * span);
                if (!fixedMinimum) upper = Math.Min(upper, extent.Value.Minimum - fraction * span);
            }
            position = upper;
            return lower <= upper;
        }
    }

    private readonly struct CoordinateBounds {
        internal double Minimum { get; }
        internal double Maximum { get; }
        internal CoordinateBounds(double minimum, double maximum) { Minimum = minimum; Maximum = maximum; }
    }
}
