using System;
using System.Globalization;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyLayoutEngine {
    private static void MirrorLayoutVertically(TopologyChart chart, double top, double bottom) {
        var axis = top + bottom;
        Dictionary<string, double>? stackOffsets = null;
        foreach (var group in chart.Groups) {
            var mirrored = axis - group.Y - group.Height;
            // A source-ordered vertical stack moves as a panel; reversing the board does not reverse its cards.
            if (group.AppliedLayoutPolicy == TopologyGroupLayoutPolicy.VerticalStack)
                (stackOffsets ??= new Dictionary<string, double>(StringComparer.Ordinal))[group.Id] = mirrored - group.Y;
            group.Y = mirrored;
        }
        foreach (var node in chart.Nodes) {
            if (stackOffsets != null && node.GroupId != null && stackOffsets.TryGetValue(node.GroupId, out var shift)) node.Y += shift;
            else node.Y = axis - node.Y - (chart.LayoutMode == TopologyLayoutMode.Layered ? LayeredNodeHeight(chart, node) : node.Height);
        }
        foreach (var edge in chart.Edges) {
            MirrorVerticalPorts(edge);
            edge.RouteLane = -edge.RouteLane;
            if (edge.HasLabelAnchorOverride) edge.LabelAnchorY = axis - edge.LabelAnchorY;
            if (edge.LabelPosition != null) edge.LabelPosition = edge.LabelPosition.Transform(point => new ChartPoint(point.X, axis - point.Y));
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
            if (edge.HasLabelAnchorOverride) edge.LabelAnchorX = axis - edge.LabelAnchorX;
            if (edge.LabelPosition != null) edge.LabelPosition = edge.LabelPosition.Transform(point => new ChartPoint(axis - point.X, point.Y));
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
