using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal sealed partial class ChartPath {
    /// <summary>Cuts the path where it crosses physical X boundaries without flattening curves.</summary>
    /// <remarks>Piece <c>i</c> holds the stroke that has reached <c>i</c> boundaries in source order, so a riser exactly on a
    /// boundary starts the next piece. Adjacent pieces share their cut point exactly, which keeps strokes joined after any
    /// affine projection. <paramref name="descending"/> marks a physical axis that runs against source order.</remarks>
    internal ChartPath[] PartitionX(IReadOnlyList<double> boundaries, bool descending) {
        if (boundaries == null) throw new ArgumentNullException(nameof(boundaries));
        var pieces = new List<ChartPathCommand>[boundaries.Count + 1];
        var ends = new ChartPoint?[pieces.Length];
        for (var index = 0; index < pieces.Length; index++) pieces[index] = new List<ChartPathCommand>();
        var current = default(ChartPoint); var hasCurrent = false;
        var cuts = new List<double>();
        foreach (var command in Commands) {
            var end = new ChartPoint(command.X, command.Y);
            if (command.Kind == ChartPathCommandKind.MoveTo || !hasCurrent) { current = end; hasCurrent = true; continue; }
            cuts.Clear();
            if (command.Kind == ChartPathCommandKind.LineTo) {
                foreach (var boundary in boundaries)
                    if ((current.X - boundary) * (end.X - boundary) < 0) cuts.Add((boundary - current.X) / (end.X - current.X));
            } else {
                var first = new ChartPoint(command.Control1X, command.Control1Y);
                var second = new ChartPoint(command.Control2X, command.Control2Y);
                foreach (var boundary in boundaries) CubicCrossings(current.X, first.X, second.X, end.X, boundary, cuts);
            }
            cuts.Sort(); cuts.Add(1);
            var from = 0d; var joined = current;
            foreach (var to in cuts) {
                if (to - from <= 1e-12) continue;
                if (command.Kind == ChartPathCommandKind.LineTo) {
                    var stop = to >= 1 ? end : Lerp(current, end, to);
                    Append(Piece(Lerp(current, end, (from + to) / 2).X), joined, ChartPathCommand.LineTo(stop.X, stop.Y));
                    joined = stop;
                } else {
                    var first = new ChartPoint(command.Control1X, command.Control1Y);
                    var second = new ChartPoint(command.Control2X, command.Control2Y);
                    var (_, c1, c2, cut) = SubCubic(current, first, second, end, from, to);
                    var stop = to >= 1 ? end : cut;
                    Append(Piece(CubicPoint(current, first, second, end, (from + to) / 2).X), joined,
                        ChartPathCommand.CubicTo(c1.X, c1.Y, c2.X, c2.Y, stop.X, stop.Y));
                    joined = stop;
                }
                from = to;
            }
            current = end;
        }
        var result = new ChartPath[pieces.Length];
        for (var index = 0; index < pieces.Length; index++) result[index] = new ChartPath(pieces[index]);
        return result;

        int Piece(double x) {
            var reached = 0;
            foreach (var boundary in boundaries) if (descending ? x <= boundary + 1e-7 : x >= boundary - 1e-7) reached++;
            return reached;
        }

        void Append(int piece, ChartPoint start, ChartPathCommand command) {
            if (ends[piece] is not { } last || Math.Abs(last.X - start.X) > 1e-9 || Math.Abs(last.Y - start.Y) > 1e-9)
                pieces[piece].Add(ChartPathCommand.MoveTo(start.X, start.Y));
            pieces[piece].Add(command);
            ends[piece] = new ChartPoint(command.X, command.Y);
        }
    }

    private static ChartPoint Lerp(ChartPoint start, ChartPoint end, double t) =>
        new(start.X + (end.X - start.X) * t, start.Y + (end.Y - start.Y) * t);

    /// <summary>Adds each parameter in (0, 1) where one cubic coordinate crosses a value, bisecting its monotone spans.</summary>
    private static void CubicCrossings(double start, double first, double second, double end, double value, List<double> cuts) {
        var stops = new List<double> { 0, 1 };
        CubicExtrema(start, first, second, end, t => { if (t > 0 && t < 1) stops.Add(t); });
        stops.Sort();
        double At(double t) {
            var inverse = 1 - t;
            return inverse * inverse * inverse * start + 3 * inverse * inverse * t * first + 3 * inverse * t * t * second + t * t * t * end - value;
        }
        for (var index = 1; index < stops.Count; index++) {
            double low = stops[index - 1], high = stops[index], lowValue = At(low);
            if (lowValue * At(high) >= 0) continue;
            for (var iteration = 0; iteration < 64; iteration++) {
                var middle = (low + high) / 2; var middleValue = At(middle);
                if (lowValue * middleValue <= 0) high = middle; else { low = middle; lowValue = middleValue; }
            }
            cuts.Add((low + high) / 2);
        }
    }

    /// <summary>Returns the exact cubic between two parameters by de Casteljau subdivision.</summary>
    private static (ChartPoint Start, ChartPoint First, ChartPoint Second, ChartPoint End) SubCubic(ChartPoint p0, ChartPoint p1, ChartPoint p2, ChartPoint p3, double from, double to) {
        // Keep [0, to], then keep the part of that curve after from / to.
        var (a0, a1, a2, a3) = SplitLeft(p0, p1, p2, p3, to);
        if (from <= 0) return (a0, a1, a2, a3);
        var t = from / to;
        var q1 = Lerp(a1, a2, t); var q2 = Lerp(a2, a3, t);
        var r0 = Lerp(Lerp(a0, a1, t), q1, t); var r1 = Lerp(q1, q2, t);
        return (Lerp(r0, r1, t), r1, q2, a3);
    }

    private static (ChartPoint, ChartPoint, ChartPoint, ChartPoint) SplitLeft(ChartPoint p0, ChartPoint p1, ChartPoint p2, ChartPoint p3, double t) {
        var q0 = Lerp(p0, p1, t); var q1 = Lerp(p1, p2, t); var q2 = Lerp(p2, p3, t);
        var r0 = Lerp(q0, q1, t); var r1 = Lerp(q1, q2, t);
        return (p0, q0, r0, Lerp(r0, r1, t));
    }
}
