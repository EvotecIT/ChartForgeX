using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    // The existing marker painter's furthest local point is inside this 15-unit envelope.
    // Optional sizing/offset/stroke and the painter consume the same style in logical space.
    internal static double MarkerExtent(TopologyMarkerStyle? style, double axisStroke) => style == null ? 15 + axisStroke / 2 : style.Extent(axisStroke);
    internal static ChartRect? StyledMarkerBounds(TopologyChart chart, TopologyEdge edge, TopologyRenderOptions options,
        IReadOnlyDictionary<string, TopologyNode> nodes) {
        var units = TopologyPreparedEdgePlan.Units(edge);
        if (edge.SourceMarkerStyle == null && edge.TargetMarkerStyle == null) return null;
        var points = EdgePoints(chart, edge, nodes);
        if (points.Count < 2) return null;
        ChartRect? bounds = null;
        Include(edge.SourceMarkerStyle, RenderedSourceMarker(edge, options.IncludeDirectionMarkers), points[0]);
        Include(edge.TargetMarkerStyle, RenderedTargetMarker(edge, options.IncludeDirectionMarkers), points[points.Count - 1]);
        return bounds;
        void Include(TopologyMarkerStyle? style, TopologyMarkerKind kind, ChartPoint point) {
            if (style == null || kind == TopologyMarkerKind.None) return;
            var e = style == null ? MarkerExtent(null, chart.Theme.AxisStrokeWidth) * units : MarkerExtent(style, chart.Theme.AxisStrokeWidth * units);
            var b = new ChartRect(point.X - e, point.Y - e, 2 * e, 2 * e);
            if (!bounds.HasValue) bounds = b;
            else { var old = bounds.Value; var left = Math.Min(old.Left, b.Left); var top = Math.Min(old.Top, b.Top);
                bounds = new ChartRect(left, top, Math.Max(old.Right, b.Right) - left, Math.Max(old.Bottom, b.Bottom) - top); }
        }
    }
}
