using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

/// <summary>One bounded paint-only observation of final logical route and owner-tail strokes.</summary>
internal sealed partial class TopologyCrossingPaintPlan {
    internal const int MaximumPairVisits = 100000;
    private readonly Dictionary<int, ChartPath> _paths = new();
    private int _pairVisits;
    private int _generatedCommands;

    private TopologyCrossingPaintPlan() { }
    internal IReadOnlyDictionary<int, ChartPath> Paths => _paths;
    internal int NearEndpointOrBendCount { get; private set; }
    internal int NonOrthogonalSegmentCount { get; private set; }
    internal int CrowdedCrossingCount { get; private set; }
    internal int CrossingCount { get; private set; }

    /// <summary>Input order is the actual deterministic native paint order, including the single owner tail.</summary>
    internal static TopologyCrossingPaintPlan Capture(IReadOnlyList<Stroke> strokes, TopologyCrossingPaint policy,
        int retainedPathCount = 0, int retainedCommandCount = 0) {
        if (strokes == null) throw new ArgumentNullException(nameof(strokes));
        if (policy == null) throw new ArgumentNullException(nameof(policy));
        if (retainedPathCount < 0 || retainedCommandCount < 0) throw new ArgumentOutOfRangeException(nameof(retainedPathCount));
        if (retainedCommandCount > TopologyPathDecoration.MaximumTotalCommands)
            throw new NotSupportedException("Crossing paint exceeds the native total-command limit.");
        if (retainedPathCount > TopologyPathDecoration.MaximumPaths || strokes.Count > TopologyPathDecoration.MaximumPaths - retainedPathCount)
            throw new NotSupportedException("Crossing paint exceeds the native path-count limit.");
        var plan = new TopologyCrossingPaintPlan(); var keys = new HashSet<int>(); var segments = new List<Segment>();
        plan._generatedCommands = retainedCommandCount;
        for (var row = 0; row < strokes.Count; row++) {
            var stroke = strokes[row];
            if (!keys.Add(stroke.Key)) throw new ArgumentException("A crossing stroke requires a unique native paint key.", nameof(strokes));
            if (stroke.SourcePointCount < 2 || stroke.SourcePointCount > TopologyPathDecoration.MaximumCommands)
                throw new NotSupportedException("Crossing paint requires 2 through 4096 native route points per path.");
            plan._generatedCommands = checked(plan._generatedCommands + stroke.SourcePointCount);
            if (plan._generatedCommands > TopologyPathDecoration.MaximumTotalCommands)
                throw new NotSupportedException("Crossing paint exceeds the native total-command limit.");
            for (var point = 0; point < stroke.Points.Count; point++) Validate(stroke.Points[point]);
            for (var point = 1; point < stroke.Points.Count; point++) {
                var segment = new Segment(stroke, row, point - 1, stroke.Points[point - 1], stroke.Points[point]);
                if (segment.Length == 0) continue;
                if (!segment.Orthogonal) plan.NonOrthogonalSegmentCount++;
                segments.Add(segment);
            }
        }
        var hops = new Dictionary<(int Stroke, int Segment), List<double>>(); var touched = new HashSet<int>();
        for (var later = 0; later < segments.Count; later++) {
            var target = segments[later]; if (!target.Orthogonal) continue;
            for (var earlier = 0; earlier < later; earlier++) {
                if (++plan._pairVisits > MaximumPairVisits) throw new NotSupportedException("Crossing paint exceeds its bounded 100000 segment-pair visits.");
                var under = segments[earlier];
                if (target.StrokeOrder == under.StrokeOrder || ReferenceEquals(target.Stroke.Edge, under.Stroke.Edge)
                    || !under.Orthogonal || target.Horizontal == under.Horizontal) continue;
                if (!ProperIntersection(target, under, policy.NumericEpsilon, out var distance)) continue;
                var startClearance = target.Index == 0 ? target.Stroke.StartClearance : 0;
                var endClearance = target.Index == target.Stroke.Points.Count - 2 ? target.Stroke.EndClearance : 0;
                if (Math.Min(distance - Math.Max(policy.BendClearance, startClearance), target.Length - Math.Max(policy.BendClearance, endClearance) - distance) < policy.MinimumRadius) {
                    plan.NearEndpointOrBendCount++; continue;
                }
                var key = (target.Stroke.Key, target.Index);
                if (!hops.TryGetValue(key, out var values)) hops.Add(key, values = new List<double>());
                if (!values.Any(value => Math.Abs(value - distance) <= policy.NumericEpsilon)) values.Add(distance);
                touched.Add(target.Stroke.Key);
            }
        }
        foreach (var stroke in strokes) {
            if (!touched.Contains(stroke.Key)) continue;
            var painted = plan.Paint(stroke, policy, hops);
            if (painted != null) plan._paths.Add(stroke.Key, painted);
        }
        return plan;
    }

    internal sealed class Stroke {
        internal Stroke(int key, TopologyEdge edge, IReadOnlyList<ChartPoint> points, double startClearance, double endClearance) {
            if (edge == null) throw new ArgumentNullException(nameof(edge));
            if (points == null) throw new ArgumentNullException(nameof(points));
            if (points.Count < 2 || points.Count > TopologyPathDecoration.MaximumCommands)
                throw new NotSupportedException("Crossing paint requires 2 through 4096 native route points per path.");
            if (!Finite(startClearance) || !Finite(endClearance) || startClearance < 0 || endClearance < 0)
                throw new ArgumentOutOfRangeException(nameof(startClearance));
            Key = key; Edge = edge; SourcePointCount = points.Count; Points = Normalize(points);
            StartClearance = startClearance; EndClearance = endClearance;
        }
        internal int SourcePointCount { get; }
        internal int Key { get; }
        internal TopologyEdge Edge { get; }
        internal IReadOnlyList<ChartPoint> Points { get; }
        internal double StartClearance { get; }
        internal double EndClearance { get; }
    }

    private static IReadOnlyList<ChartPoint> Normalize(IReadOnlyList<ChartPoint> source) {
        var points = new List<ChartPoint>(source.Count);
        foreach (var point in source) {
            Validate(point);
            if (points.Count != 0 && points[points.Count - 1].X == point.X && points[points.Count - 1].Y == point.Y) continue;
            if (points.Count > 1) {
                var a = points[points.Count - 2]; var b = points[points.Count - 1];
                if (a.Y == b.Y && b.Y == point.Y && Math.Sign(b.X - a.X) == Math.Sign(point.X - b.X)
                    || a.X == b.X && b.X == point.X && Math.Sign(b.Y - a.Y) == Math.Sign(point.Y - b.Y)) points.RemoveAt(points.Count - 1);
            }
            points.Add(point);
        }
        return points.AsReadOnly();
    }

    private readonly struct Segment {
        internal Segment(Stroke stroke, int strokeOrder, int index, ChartPoint first, ChartPoint last) {
            Stroke = stroke; StrokeOrder = strokeOrder; Index = index; First = first; Last = last;
            Horizontal = first.Y == last.Y; Orthogonal = Horizontal || first.X == last.X;
            Length = Horizontal ? Math.Abs(last.X - first.X) : Math.Abs(last.Y - first.Y);
        }
        internal Stroke Stroke { get; }
        internal int StrokeOrder { get; }
        internal int Index { get; }
        internal ChartPoint First { get; }
        internal ChartPoint Last { get; }
        internal bool Horizontal { get; }
        internal bool Orthogonal { get; }
        internal double Length { get; }
    }

    private static bool ProperIntersection(Segment target, Segment other, double epsilon, out double distance) {
        var horizontal = target.Horizontal ? target : other; var vertical = target.Horizontal ? other : target;
        var x = vertical.First.X; var y = horizontal.First.Y;
        distance = 0;
        // A genuine endpoint/T junction, shared endpoint or collinear run is never a hop.
        if (x <= Math.Min(horizontal.First.X, horizontal.Last.X) + epsilon || x >= Math.Max(horizontal.First.X, horizontal.Last.X) - epsilon
            || y <= Math.Min(vertical.First.Y, vertical.Last.Y) + epsilon || y >= Math.Max(vertical.First.Y, vertical.Last.Y) - epsilon) return false;
        distance = target.Horizontal ? Math.Abs(x - target.First.X) : Math.Abs(y - target.First.Y);
        return true;
    }
    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    private static void Validate(ChartPoint point) {
        if (!Finite(point.X) || !Finite(point.Y) || Math.Abs(point.X) > TopologyPathGeometry.MaximumCoordinate
            || Math.Abs(point.Y) > TopologyPathGeometry.MaximumCoordinate)
            throw new ArgumentOutOfRangeException(nameof(point), "Crossing paths require finite bounded native coordinates.");
    }
}
