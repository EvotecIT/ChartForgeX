using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal sealed partial class ChartPath {
    /// <summary>Retains every visible periodic native fragment without joining separate copies or resampling curves.</summary>
    /// <remarks>Segment-local shift ranges avoid cloning an entire winding route for every intersecting turn.
    /// One original visible copy returns this exact path, retaining established narrow-window commands and arithmetic.</remarks>
    internal IReadOnlyList<ChartPath> PeriodicRectangleFragments(ChartRect rectangle, double period, out bool clipped) {
        if (double.IsNaN(period) || double.IsInfinity(period) || period <= 0) throw new ArgumentOutOfRangeException(nameof(period));
        var bounds = Bounds();
        clipped = bounds.Left < rectangle.Left || bounds.Right > rectangle.Right || bounds.Top < rectangle.Top || bounds.Bottom > rectangle.Bottom;
        if (Commands.Count < 2) return Array.Empty<ChartPath>();
        var whole = PeriodicRange(bounds.Left, bounds.Right, rectangle, period);
        if (whole.First == 0 && whole.Last == 0)
            return IntersectsRectangle(rectangle) ? new[] { this } : Array.Empty<ChartPath>();

        var segments = EndpointSegments(); var plans = new List<PeriodicSegmentPlan>(segments.Count); long candidates = 0;
        for (var index = 0; index < segments.Count; index++) {
            var segment = segments[index];
            var left = Math.Min(segment.Start.X, segment.End.X); var right = Math.Max(segment.Start.X, segment.End.X);
            var top = Math.Min(segment.Start.Y, segment.End.Y); var bottom = Math.Max(segment.Start.Y, segment.End.Y);
            if (segment.Cubic) {
                left = Math.Min(left, Math.Min(segment.First.X, segment.Second.X)); right = Math.Max(right, Math.Max(segment.First.X, segment.Second.X));
                top = Math.Min(top, Math.Min(segment.First.Y, segment.Second.Y)); bottom = Math.Max(bottom, Math.Max(segment.First.Y, segment.Second.Y));
            }
            if (top > rectangle.Bottom || bottom < rectangle.Top) continue;
            var range = PeriodicRange(left, right, rectangle, period);
            if (range.First > range.Last) continue;
            var count = checked(range.Last - range.First + 1);
            // Four cubic boundaries have at most twelve roots: <=13 intervals and <=14 isolated contacts.
            // Count the conservative command expansion before any periodic geometry is allocated.
            if (count > int.MaxValue / 56L - candidates)
                throw new InvalidOperationException("Periodic native route commands exceed the supported collection size.");
            candidates += count;
            plans.Add(new PeriodicSegmentPlan(index, segment, range.First, range.Last));
        }
        var fragments = new List<PeriodicFragment>(); var latest = new Dictionary<long, PeriodicFragment>();
        var hasCopy = false; long minimumCopy = 0, maximumCopy = 0;
        foreach (var plan in plans) for (var copy = plan.FirstCopy; ; copy++) {
            var offset = copy * period;
            if (double.IsNaN(offset) || double.IsInfinity(offset)) throw new InvalidOperationException("Periodic native route offsets must be finite.");
            var segment = PeriodicShift(plan.Segment, offset);
            var parameters = RectangleParameters(segment, rectangle);
            var intervals = new List<(double Low, double High)>();
            for (var index = 1; index < parameters.Count; index++) {
                var low = parameters[index - 1]; var high = parameters[index];
                if (high > low && RectangleContains(rectangle, EndpointPoint(segment, low + (high - low) / 2)))
                    intervals.Add((low, high));
            }
            // Inclusive native clips retain isolated contacts too; they never fabricate a bridge across outside intervals.
            foreach (var parameter in parameters) {
                if (!RectangleContains(rectangle, EndpointPoint(segment, parameter))) continue;
                var covered = false;
                foreach (var interval in intervals) if (parameter >= interval.Low && parameter <= interval.High) { covered = true; break; }
                if (!covered) intervals.Add((parameter, parameter));
            }
            intervals.Sort((a, b) => a.Low.CompareTo(b.Low));
            foreach (var interval in intervals) {
                if (!hasCopy) { minimumCopy = maximumCopy = copy; hasCopy = true; }
                else { minimumCopy = Math.Min(minimumCopy, copy); maximumCopy = Math.Max(maximumCopy, copy); }
                var piece = EndpointSlice(segment, interval.Low, interval.High);
                var join = latest.TryGetValue(copy, out var previous) && (previous.LastSegment == plan.Index && previous.LastParameter == interval.Low
                    || previous.LastSegment == plan.Index - 1 && previous.LastParameter == 1 && interval.Low == 0);
                if (!join) {
                    previous = new PeriodicFragment(piece.Start); fragments.Add(previous); latest[copy] = previous;
                }
                var target = previous!;
                target.Commands.Add(piece.Cubic ? ChartPathCommand.CubicTo(piece.First.X, piece.First.Y, piece.Second.X, piece.Second.Y, piece.End.X, piece.End.Y)
                    : ChartPathCommand.LineTo(piece.End.X, piece.End.Y));
                target.LastSegment = plan.Index; target.LastParameter = interval.High;
            }
            if (copy == plan.LastCopy) break;
        }
        if (!hasCopy) return Array.Empty<ChartPath>();
        clipped = bounds.Left + minimumCopy * period < rectangle.Left || bounds.Right + maximumCopy * period > rectangle.Right
            || bounds.Top < rectangle.Top || bounds.Bottom > rectangle.Bottom;
        if (minimumCopy == 0 && maximumCopy == 0) return new[] { this };
        var result = new ChartPath[fragments.Count];
        for (var index = 0; index < result.Length; index++) result[index] = new ChartPath(fragments[index].Commands.ToArray());
        return result;
    }

    private static (long First, long Last) PeriodicRange(double left, double right, ChartRect rectangle, double period) {
        var first = Math.Ceiling((rectangle.Left - right) / period); var last = Math.Floor((rectangle.Right - left) / period);
        if (double.IsNaN(first) || double.IsInfinity(first) || double.IsNaN(last) || double.IsInfinity(last)
            || first < long.MinValue || first >= long.MaxValue || last < long.MinValue || last >= long.MaxValue)
            throw new InvalidOperationException("Periodic native route indices exceed the supported integer range.");
        return ((long)first, (long)last);
    }
    private static EndpointSegment PeriodicShift(EndpointSegment segment, double offset) => new(
        new ChartPoint(segment.Start.X + offset, segment.Start.Y), new ChartPoint(segment.First.X + offset, segment.First.Y),
        new ChartPoint(segment.Second.X + offset, segment.Second.Y), new ChartPoint(segment.End.X + offset, segment.End.Y), segment.Cubic);
    private static bool RectangleContains(ChartRect rectangle, ChartPoint point) =>
        point.X >= rectangle.Left && point.X <= rectangle.Right && point.Y >= rectangle.Top && point.Y <= rectangle.Bottom;

    private readonly struct PeriodicSegmentPlan {
        internal PeriodicSegmentPlan(int index, EndpointSegment segment, long firstCopy, long lastCopy) {
            Index = index; Segment = segment; FirstCopy = firstCopy; LastCopy = lastCopy;
        }
        internal int Index { get; } internal EndpointSegment Segment { get; }
        internal long FirstCopy { get; } internal long LastCopy { get; }
    }
    private sealed class PeriodicFragment {
        internal PeriodicFragment(ChartPoint start) { Commands.Add(ChartPathCommand.MoveTo(start.X, start.Y)); }
        internal readonly List<ChartPathCommand> Commands = new();
        internal int LastSegment; internal double LastParameter;
    }
}
