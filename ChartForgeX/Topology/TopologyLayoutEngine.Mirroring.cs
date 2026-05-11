using System;
using System.Globalization;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyLayoutEngine {
    private static void MirrorLayoutVertically(TopologyChart chart, double top, double bottom) {
        var axis = top + bottom;
        foreach (var group in chart.Groups) group.Y = axis - group.Y - group.Height;
        foreach (var node in chart.Nodes) node.Y = axis - node.Y - node.Height;
        foreach (var edge in chart.Edges) {
            MirrorVerticalPorts(edge);
            edge.RouteLane = -edge.RouteLane;
            for (var i = 0; i < edge.Waypoints.Count; i++) {
                var point = edge.Waypoints[i];
                edge.Waypoints[i] = new ChartPoint(point.X, axis - point.Y);
            }

            if (edge.Metadata.TryGetValue("hierarchy.route.busY", out var busY) && double.TryParse(busY, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)) {
                edge.Metadata["hierarchy.route.busY"] = (axis - value).ToString("0.###", CultureInfo.InvariantCulture);
            }
        }
    }

    private static void MirrorLayoutHorizontally(TopologyChart chart, double left, double right) {
        var axis = left + right;
        foreach (var group in chart.Groups) group.X = axis - group.X - group.Width;
        foreach (var node in chart.Nodes) node.X = axis - node.X - node.Width;
        foreach (var edge in chart.Edges) {
            MirrorHorizontalPorts(edge);
            edge.RouteLane = -edge.RouteLane;
            for (var i = 0; i < edge.Waypoints.Count; i++) {
                var point = edge.Waypoints[i];
                edge.Waypoints[i] = new ChartPoint(axis - point.X, point.Y);
            }

            if (edge.Metadata.TryGetValue("hierarchy.route.busX", out var busX) && double.TryParse(busX, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)) {
                edge.Metadata["hierarchy.route.busX"] = (axis - value).ToString("0.###", CultureInfo.InvariantCulture);
            }
        }
    }

    private static void MirrorVerticalPorts(TopologyEdge edge) {
        edge.SourcePort = MirrorVerticalPort(edge.SourcePort);
        edge.TargetPort = MirrorVerticalPort(edge.TargetPort);
    }

    private static void MirrorHorizontalPorts(TopologyEdge edge) {
        edge.SourcePort = MirrorHorizontalPort(edge.SourcePort);
        edge.TargetPort = MirrorHorizontalPort(edge.TargetPort);
    }

    private static TopologyEdgePort MirrorVerticalPort(TopologyEdgePort port) {
        return port switch {
            TopologyEdgePort.Top => TopologyEdgePort.Bottom,
            TopologyEdgePort.Bottom => TopologyEdgePort.Top,
            _ => port
        };
    }

    private static TopologyEdgePort MirrorHorizontalPort(TopologyEdgePort port) {
        return port switch {
            TopologyEdgePort.Left => TopologyEdgePort.Right,
            TopologyEdgePort.Right => TopologyEdgePort.Left,
            _ => port
        };
    }
}
