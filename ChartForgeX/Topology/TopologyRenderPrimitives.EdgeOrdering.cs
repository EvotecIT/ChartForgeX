using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    public static List<(TopologyEdge Edge, int RenderOrder)> OrderedEdgesForRendering(TopologyChart chart, TopologyRenderOptions options) {
        return chart.Edges
            .Select((edge, index) => new { Edge = edge, Index = index, Priority = EdgeRenderPriority(edge, options.SelectedEdgeIds.Contains(edge.Id)) })
            .OrderBy(item => item.Priority)
            .ThenBy(item => item.Index)
            .Select((item, renderOrder) => (item.Edge, renderOrder))
            .ToList();
    }

    public static List<(TopologyEdgeLabelLayout Layout, int RenderOrder)> OrderedEdgeLabelsForRendering(TopologyChart chart, TopologyRenderOptions options) {
        var edgeOrders = OrderedEdgesForRendering(chart, options).ToDictionary(item => item.Edge.Id, item => item.RenderOrder, System.StringComparer.Ordinal);
        return EdgeLabelLayouts(chart, options)
            .Select(layout => new { Layout = layout, RenderOrder = edgeOrders.TryGetValue(layout.Edge.Id, out var order) ? order : 0 })
            .OrderBy(item => item.RenderOrder)
            .Select(item => (item.Layout, item.RenderOrder))
            .ToList();
    }

    public static List<TopologyEdge> OrderedEdgesForLabelPlacement(TopologyChart chart, TopologyRenderOptions options) {
        return chart.Edges
            .Select((edge, index) => new { Edge = edge, Index = index, Priority = EdgeRenderPriority(edge, options.SelectedEdgeIds.Contains(edge.Id)) })
            .OrderByDescending(item => item.Priority)
            .ThenBy(item => item.Index)
            .Select(item => item.Edge)
            .ToList();
    }

    private static int EdgeRenderPriority(TopologyEdge edge, bool selected) {
        var score = 0;
        if (edge.Kind == TopologyEdgeKind.Dependency) score -= 10;
        if (edge.Kind is TopologyEdgeKind.Link or TopologyEdgeKind.Connectivity) score += 4;
        if (edge.IsMuted) score -= 18;
        score += edge.Emphasis switch {
            TopologyEdgeEmphasis.Subtle => -12,
            TopologyEdgeEmphasis.Strong => 18,
            _ => 0
        };
        score += edge.Status switch {
            TopologyHealthStatus.Critical => 14,
            TopologyHealthStatus.Warning => 8,
            TopologyHealthStatus.Healthy => 3,
            TopologyHealthStatus.Disabled => -4,
            _ => 0
        };
        if (selected) score += 100;
        return score;
    }
}
