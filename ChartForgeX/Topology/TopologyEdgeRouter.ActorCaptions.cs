using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyEdgeRouter {
    /// <summary>Uses the existing finite corridor fallback with actual extra caption obstacles, without changing the authored edge policy.</summary>
    internal static TopologyRoutePlan RouteAroundActorCaptions(TopologyChart chart, TopologyEdge edge, TopologyNode source,
        TopologyNode target, double routeLane, IReadOnlyList<ChartRect> captions) {
        var policy = TopologyLayoutEngine.Clone(edge);
        policy.Routing = TopologyEdgeRouting.ObstacleAvoidingOrthogonal;
        // The authored edge is still Straight/Orthogonal in chart.Edges. Looking up a dense plan for the temporary
        // clone can recurse through the same fixed-route snapshot on mixed graphs; use this owner's corridor fallback.
        return RouteCore(chart, policy, source, target, routeLane, edge, false, captions);
    }
}
