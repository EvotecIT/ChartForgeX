using System;
using System.Linq;

namespace ChartForgeX.Topology;

internal static partial class TopologyLayoutEngine {
    // Rank direction owns automatic attachments. A diagonal centre ray can otherwise enter
    // a wide TD child through its side, even when a complete rank gutter is available above it.
    private static void ApplyLayeredFacingPorts(TopologyChart chart) {
        if (chart.LayoutMode != TopologyLayoutMode.Layered) return;
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var horizontal = chart.LayoutDirection is TopologyLayoutDirection.LeftToRight or TopologyLayoutDirection.RightToLeft;
        foreach (var edge in chart.Edges) {
            if (edge.PathGeometry != null || edge.Waypoints.Count > 0 || edge.Routing is not
                (TopologyEdgeRouting.Orthogonal or TopologyEdgeRouting.ObstacleAvoidingOrthogonal)) continue;
            if (!nodes.TryGetValue(edge.SourceNodeId, out var source) || !nodes.TryGetValue(edge.TargetNodeId, out var target)) continue;
            // Actor routing owns its measured external caption and may need a side corridor.
            if (TopologyNodeShapeGeometry.IsActor(source) || TopologyNodeShapeGeometry.IsActor(target)) continue;
            var difference = horizontal ? target.X + target.Width / 2 - source.X - source.Width / 2
                : target.Y + target.Height / 2 - source.Y - source.Height / 2;
            var minimum = horizontal ? (source.Width + target.Width) / 2 : (source.Height + target.Height) / 2;
            if (Math.Abs(difference) <= minimum) continue;
            var forward = difference > 0;
            if (edge.SourcePort == TopologyEdgePort.Auto) {
                edge.SourcePort = horizontal ? forward ? TopologyEdgePort.Right : TopologyEdgePort.Left
                    : forward ? TopologyEdgePort.Bottom : TopologyEdgePort.Top;
                edge.LayoutInference |= TopologyEdgeLayoutInference.SourcePort;
            }
            if (edge.TargetPort == TopologyEdgePort.Auto) {
                edge.TargetPort = horizontal ? forward ? TopologyEdgePort.Left : TopologyEdgePort.Right
                    : forward ? TopologyEdgePort.Top : TopologyEdgePort.Bottom;
                edge.LayoutInference |= TopologyEdgeLayoutInference.TargetPort;
            }
        }
    }
}
