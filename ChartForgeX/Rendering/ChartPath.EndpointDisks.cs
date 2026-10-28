using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal sealed partial class ChartPath {
    /// <summary>Crops a connected open path's leading/trailing endpoint-disk interiors, retaining its exact line/cubic interiors.</summary>
    /// <remarks>Boundary parameters use bounded polynomial root isolation in normalized coordinates and double precision.
    /// This is endpoint clearance, not subtraction of disk intersections later inside the retained route.</remarks>
    internal ChartPath TrimEndpointDisks(IReadOnlyList<(ChartPoint Center, double Radius)> startDisks,
        IReadOnlyList<(ChartPoint Center, double Radius)> endDisks) {
        var segments = EndpointSegments();
        if (segments.Count == 0) return this;
        double Boundary(IReadOnlyList<(ChartPoint Center, double Radius)> disks, bool fromStart) {
            if (disks.Count == 0) return fromStart ? 0 : segments.Count;
            for (var ordinal = 0; ordinal < segments.Count; ordinal++) {
                var index = fromStart ? ordinal : segments.Count - ordinal - 1;
                var segment = segments[index]; var roots = new List<double> { 0, 1 };
                foreach (var disk in disks) roots.AddRange(EndpointRoots(segment, disk.Center, disk.Radius));
                roots.Sort();
                for (var step = 0; step < roots.Count - 1; step++) {
                    var lowIndex = fromStart ? step : roots.Count - step - 2;
                    var low = roots[lowIndex]; var high = roots[lowIndex + 1];
                    if (high <= low) continue;
                    var point = EndpointPoint(segment, low + (high - low) / 2);
                    if (disks.All(disk => !EndpointInside(point, disk.Center, disk.Radius)))
                        return index + (fromStart ? low : high);
                }
            }
            return fromStart ? segments.Count : 0;
        }
        var first = Boundary(startDisks, true); var last = Boundary(endDisks, false);
        if (first >= last) return new ChartPath(Array.Empty<ChartPathCommand>());
        var commands = new List<ChartPathCommand>();
        for (var index = (int)Math.Floor(first); index < segments.Count && index < last; index++) {
            var low = Math.Max(0, first - index); var high = Math.Min(1, last - index);
            if (high <= low) continue;
            var segment = EndpointSlice(segments[index], low, high);
            if (commands.Count == 0) commands.Add(ChartPathCommand.MoveTo(segment.Start.X, segment.Start.Y));
            commands.Add(segment.Cubic ? ChartPathCommand.CubicTo(segment.First.X, segment.First.Y, segment.Second.X, segment.Second.Y, segment.End.X, segment.End.Y)
                : ChartPathCommand.LineTo(segment.End.X, segment.End.Y));
        }
        return new ChartPath(commands);
    }

    private readonly struct EndpointSegment {
        internal EndpointSegment(ChartPoint start, ChartPoint first, ChartPoint second, ChartPoint end, bool cubic) {
            Start = start; First = first; Second = second; End = end; Cubic = cubic;
        }
        internal ChartPoint Start { get; } internal ChartPoint First { get; } internal ChartPoint Second { get; } internal ChartPoint End { get; }
        internal bool Cubic { get; }
    }
    private List<EndpointSegment> EndpointSegments() {
        var result = new List<EndpointSegment>(); var current = default(ChartPoint); var hasStart = false;
        foreach (var command in Commands) {
            var end = new ChartPoint(command.X, command.Y);
            if (command.Kind == ChartPathCommandKind.MoveTo) {
                if (hasStart) throw new InvalidOperationException("Endpoint clearance requires one connected open path.");
                hasStart = true;
            } else {
                if (!hasStart) throw new InvalidOperationException("Endpoint clearance requires a starting point.");
                var cubic = command.Kind == ChartPathCommandKind.CubicTo;
                result.Add(new EndpointSegment(current, cubic ? new ChartPoint(command.Control1X, command.Control1Y) : current,
                    cubic ? new ChartPoint(command.Control2X, command.Control2Y) : end, end, cubic));
            }
            current = end;
        }
        return result;
    }
    private static bool EndpointInside(ChartPoint point, ChartPoint center, double radius) {
        if (radius <= 0) return false;
        var x = (point.X - center.X) / radius; var y = (point.Y - center.Y) / radius;
        return x * x + y * y < 1;
    }
    private static ChartPoint EndpointLerp(ChartPoint a, ChartPoint b, double t) => new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
    private static ChartPoint EndpointPoint(EndpointSegment segment, double t) => segment.Cubic
        ? CubicPoint(segment.Start, segment.First, segment.Second, segment.End, t) : EndpointLerp(segment.Start, segment.End, t);

    // de Casteljau slicing preserves the original curve's tangents and interior; no sampled polyline replaces exported commands.
    private static (EndpointSegment Left, EndpointSegment Right) EndpointSplit(EndpointSegment s, double t) {
        if (!s.Cubic) {
            var point = EndpointLerp(s.Start, s.End, t);
            return (new EndpointSegment(s.Start, s.Start, point, point, false), new EndpointSegment(point, point, s.End, s.End, false));
        }
        var a = EndpointLerp(s.Start, s.First, t); var b = EndpointLerp(s.First, s.Second, t); var c = EndpointLerp(s.Second, s.End, t);
        var d = EndpointLerp(a, b, t); var e = EndpointLerp(b, c, t); var middle = EndpointLerp(d, e, t);
        return (new EndpointSegment(s.Start, a, d, middle, true), new EndpointSegment(middle, e, c, s.End, true));
    }
    private static EndpointSegment EndpointSlice(EndpointSegment segment, double low, double high) {
        if (high < 1) segment = EndpointSplit(segment, high).Left;
        return low > 0 ? EndpointSplit(segment, low / high).Right : segment;
    }

    private static IEnumerable<double> EndpointRoots(EndpointSegment segment, ChartPoint center, double radius) {
        if (double.IsNaN(radius) || double.IsInfinity(radius) || radius < 0) throw new ArgumentOutOfRangeException(nameof(radius));
        if (radius == 0) return Array.Empty<double>();
        var points = new[] { segment.Start, segment.First, segment.Second, segment.End };
        var scale = radius;
        foreach (var point in points) scale = Math.Max(scale, Math.Max(Math.Abs(point.X - center.X), Math.Abs(point.Y - center.Y)));
        double[] Coefficients(bool horizontal) {
            var p = points.Select(point => ((horizontal ? point.X : point.Y) - (horizontal ? center.X : center.Y)) / scale).ToArray();
            return segment.Cubic ? new[] { p[0], 3 * (p[1] - p[0]), 3 * (p[0] - 2 * p[1] + p[2]), p[3] - p[0] + 3 * (p[1] - p[2]) }
                : new[] { p[0], p[3] - p[0] };
        }
        var x = Coefficients(true); var y = Coefficients(false); var coefficients = new double[x.Length * 2 - 1];
        for (var i = 0; i < x.Length; i++) for (var j = 0; j < x.Length; j++) coefficients[i + j] += x[i] * x[j] + y[i] * y[j];
        coefficients[0] -= (radius / scale) * (radius / scale);
        return EndpointPolynomialRoots(coefficients);
    }
    private static List<double> EndpointPolynomialRoots(double[] coefficients) {
        var degree = coefficients.Length - 1;
        while (degree > 0 && coefficients[degree] == 0) degree--;
        if (degree == 0) return new List<double>();
        double Value(double t) { var value = coefficients[degree]; for (var index = degree - 1; index >= 0; index--) value = value * t + coefficients[index]; return value; }
        if (degree == 1) {
            var t = -coefficients[0] / coefficients[1]; return t > 0 && t < 1 ? new List<double> { t } : new List<double>();
        }
        var derivative = new double[degree];
        for (var index = 1; index <= degree; index++) derivative[index - 1] = index * coefficients[index];
        var critical = EndpointPolynomialRoots(derivative); critical.Insert(0, 0); critical.Add(1);
        var roots = new List<double>();
        // A near-zero stationary root is a tangent; it does not on its own change inside/outside membership.
        var tolerance = coefficients.Sum(Math.Abs) * 2e-14;
        foreach (var t in critical) if (t > 0 && t < 1 && Math.Abs(Value(t)) <= tolerance) roots.Add(t);
        for (var index = 1; index < critical.Count; index++) {
            var low = critical[index - 1]; var high = critical[index]; var left = Value(low); var right = Value(high);
            if (left == 0 || right == 0 || (left < 0) == (right < 0)) continue;
            for (var iteration = 0; iteration < 56; iteration++) {
                var middle = low + (high - low) / 2; var value = Value(middle);
                if (value == 0) { low = high = middle; break; }
                if ((value < 0) == (left < 0)) { low = middle; left = value; } else high = middle;
            }
            roots.Add(low + (high - low) / 2);
        }
        roots.Sort(); return roots;
    }
}
