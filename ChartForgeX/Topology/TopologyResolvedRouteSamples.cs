using System.Collections.Generic;
using ChartForgeX.Primitives;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

/// <summary>Samples the same static route contour for scene rendering and optional geometry observations.</summary>
internal static class TopologyResolvedRouteSamples {
    internal static IReadOnlyList<ChartPoint> Sample(TopologyChart chart, TopologyRenderOptions options,
        TopologyEdge edge, IReadOnlyDictionary<string, TopologyNode> nodes, List<ChartPoint> points) {
        var rendered = RenderedEdgeSamplePoints(chart, edge, nodes, points, 64);
        return ShouldRoundEdgeCorners(edge, rendered, options)
            ? RoundedOrthogonalRoutePoints(rendered, options.EdgeCornerRadius) : rendered;
    }
}
