using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal static partial class RasterStroker {
    private const int MaximumDashPieces = 100000;

    /// <summary>Clips invisible lengths before splitting dashes, retaining their phase and joins.</summary>
    internal static List<List<ChartPoint>> Dash(IReadOnlyList<ChartPoint> points, IReadOnlyList<double> pattern,
        double left, double top, double right, double bottom, double dashOffset = 0, double pixelsPerUnit = 1,
        double strokeWidth = 0, RasterLineCap cap = RasterLineCap.Butt) {
        if (!TryDashPattern(pattern, out var lengths, out double total))
            return new List<List<ChartPoint>> { new List<ChartPoint>(points) };
        double phase = double.IsNaN(dashOffset) || double.IsInfinity(dashOffset) ? 0 : dashOffset % total;
        if (phase < 0) phase += total;
        bool zeroLength = points.Count > 0;
        for (int i = 1; zeroLength && i < points.Count; i++) zeroLength = Distance(points[i - 1], points[i]) <= Epsilon;
        if (zeroLength) {
            // A zero-length on dash still has its native cap, including a zero on entry.
            int index = 0;
            while (index < lengths.Count - 1 && phase >= lengths[index]) {
                if (phase == 0 && (index & 1) == 0) break;
                phase -= lengths[index++];
            }
            return (index & 1) == 0 ? new List<List<ChartPoint>> { new List<ChartPoint> { points[0] } } : new List<List<ChartPoint>>();
        }
        double coarsePeriod = CoarsenedDashPeriod(points, total, pixelsPerUnit, strokeWidth, cap, left, top, right, bottom);

        var runs = new List<List<ChartPoint>>();
        List<ChartPoint>? run = null;
        int pieces = 0;
        for (int i = 1; i < points.Count; i++) {
            ChartPoint a = points[i - 1], b = points[i];
            double length = Distance(a, b);
            if (double.IsNaN(length) || double.IsInfinity(length)) { run = null; continue; }
            if (length <= Epsilon) continue;
            if (!ClipDashSegment(a, b, left, top, right, bottom, out double first, out double last)) {
                phase = (phase + length % total) % total; run = null; continue;
            }
            if (first > 0) run = null;
            phase = (phase + (length * first) % total) % total;
            double position = length * first, end = length * last;
            double endPhase = (phase + (end - position) % total) % total;
            var segmentLengths = lengths;
            double segmentPhase = phase;
            if (coarsePeriod > 0) CoarsenDashSegment(lengths, total, phase, endPhase, end - position,
                coarsePeriod, out segmentLengths, out segmentPhase);
            int index = 0;
            double offset = segmentPhase;
            while (index < segmentLengths.Count - 1 && offset > 0 && offset >= segmentLengths[index]) offset -= segmentLengths[index++];
            while (position < end) {
                if (++pieces > MaximumDashPieces) throw new InvalidOperationException("The visible stroke exceeds the raster dash-piece limit.");
                double remaining = segmentLengths[index] - offset;
                if (remaining <= 0) {
                    if (segmentLengths[index] == 0 && (index & 1) == 0) runs.Add(new List<ChartPoint> { DashPoint(a, b, position / length) });
                    index = (index + 1) % segmentLengths.Count; offset = 0; continue;
                }
                bool reachedBoundary = remaining <= end - position;
                double next = Math.Min(end, position + remaining);
                if (next <= position) {
                    // An entry below this coordinate's ULP has the same geometry as a zero entry.
                    // An on entry still has a cap; an off entry must not break the adjacent on run.
                    if (offset == 0 && (index & 1) == 0) runs.Add(new List<ChartPoint> { DashPoint(a, b, position / length) });
                    index = (index + 1) % segmentLengths.Count; offset = 0; continue;
                }
                if ((index & 1) == 0) {
                    ChartPoint start = DashPoint(a, b, position / length), stop = DashPoint(a, b, next / length);
                    if (run == null || !Same(run[run.Count - 1], start)) { run = new List<ChartPoint> { start }; runs.Add(run); }
                    run.Add(stop);
                } else run = null;
                if (coarsePeriod == 0) phase = (phase + (next - position) % total) % total;
                if (reachedBoundary) { index = (index + 1) % segmentLengths.Count; offset = 0; }
                else offset += next - position;
                position = next;
            }
            if (coarsePeriod > 0) phase = endPhase;
            phase = (phase + (length * (1 - last)) % total) % total;
            if (last < 1) run = null;
        }
        for (int i = runs.Count - 1; i > 0; i--) {
            if (!Same(runs[i - 1][runs[i - 1].Count - 1], runs[i][0])) continue;
            runs[i - 1].AddRange(runs[i].GetRange(1, runs[i].Count - 1)); runs.RemoveAt(i);
        }
        if (runs.Count > 1 && points.Count > 2 && Same(points[0], points[points.Count - 1]) && Same(runs[runs.Count - 1][runs[runs.Count - 1].Count - 1], runs[0][0])) {
            runs[runs.Count - 1].AddRange(runs[0].GetRange(1, runs[0].Count - 1)); runs.RemoveAt(0);
        }
        return runs;
    }

    private static bool TryDashPattern(IReadOnlyList<double> pattern, out List<double> lengths, out double total) {
        lengths = new List<double>(pattern.Count * 2); total = 0;
        foreach (double value in pattern) {
            if (value < 0 || double.IsNaN(value) || double.IsInfinity(value)) return false;
            lengths.Add(value); total += value;
        }
        if ((lengths.Count & 1) != 0) { lengths.AddRange(pattern); total *= 2; }
        if (!(total > 0) || double.IsInfinity(total)) return false;
        // Repeated entries must not hide a microscopic effective cycle behind a large
        // declared period. Only an even entry count preserves the on/off states and caps.
        if (lengths.Count > 2) {
            var prefix = new int[lengths.Count];
            for (int i = 1; i < lengths.Count; i++) {
                int match = prefix[i - 1];
                while (match > 0 && lengths[i] != lengths[match]) match = prefix[match - 1];
                if (lengths[i] == lengths[match]) match++;
                prefix[i] = match;
            }
            int period = lengths.Count - prefix[lengths.Count - 1];
            if (lengths.Count % period == 0) {
                if ((period & 1) != 0) period *= 2;
                if (period < lengths.Count) {
                    lengths.RemoveRange(period, lengths.Count - period);
                    total = 0;
                    foreach (double value in lengths) total += value;
                }
            }
        }
        return true;
    }

    private static ChartPoint DashPoint(ChartPoint a, ChartPoint b, double fraction) => new ChartPoint(a.X + (b.X - a.X) * fraction, a.Y + (b.Y - a.Y) * fraction);

    private static bool ClipDashSegment(ChartPoint a, ChartPoint b, double left, double top, double right, double bottom, out double first, out double last) {
        first = 0; last = 1;
        double dx = b.X - a.X, dy = b.Y - a.Y;
        return ClipDashBoundary(-dx, a.X - left, ref first, ref last) && ClipDashBoundary(dx, right - a.X, ref first, ref last)
            && ClipDashBoundary(-dy, a.Y - top, ref first, ref last) && ClipDashBoundary(dy, bottom - a.Y, ref first, ref last);
    }

    private static bool ClipDashBoundary(double direction, double distance, ref double first, ref double last) {
        if (direction == 0) return distance >= 0;
        double fraction = distance / direction;
        if (direction < 0) { if (fraction > last) return false; first = Math.Max(first, fraction); }
        else { if (fraction < first) return false; last = Math.Min(last, fraction); }
        return true;
    }
}
