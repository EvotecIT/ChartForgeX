using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal static partial class RasterStroker {
    private const int MaximumDashPieces = 100000;

    /// <summary>Clips invisible lengths before splitting dashes, retaining their phase and joins.</summary>
    internal static List<List<ChartPoint>> Dash(IReadOnlyList<ChartPoint> points, IReadOnlyList<double> pattern,
        double left, double top, double right, double bottom, double dashOffset = 0) {
        var lengths = new List<double>(pattern.Count * 2);
        double total = 0;
        foreach (double value in pattern) {
            if (value < 0 || double.IsNaN(value) || double.IsInfinity(value)) return new List<List<ChartPoint>> { new List<ChartPoint>(points) };
            lengths.Add(value); total += value;
        }
        if ((lengths.Count & 1) != 0) { lengths.AddRange(pattern); total *= 2; }
        if (!(total > 0) || double.IsInfinity(total) || points.Count <= 1) return new List<List<ChartPoint>> { new List<ChartPoint>(points) };

        var runs = new List<List<ChartPoint>>();
        List<ChartPoint>? run = null;
        double phase = double.IsNaN(dashOffset) || double.IsInfinity(dashOffset) ? 0 : dashOffset % total;
        if (phase < 0) phase += total;
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
            int index = 0;
            double offset = phase;
            while (index < lengths.Count - 1 && offset > 0 && offset >= lengths[index]) offset -= lengths[index++];
            double position = length * first, end = length * last;
            while (position < end) {
                if (++pieces > MaximumDashPieces) throw new InvalidOperationException("The visible stroke exceeds the raster dash-piece limit.");
                double remaining = lengths[index] - offset;
                if (remaining <= 0) {
                    if (lengths[index] == 0 && (index & 1) == 0) runs.Add(new List<ChartPoint> { DashPoint(a, b, position / length) });
                    index = (index + 1) % lengths.Count; offset = 0; continue;
                }
                bool reachedBoundary = remaining <= end - position;
                double next = Math.Min(end, position + remaining);
                if (next <= position) {
                    // A modulo/subtraction residue can be smaller than the coordinate's ULP.
                    // Finish that already consumed entry without losing the following dash.
                    if (offset > 0) { index = (index + 1) % lengths.Count; offset = 0; continue; }
                    throw new InvalidOperationException("The stroke dash pattern is too fine for its coordinates.");
                }
                if ((index & 1) == 0) {
                    ChartPoint start = DashPoint(a, b, position / length), stop = DashPoint(a, b, next / length);
                    if (run == null || !Same(run[run.Count - 1], start)) { run = new List<ChartPoint> { start }; runs.Add(run); }
                    run.Add(stop);
                } else run = null;
                phase = (phase + (next - position) % total) % total;
                if (reachedBoundary) { index = (index + 1) % lengths.Count; offset = 0; }
                else offset += next - position;
                position = next;
            }
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
