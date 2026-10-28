using System;
using System.Linq;
using ChartForgeX.Primitives;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private (ChartRect Bounds, double FontSize) ResolvedEndpointLabel(TopologyEdge edge, ChartPoint endpoint, ChartPoint adjacent, string text, bool source) {
        var geometry = _numericRoutes.TryGetValue(edge, out var numeric) ? numeric : null;
        var captured = source ? geometry?.SourceLabel : geometry?.TargetLabel;
        return captured == null ? EdgeEndpointLabelLayout(_chart, _options, endpoint, adjacent, text, TopologyPreparedEdgePlan.Units(edge))
            : (captured.Bounds, captured.FontSize);
    }
    private TopologyPreparedEdgeGeometry? CaptureNumericGeometry(TopologyEdge edge) {
        if (!_numericRoutes.TryGetValue(edge, out var geometry)) {
            // These are the actual final sampled paint/hit routes. Do not interpolate or round them again.
            if (edge.PathGeometry != null || edge.Waypoints.Count != 0 || _scale == 1 && _offsetX == 0 && _offsetY == 0
                || _source.LayoutMode == TopologyLayoutMode.Manual && !_options.FitContentToViewport) return null;
            var hasTrunk = _trunks.TryGetValue(edge, out var trunk);
            var body = Samples(_paintRoutes[edge]);
            var ownerTail = hasTrunk ? Samples(_trunks[trunk.Owner].Tail) : null;
            var fullPaint = ownerTail == null ? body : TopologyInterpolationGeometry.JoinBodyAndTail(body, ownerTail);
            geometry = new TopologyPreparedEdgeGeometry(null, null, _routes[edge], fullPaint,
                hasTrunk ? body : null, hasTrunk && ReferenceEquals(trunk.Owner, edge) ? ownerTail : null,
                _chart.Edges.IndexOf(edge), hasTrunk ? _chart.Edges.IndexOf(trunk.Owner) : (int?)null);
        }
        static ChartForgeX.Rendering.ChartPath Samples(System.Collections.Generic.IReadOnlyList<ChartPoint> points) =>
            ChartForgeX.Rendering.ChartPathBuilder.FromPoints(points);
        TopologyPreparedMainLabel? main = null;
        var layout = _edgeLabels.FirstOrDefault(item => ReferenceEquals(item.Edge, edge));
        if (layout != null) main = new TopologyPreparedMainLabel(new ChartRect(layout.CenterX - layout.Width / 2,
            layout.CenterY - layout.Height / 2, layout.Width, layout.Height), new ChartPoint(layout.AnchorX, layout.AnchorY));
        TopologyPreparedEndpointLabel? Endpoint(string? text, bool source) {
            if (!_options.IncludeEndpointLabels || string.IsNullOrWhiteSpace(text)) return null;
            var command = source ? geometry.FullPath.Commands[0] : geometry.FullPath.Commands[geometry.FullPath.Commands.Count - 1];
            var resolved = ResolvedEndpointLabel(edge, new ChartPoint(command.X, command.Y), geometry.Interpolation.HasValue ? geometry.FullPath.AdjacentToEndpoint(source)
                : source ? geometry.Input[1] : geometry.Input[geometry.Input.Count - 2], text!, source);
            return new TopologyPreparedEndpointLabel(resolved.Bounds, resolved.FontSize);
        }
        return geometry.WithCaptions(main, Endpoint(edge.SourceLabel, true), Endpoint(edge.TargetLabel, false)).Transform(Point, _scale);
    }
}
