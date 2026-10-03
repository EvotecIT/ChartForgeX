using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyDenseRoutePlanner {
    /// <summary>Snapshots authored routes using the same ports, parallel offsets and curve sampling as the renderers.</summary>
    private static List<List<ChartPoint>> FixedRoutes(TopologyChart chart, Dictionary<string, TopologyNode> nodes) {
        var routes = new List<List<ChartPoint>>();
        foreach (var edge in chart.Edges) {
            if (edge.Routing == TopologyEdgeRouting.ObstacleAvoidingOrthogonal && edge.Waypoints.Count == 0) continue;
            if (!nodes.ContainsKey(edge.SourceNodeId) || !nodes.ContainsKey(edge.TargetNodeId)) continue;
            var points = TopologyRenderPrimitives.EdgePoints(chart, edge, nodes);
            routes.Add(TopologyRenderPrimitives.RenderedEdgeSamplePoints(chart, edge, nodes, points));
        }
        return routes;
    }

    private sealed partial class Grid {
        /// <summary>
        /// Adds or removes corridor occupancy. Orthogonal runs mark their grid intervals, including direct routes;
        /// sampled curves and diagonals mark the grid moves they cross. Removal supports bounded rip-up/reroute.
        /// </summary>
        public void Record(IReadOnlyList<ChartPoint> points, int delta) {
            for (var i = 0; i + 1 < points.Count; i++) {
                var a = points[i];
                var b = points[i + 1];
                var row = Near(Ys, a.Y);
                var column = Near(Xs, a.X);
                if (Math.Abs(a.Y - b.Y) < 0.01 && row >= 0) {
                    for (var x = Math.Max(0, UpperBound(Xs, Math.Min(a.X, b.X)) - 1); x + 1 < Width && Xs[x] < Math.Max(a.X, b.X) - 0.01; x++)
                        _usedRight[row * Width + x] += delta;
                    continue;
                }
                if (Math.Abs(a.X - b.X) < 0.01 && column >= 0) {
                    for (var y = Math.Max(0, UpperBound(Ys, Math.Min(a.Y, b.Y)) - 1); y + 1 < Height && Ys[y] < Math.Max(a.Y, b.Y) - 0.01; y++)
                        _usedDown[y * Width + column] += delta;
                    continue;
                }
                // Each crossing is projected only onto the containing grid interval, never the segment's whole box.
                if (Math.Abs(b.Y - a.Y) > 0.01) {
                    for (var y = LowerBound(Ys, Math.Min(a.Y, b.Y)); y < Height && Ys[y] <= Math.Max(a.Y, b.Y); y++) {
                        var x = a.X + (Ys[y] - a.Y) * (b.X - a.X) / (b.Y - a.Y);
                        var cellX = UpperBound(Xs, x) - 1;
                        if (cellX >= 0 && cellX + 1 < Width) _crossRight[y * Width + cellX] += delta;
                    }
                }
                if (Math.Abs(b.X - a.X) > 0.01) {
                    for (var x = LowerBound(Xs, Math.Min(a.X, b.X)); x < Width && Xs[x] <= Math.Max(a.X, b.X); x++) {
                        var y = a.Y + (Xs[x] - a.X) * (b.Y - a.Y) / (b.X - a.X);
                        var cellY = UpperBound(Ys, y) - 1;
                        if (cellY >= 0 && cellY + 1 < Height) _crossDown[cellY * Width + x] += delta;
                    }
                }
            }
        }

        public int FixedCrossings(int from, int to, int direction) => Math.Min(MaximumSharedRoutes,
            direction < 2 ? _crossRight[Math.Min(from, to)] : _crossDown[Math.Min(from, to)]);

        private static int Near(double[] values, double value) {
            var index = LowerBound(values, value);
            if (index < values.Length && Math.Abs(values[index] - value) <= 0.26) return index;
            return index > 0 && Math.Abs(values[index - 1] - value) <= 0.26 ? index - 1 : -1;
        }
    }
}
