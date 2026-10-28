using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    /// <summary>Uses the actual planner's branch and owner tail even during canonical layout measurement.</summary>
    /// <remarks>Only the final compiler scope caches a path. Earlier mutating layout passes recompute from their
    /// current terminal inputs; they never interpolate a full control polyline that differs from body/tail paint.</remarks>
    private static ChartPath InterpolatedOrdinaryPath(TopologyChart chart, TopologyEdge edge, IReadOnlyList<ChartPoint> resolved) {
        var captured = TopologyRenderRouteCache.FullPath(chart, edge); if (captured != null) return captured;
        if (!TopologyDenseRoutePlanner.Handles(chart, edge))
            return TopologyInterpolationGeometry.Build(resolved, edge.Interpolation!.Value, edge.InterpolationCornerBlendLength, TopologyPreparedEdgePlan.Units(edge));
        var body = TopologyDenseRoutePlanner.PaintPoints(chart, edge, resolved.ToList(), includeOwner: true);
        var trunk = TopologyDenseRoutePlanner.SharedTrunk(chart, edge);
        var path = TopologyInterpolationGeometry.Build(body, edge.Interpolation!.Value, edge.InterpolationCornerBlendLength, TopologyPreparedEdgePlan.Units(edge));
        if (trunk.Owner == null) return path;
        var tail = TopologyDenseRoutePlanner.SharedTrunk(chart, trunk.Owner).Tail!;
        var ownerTail = TopologyInterpolationGeometry.Build(tail, trunk.Owner.Interpolation!.Value,
            trunk.Owner.InterpolationCornerBlendLength, TopologyPreparedEdgePlan.Units(trunk.Owner));
        return TopologyInterpolationGeometry.JoinBodyAndTail(path, ownerTail);
    }
}
