using System;
using System.Linq;
using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private void PrepareAuthoredEdgePaths() {
        ValidateDetailBadgeRoles();
        if (_options.EnableHtmlInteractions && _options.EnableHtmlForceGraphControls && _chart.Edges.Any(edge => edge.LabelPosition != null || edge.SourceMarkerStyle != null || edge.TargetMarkerStyle != null))
            throw new NotSupportedException("Explicit label and marker layouts cannot be rewritten by live force graph controls.");
        var paths = _chart.Edges.Where(edge => edge.PathGeometry != null).ToArray();
        if (paths.Length == 0) return;
        if (_options.EnableHtmlInteractions && _options.EnableHtmlForceGraphControls) throw new NotSupportedException("Authored edge paths cannot be rewritten by live force graph controls.");
        if (_chart.LayoutMode != TopologyLayoutMode.Manual) throw new NotSupportedException("Authored edge paths require Manual topology coordinates.");
        if (paths.Length + _chart.PathDecorations.Count > TopologyPathDecoration.MaximumPaths) throw new ArgumentException("Topology paths exceed 1024 paths.");
        var commands = _chart.PathDecorations.Sum(path => path?.Commands.Count ?? 0); var points = 0;
        foreach (var edge in paths) {
            if (edge.Waypoints.Count != 0 || edge.RouteLane != 0 || edge.HasRouteLaneOverride || edge.SourcePort != TopologyEdgePort.Auto || edge.TargetPort != TopologyEdgePort.Auto ||
                !string.IsNullOrWhiteSpace(edge.SourcePortId) || !string.IsNullOrWhiteSpace(edge.TargetPortId))
                throw new ArgumentException("Authored edge path '" + edge.Id + "' cannot also carry waypoints, route-lane or port overrides.");
            commands += edge.PathGeometry!.Commands.Count;
            if (commands > TopologyPathDecoration.MaximumTotalCommands) throw new ArgumentException("Topology paths exceed 65536 commands.");
            points += edge.PathGeometry.RoutePoints().Count;
            if (points > 65536) throw new ArgumentException("Authored edge paths exceed 65536 flattened route points.");
        }
    }
    private void ValidateDetailBadgeRoles() {
        foreach (var node in _chart.Nodes) if (node.Details.Any(detail => detail.BadgeStyle != null)) {
            var mode = TopologyRenderPrimitives.EffectiveNodeDisplayMode(node, _options);
            if (mode == TopologyNodeDisplayMode.Hidden || !_options.IncludeNodeLabels) continue;
            if (TopologyNodeShapeGeometry.IsActor(node) || mode is not (TopologyNodeDisplayMode.Card or TopologyNodeDisplayMode.CompactCard or TopologyNodeDisplayMode.Pill))
                throw new NotSupportedException("Detail badge surfaces require a native Card, CompactCard or Pill detail body.");
        }
    }
    private ChartRect IncludeAuthoredEdgePathBounds(ChartRect content) {
        foreach (var edge in _chart.Edges) {
            if (edge.PathGeometry == null) continue;
            var b = edge.PathGeometry.Capture().Bounds(); var half = (edge.StrokeWidth ?? _context.Theme.SeriesStrokeWidth) / 2;
            if (edge.Emphasis == TopologyEdgeEmphasis.Strong) half *= 1.6;
            if (_options.SelectedEdgeIds.Contains(edge.Id)) half *= 1.5;
            var left = Math.Min(content.Left, b.Left - half); var top = Math.Min(content.Top, b.Top - half);
            var right = Math.Max(content.Right, b.Right + half); var bottom = Math.Max(content.Bottom, b.Bottom + half);
            content = new ChartRect(left, top, right - left, bottom - top);
        }
        return content;
    }
}
