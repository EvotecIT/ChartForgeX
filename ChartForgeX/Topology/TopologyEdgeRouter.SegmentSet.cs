using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyEdgeRouter {
    /// <summary>
    /// The routes of the other edges that a route is scored against. Every edge is scored against every other edge, so
    /// the set keeps references to their point lists instead of copying all of their segments for each edge.
    /// </summary>
    private sealed class RouteSegmentSet {
        public static readonly RouteSegmentSet Empty = new(Array.Empty<IReadOnlyList<ChartPoint>>(), Array.Empty<bool>(), Array.Empty<bool>(), 0);
        private readonly IReadOnlyList<ChartPoint>[] _routes;
        private readonly bool[] _skipFirst;
        private readonly bool[] _skipLast;
        private readonly int _count;

        private RouteSegmentSet(IReadOnlyList<ChartPoint>[] routes, bool[] skipFirst, bool[] skipLast, int count) {
            _routes = routes;
            _skipFirst = skipFirst;
            _skipLast = skipLast;
            _count = count;
        }

        /// <summary>Adds the overlap of <paramref name="segment"/> with every segment, in route order, to a running score.</summary>
        public double AddOverlap(double score, RouteSegment segment) {
            for (var route = 0; route < _count; route++) {
                var points = _routes[route];
                for (var i = 0; i < points.Count - 1; i++) {
                    // A terminal leg fanned apart at a shared card is left out, as it is drawn apart later.
                    if (i == 0 && _skipFirst[route] || i == points.Count - 2 && _skipLast[route]) continue;
                    score += segment.OverlapScore(new RouteSegment(points[i], points[i + 1]));
                }
            }

            return score;
        }

        public sealed class Builder {
            private readonly IReadOnlyList<ChartPoint>[] _routes;
            private readonly bool[] _skipFirst;
            private readonly bool[] _skipLast;
            private int _count;

            public Builder(int capacity) {
                _routes = new IReadOnlyList<ChartPoint>[capacity];
                _skipFirst = new bool[capacity];
                _skipLast = new bool[capacity];
            }

            public void Add(IReadOnlyList<ChartPoint> points, bool skipFirst, bool skipLast) {
                _routes[_count] = points;
                _skipFirst[_count] = skipFirst;
                _skipLast[_count++] = skipLast;
            }

            public RouteSegmentSet Build() => new(_routes, _skipFirst, _skipLast, _count);
        }
    }
}
