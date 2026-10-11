using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal static partial class RasterStroker {
    /// <summary>
    /// Resolves overlapping microscopic caps as their segment envelope. The gap must be below
    /// one alpha level in device pixels and much smaller than the cap, keeping this geometric
    /// approximation below the raster's coverage precision without allocating every cap.
    /// </summary>
    private static bool TryAppendDenseCapOutline(IReadOnlyList<ChartPoint> source, IReadOnlyList<double> pattern,
        double width, RasterLineCap cap, RasterLineJoin join, double miter, double offset, double pixelsPerUnit,
        double left, double top, double right, double bottom, List<List<ChartPoint>> output) {
        if (cap == RasterLineCap.Butt || !TryDashPattern(pattern, out var lengths, out double total)) return false;
        if (CoarsenedDashPeriod(source, total, pixelsPerUnit, width, cap, left, top, right, bottom) == 0) return false;
        double gap = 0;
        for (int i = 1; i < lengths.Count; i += 2) gap = Math.Max(gap, lengths[i]);
        if (gap == 0) { AppendOutline(source, width, cap, join, miter, pixelsPerUnit, output); return true; }
        if (gap > width / 16 || gap * pixelsPerUnit > 1d / 255) return false;
        var points = Distinct(source);
        if (points.Count <= 1) return false; // The shared dash splitter owns zero-length cap phase.
        double phase = double.IsNaN(offset) || double.IsInfinity(offset) ? 0 : offset % total;
        if (phase < 0) phase += total;
        bool seam = Same(points[0], points[points.Count - 1]) && DashContinuesAt(lengths, phase);
        for (int i = 1; i < points.Count; i++) {
            var a = points[i - 1]; var b = points[i];
            double length = Distance(a, b);
            if (!(length > Epsilon) || double.IsInfinity(length)) continue;
            if (ClipDashSegment(a, b, left, top, right, bottom, out double first, out double last)) {
                double start = length * first, end = length * last;
                DashEntry(lengths, (phase + start % total) % total, out int firstIndex, out double firstOffset);
                DashEntry(lengths, (phase + end % total) % total, out int lastIndex, out double lastOffset);
                if ((firstIndex & 1) != 0) start += lengths[firstIndex] - firstOffset;
                if ((lastIndex & 1) != 0) end -= lastOffset;
                if (end >= start) AppendOutline(new[] { DashPoint(a, b, start / length), DashPoint(a, b, end / length) },
                    width, cap, join, miter, pixelsPerUnit, output);
            }
            phase = (phase + length % total) % total;
            bool closes = i == points.Count - 1;
            if ((closes && !seam) || !DashContinuesAt(lengths, phase) || b.X < left || b.X > right || b.Y < top || b.Y > bottom) continue;
            var next = closes ? points[1] : points[i + 1];
            double nextLength = Distance(b, next);
            if (!(nextLength > Epsilon) || double.IsInfinity(nextLength)) continue;
            AddJoin(output, b, new ChartPoint((b.X - a.X) / length, (b.Y - a.Y) / length),
                new ChartPoint((next.X - b.X) / nextLength, (next.Y - b.Y) / nextLength), width / 2, join, miter, pixelsPerUnit);
        }
        return true;
    }

    private static void DashEntry(IReadOnlyList<double> lengths, double phase, out int index, out double offset) {
        index = 0; offset = phase;
        while (index < lengths.Count - 1 && offset > 0 && offset >= lengths[index]) offset -= lengths[index++];
    }

    /// <summary>True only when the original dash paints continuously on both sides of a vertex.</summary>
    private static bool DashContinuesAt(IReadOnlyList<double> lengths, double phase) {
        DashEntry(lengths, phase, out int index, out double offset);
        if (offset > 0) return (index & 1) == 0;
        int after = index, before = (index + lengths.Count - 1) % lengths.Count;
        while (lengths[after] == 0) after = (after + 1) % lengths.Count;
        while (lengths[before] == 0) before = (before + lengths.Count - 1) % lengths.Count;
        return (after & 1) == 0 && (before & 1) == 0;
    }
}
