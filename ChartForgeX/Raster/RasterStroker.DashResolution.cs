using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal static partial class RasterStroker {
    // Below the stroke scan converter's eighth-pixel resolution, resolve a period's painted
    // fraction instead of allocating geometry for every microscopic dash. The original phase
    // still determines whether a run crosses a vertex or the seam of a closed path.
    private const double SubpixelDashPeriod = 0.125;

    private static double CoarsenedDashPeriod(IReadOnlyList<ChartPoint> points,
        double total, double pixelsPerUnit, double strokeWidth, RasterLineCap cap, double left, double top, double right, double bottom) {
        if (!(pixelsPerUnit > 0) || double.IsInfinity(pixelsPerUnit)) pixelsPerUnit = 1;
        double period = SubpixelDashPeriod / pixelsPerUnit;
        if (total >= period) return 0;
        // Cap overlap is part of the painted envelope, especially for a low-duty pattern.
        // Resolve thin round/square envelopes more finely than butt duty-cycle coverage.
        if (cap != RasterLineCap.Butt && strokeWidth > 0) period = Math.Max(total, Math.Min(period, strokeWidth / 4));
        if (cap == RasterLineCap.Round) period = Math.Max(total, Math.Min(period, 1 / (32 * pixelsPerUnit)));
        double visible = 0;
        int segments = 0;
        for (int i = 1; i < points.Count; i++) {
            if (!ClipDashSegment(points[i - 1], points[i], left, top, right, bottom, out double first, out double last)) continue;
            double length = Distance(points[i - 1], points[i]) * (last - first);
            if (!(length > Epsilon) || double.IsInfinity(length)) continue;
            visible += length; segments++;
            if (segments > MaximumDashPieces) throw new InvalidOperationException("The visible stroke exceeds the raster dash-piece limit.");
        }
        // Each segment also has at most two partial periods. Reserve those pieces before
        // adapting the period for wide/high-DPI exports, retaining the same geometry budget.
        int budget = MaximumDashPieces - segments * 6;
        if (budget <= 0) return period;
        period = Math.Max(period, visible * 2 / budget * 1.01);
        // Prefer an integral number of periods per device pixel, so straight dense strokes
        // average their duty cycle in every pixel rather than forming visible beat bands.
        double devicePeriod = Math.Min(1, period * pixelsPerUnit);
        return 1 / Math.Max(1, Math.Floor(1 / devicePeriod)) / pixelsPerUnit;
    }

    private static void CoarsenDashSegment(IReadOnlyList<double> lengths, double total, double phase,
        double endPhase, double length, double targetPeriod, out List<double> pattern, out double offset) {
        double painted = 0;
        for (int i = 0; i < lengths.Count; i += 2) painted += lengths[i];
        painted = Math.Min(total, painted);
        double duty = painted / total;
        double start = CoarsenedDashPhase(lengths, total, phase, painted);
        double end = CoarsenedDashPhase(lengths, total, endPhase, painted);
        double fraction = end - start;
        if (fraction < 0) fraction += 1;
        double cycles = Math.Max(1, Math.Floor(length / targetPeriod));
        double period = length / (cycles + fraction);
        offset = start * period;
        pattern = new List<double> { duty * period, (1 - duty) * period };
    }

    /// <summary>Maps a microscopic cycle into one on/off pair without changing its state at a vertex.</summary>
    private static double CoarsenedDashPhase(IReadOnlyList<double> lengths, double total, double phase, double painted) {
        double on = 0, off = 0;
        for (int i = 0; i < lengths.Count; i++) {
            double value = lengths[i];
            if (phase < value || i == lengths.Count - 1) return (i & 1) == 0 ? (on + phase) / total : (painted + off + phase) / total;
            phase -= value;
            if ((i & 1) == 0) on += value; else off += value;
        }
        return 0;
    }
}
