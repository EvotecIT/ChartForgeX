using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyLayoutEngine {
    private const double LongEdgeLaneWidth = 36;

    /// <summary>
    /// Reserves a corridor in every intermediate rank of an edge that skips ranks, so the edge is routed
    /// beside the nodes of those ranks instead of straight through them.
    /// </summary>
    private sealed class LongEdgeLanePlan {
        private readonly List<LongEdgeLane> _lanes;
        private readonly double[] _rankStart;
        private readonly double[] _rankEnd;

        private LongEdgeLanePlan(List<LongEdgeLane> lanes, int rankCount) {
            _lanes = lanes;
            _rankStart = new double[rankCount];
            _rankEnd = new double[rankCount];
        }

        public static LongEdgeLanePlan Create(TopologyChart chart, IReadOnlyList<LayerPlacement> layers, bool horizontal, double crossLength, double gap) {
            var rankByNode = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var rank = 0; rank < layers.Count; rank++) {
                foreach (var node in layers[rank].Nodes) rankByNode[node.Id] = rank;
            }

            var lanes = new List<LongEdgeLane>();
            for (var order = 0; order < chart.Edges.Count; order++) {
                var edge = chart.Edges[order];
                if (edge.Waypoints.Count > 0 || edge.Routing != TopologyEdgeRouting.Orthogonal || IsHierarchyParentChildEdge(edge)) continue;
                if (edge.SourcePort != TopologyEdgePort.Auto || edge.TargetPort != TopologyEdgePort.Auto
                    || !string.IsNullOrWhiteSpace(edge.SourcePortId) || !string.IsNullOrWhiteSpace(edge.TargetPortId)) continue;
                if (!rankByNode.TryGetValue(edge.SourceNodeId, out var sourceRank) || !rankByNode.TryGetValue(edge.TargetNodeId, out var targetRank)) continue;
                if (Math.Abs(sourceRank - targetRank) < 2) continue;
                lanes.Add(new LongEdgeLane(edge, order, sourceRank, targetRank));
            }

            // Decide every corridor before placement so a rank that cannot host it never reserves dead space earlier.
            for (var rank = 0; rank < layers.Count; rank++) {
                var layer = layers[rank];
                var passing = lanes.Where(lane => lane.Passes(rank)).ToList();
                if (passing.Count == 0) continue;
                var total = layer.Nodes.Sum(node => horizontal ? node.Height : node.Width) + passing.Count * LongEdgeLaneWidth
                    + Math.Max(0, layer.Nodes.Count + passing.Count - 1) * gap;
                var fits = (horizontal ? layer.Columns : layer.Rows) == 1 && total <= crossLength
                    && layer.Nodes.All(node => IsUnset(node.X) && IsUnset(node.Y));
                if (!fits) foreach (var lane in passing) lane.Rejected = true;
            }

            lanes.RemoveAll(lane => lane.Rejected);
            return new LongEdgeLanePlan(lanes, layers.Count);
        }

        public bool HasLanes(int rank) => _lanes.Any(lane => lane.Passes(rank) && !lane.Rejected);

        public void RecordRank(int rank, double start, double end) {
            _rankStart[rank] = start;
            _rankEnd[rank] = end;
        }

        public void Reject(int rank) {
            foreach (var lane in _lanes) {
                if (lane.Passes(rank)) lane.Rejected = true;
            }
        }

        /// <summary>
        /// Packs one rank's nodes and corridors along the cross axis. Items follow the barycenter of their
        /// connections in the preceding rank, then source declaration order, so branches keep their authored order.
        /// </summary>
        public void Place(TopologyChart chart, LayerPlacement layer, int rank, bool horizontal, double crossStart, double crossLength, double rankPosition, double gap, IReadOnlyDictionary<string, TopologyNode> nodesById) {
            var items = new List<LaneItem>();
            for (var i = 0; i < layer.Nodes.Count; i++) {
                var node = layer.Nodes[i];
                items.Add(new LaneItem(node, null, horizontal ? node.Height : node.Width, Barycenter(chart, node, rank, horizontal, nodesById), FirstEdgeOrder(chart, node), i));
            }

            foreach (var lane in _lanes.Where(lane => lane.Passes(rank) && !lane.Rejected)) {
                items.Add(new LaneItem(null, lane, LongEdgeLaneWidth, lane.PreviousCenter(rank, horizontal, nodesById), lane.Order, layer.Nodes.Count + lane.Order));
            }

            var total = items.Sum(item => item.Size) + Math.Max(0, items.Count - 1) * gap;
            var cursor = crossStart + (crossLength - total) / 2;
            var column = 0;
            foreach (var item in items.OrderBy(item => item.Barycenter).ThenBy(item => item.Order).ThenBy(item => item.Index)) {
                if (item.Node != null) {
                    ApplyLayerMetadata(item.Node, layer, horizontal ? column : 0, horizontal ? 0 : column);
                    if (horizontal) {
                        item.Node.X = rankPosition + (layer.MaxWidth - item.Node.Width) / 2;
                        item.Node.Y = cursor;
                    } else {
                        item.Node.X = cursor;
                        item.Node.Y = rankPosition + (layer.MaxHeight - item.Node.Height) / 2;
                    }
                } else {
                    item.Lane!.Centers[rank] = cursor + item.Size / 2;
                }

                cursor += item.Size + gap;
                column++;
            }
        }

        public void ApplyRoutes(IReadOnlyDictionary<string, TopologyNode> nodesById, bool horizontal) {
            foreach (var lane in _lanes) {
                if (lane.Rejected || !nodesById.TryGetValue(lane.Edge.SourceNodeId, out var source) || !nodesById.TryGetValue(lane.Edge.TargetNodeId, out var target)) continue;
                var low = lane.Reversed ? target : source;
                var high = lane.Reversed ? source : target;
                ChartPoint At(double cross, double main) => horizontal ? new ChartPoint(main, cross) : new ChartPoint(cross, main);
                double Bus(int rank) => (_rankEnd[rank - 1] + _rankStart[rank]) / 2;
                var first = lane.Centers[lane.LowRank + 1];
                var last = lane.Centers[lane.HighRank - 1];
                // A corridor wholly beside an endpoint leaves or enters through that endpoint's side, so it does not
                // share the rank-facing stub with the endpoint's direct neighbours.
                var lowSide = Beside(low, first, horizontal);
                var highSide = Beside(high, last, horizontal);
                var points = new List<ChartPoint>();
                if (lowSide) AddDistinct(points, At(first, Main(low, horizontal)));
                else {
                    AddDistinct(points, At(Cross(low, horizontal), Bus(lane.LowRank + 1)));
                    AddDistinct(points, At(first, Bus(lane.LowRank + 1)));
                }

                for (var rank = lane.LowRank + 1; rank < lane.HighRank - 1; rank++) {
                    AddDistinct(points, At(lane.Centers[rank], Bus(rank + 1)));
                    AddDistinct(points, At(lane.Centers[rank + 1], Bus(rank + 1)));
                }

                if (highSide) AddDistinct(points, At(last, Main(high, horizontal)));
                else {
                    AddDistinct(points, At(last, Bus(lane.HighRank)));
                    AddDistinct(points, At(Cross(high, horizontal), Bus(lane.HighRank)));
                }

                if (lane.Reversed) points.Reverse();
                var edge = lane.Edge;
                edge.Waypoints.Clear();
                edge.Waypoints.AddRange(points);
                var lowPort = lowSide ? SidePort(first < Cross(low, horizontal), horizontal) : horizontal ? TopologyEdgePort.Right : TopologyEdgePort.Bottom;
                var highPort = highSide ? SidePort(last < Cross(high, horizontal), horizontal) : horizontal ? TopologyEdgePort.Left : TopologyEdgePort.Top;
                edge.SourcePort = lane.Reversed ? highPort : lowPort;
                edge.TargetPort = lane.Reversed ? lowPort : highPort;
                edge.Metadata["layout.route"] = "long-edge-lane";
            }
        }

        private static double Barycenter(TopologyChart chart, TopologyNode node, int rank, bool horizontal, IReadOnlyDictionary<string, TopologyNode> nodesById) {
            var total = 0d;
            var count = 0;
            foreach (var edge in chart.Edges) {
                var otherId = string.Equals(edge.TargetNodeId, node.Id, StringComparison.Ordinal) ? edge.SourceNodeId
                    : string.Equals(edge.SourceNodeId, node.Id, StringComparison.Ordinal) ? edge.TargetNodeId : null;
                if (otherId == null || !nodesById.TryGetValue(otherId, out var other) || RankIndex(other) != rank - 1) continue;
                total += Cross(other, horizontal);
                count++;
            }

            return count == 0 ? double.MaxValue : total / count;
        }

        private static int FirstEdgeOrder(TopologyChart chart, TopologyNode node) {
            for (var order = 0; order < chart.Edges.Count; order++) {
                if (string.Equals(chart.Edges[order].TargetNodeId, node.Id, StringComparison.Ordinal)) return order;
            }

            return int.MaxValue;
        }

        private static int RankIndex(TopologyNode node) =>
            node.Metadata.TryGetValue("layout.layerIndex", out var value) && int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var index) ? index : -1;

        private static double Cross(TopologyNode node, bool horizontal) => horizontal ? CenterY(node) : CenterX(node);

        private static double Main(TopologyNode node, bool horizontal) => horizontal ? CenterX(node) : CenterY(node);

        private static bool Beside(TopologyNode node, double cross, bool horizontal) =>
            Math.Abs(cross - Cross(node, horizontal)) > (horizontal ? node.Height : node.Width) / 2;

        private static TopologyEdgePort SidePort(bool before, bool horizontal) =>
            horizontal ? before ? TopologyEdgePort.Top : TopologyEdgePort.Bottom : before ? TopologyEdgePort.Left : TopologyEdgePort.Right;

        private static void AddDistinct(List<ChartPoint> points, ChartPoint point) {
            if (points.Count > 0 && Math.Abs(points[points.Count - 1].X - point.X) < 0.01 && Math.Abs(points[points.Count - 1].Y - point.Y) < 0.01) return;
            points.Add(point);
        }
    }

    private sealed class LongEdgeLane {
        public LongEdgeLane(TopologyEdge edge, int order, int sourceRank, int targetRank) {
            Edge = edge;
            Order = order;
            LowRank = Math.Min(sourceRank, targetRank);
            HighRank = Math.Max(sourceRank, targetRank);
            Reversed = sourceRank > targetRank;
        }

        public TopologyEdge Edge { get; }
        public int Order { get; }
        public int LowRank { get; }
        public int HighRank { get; }
        public bool Reversed { get; }
        public bool Rejected { get; set; }
        public Dictionary<int, double> Centers { get; } = new();

        public bool Passes(int rank) => rank > LowRank && rank < HighRank;

        public double PreviousCenter(int rank, bool horizontal, IReadOnlyDictionary<string, TopologyNode> nodesById) {
            if (rank - 1 > LowRank) return Centers.TryGetValue(rank - 1, out var center) ? center : double.MaxValue;
            var lowId = Reversed ? Edge.TargetNodeId : Edge.SourceNodeId;
            return nodesById.TryGetValue(lowId, out var low) ? horizontal ? CenterY(low) : CenterX(low) : double.MaxValue;
        }
    }

    private sealed class LaneItem {
        public LaneItem(TopologyNode? node, LongEdgeLane? lane, double size, double barycenter, int order, int index) {
            Node = node;
            Lane = lane;
            Size = size;
            Barycenter = barycenter;
            Order = order;
            Index = index;
        }

        public TopologyNode? Node { get; }
        public LongEdgeLane? Lane { get; }
        public double Size { get; }
        public double Barycenter { get; }
        public int Order { get; }
        public int Index { get; }
    }
}
