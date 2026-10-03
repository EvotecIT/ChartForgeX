using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyDenseRoutePlanner {
    /// <summary>Returns the edge's unique painted branch, while callers retain the complete route for semantics.</summary>
    internal static List<ChartPoint> PaintPoints(TopologyChart chart, TopologyEdge edge, List<ChartPoint> fullRoute, bool includeOwner = false) {
        if (chart.RenderOptions?.ShareIncomingTrunks != true) return fullRoute;
        Plan(chart);
        return Plans.TryGetValue(chart, out var holder) && holder.PaintRoutes.TryGetValue(edge, out var branch) &&
            (includeOwner || !ReferenceEquals(holder.TrunkOwners[edge], edge)) ? new List<ChartPoint>(branch) : fullRoute;
    }

    /// <summary>SVG keeps one movable tail so an HTML host can focus or filter any member without losing the bus.</summary>
    internal static (TopologyEdge? Owner, List<ChartPoint>? Tail) SharedTrunk(TopologyChart chart, TopologyEdge edge) {
        if (chart.RenderOptions?.ShareIncomingTrunks != true) return (null, null);
        Plan(chart);
        if (!Plans.TryGetValue(chart, out var holder) || !holder.TrunkOwners.TryGetValue(edge, out var owner)) return (null, null);
        return (owner, holder.Routes![edge]!.Skip(holder.PaintRoutes[edge].Count - 1).ToList());
    }

    internal static bool IsTrunkBranch(TopologyChart chart, TopologyEdge edge) {
        if (chart.RenderOptions?.ShareIncomingTrunks != true) return false;
        Plan(chart);
        return Plans.TryGetValue(chart, out var holder) && holder.TrunkOwners.TryGetValue(edge, out var owner) && !ReferenceEquals(edge, owner);
    }

    /// <summary>Reports the intentional shared suffix, so diagnostics can distinguish a bus from accidental overlap.</summary>
    internal static double SharedTrunkLength(TopologyChart chart, TopologyEdge first, TopologyEdge second) {
        if (chart.RenderOptions?.ShareIncomingTrunks != true) return 0;
        Plan(chart);
        if (!Plans.TryGetValue(chart, out var holder) || !holder.TrunkOwners.TryGetValue(first, out var a) ||
            !holder.TrunkOwners.TryGetValue(second, out var b) || !ReferenceEquals(a, b)) return 0;
        var firstPoints = holder.Routes![first]!;
        var secondPoints = holder.Routes[second]!;
        var branch = ReferenceEquals(first, a) ? second : first;
        var junction = holder.PaintRoutes[branch].Last();
        var firstStart = firstPoints.FindIndex(point => Length(point, junction) < 0.01);
        var secondStart = secondPoints.FindIndex(point => Length(point, junction) < 0.01);
        return firstStart < 0 || secondStart < 0 ? 0 : Interaction(firstPoints.Skip(firstStart).ToList(), secondPoints.Skip(secondStart).ToList()).Shared;
    }

    private static void JoinIncomingTrunks(TopologyChart chart, Scene scene, List<PlannedRoute> routes, List<List<ChartPoint>> fixedRoutes,
        Dictionary<TopologyEdge, List<ChartPoint>> paintRoutes, Dictionary<TopologyEdge, TopologyEdge> trunkOwners) {
        var options = chart.RenderOptions!;
        // Live force controls replace routes as nodes move; their independently movable relationships keep full paths.
        if (options.EnableHtmlInteractions && options.EnableHtmlForceGraphControls &&
            chart.LayoutMode is TopologyLayoutMode.ForceDirected or TopologyLayoutMode.RelationshipRadial) return;
        var highlight = TopologyHighlightState.From(chart, options);
        var groups = routes.Where(route => TopologyRenderPrimitives.EffectiveEdgeDash(route.Request.Edge) == "none")
            .GroupBy(route => TrunkKey(route, options, highlight));
        foreach (var group in groups) {
            var members = group.OrderByDescending(route => Length(route.Points[route.Points.Count - 2], route.Points[route.Points.Count - 1]))
                .ThenBy(route => route.Request.Order).ToList();
            if (members.Count < 2) continue;
            var leader = members[0];
            var tail = leader.Points[leader.Points.Count - 1];
            var tailIndex = leader.Points.Count - 2;
            while (tailIndex > Math.Max(0, leader.Points.Count - 5) && Length(leader.Points[tailIndex], leader.Points[tailIndex + 1]) < 32) tailIndex--;
            var previous = leader.Points[tailIndex];
            var next = leader.Points[tailIndex + 1];
            var length = Length(previous, next);
            if (length < 32) continue;
            var trunkLength = Math.Min(80, length * 0.6);
            var junction = new ChartPoint(next.X + (previous.X - next.X) * trunkLength / length,
                next.Y + (previous.Y - next.Y) * trunkLength / length);
            var suffix = leader.Points.Skip(tailIndex + 1).ToArray();
            var sharedSuffix = new[] { junction }.Concat(suffix).ToList();
            var leaderPrefix = leader.Points.Take(tailIndex + 1).Concat(new[] { junction }).ToList();
            var outside = routes.Where(route => !members.Contains(route)).ToList();
            var joined = false;
            foreach (var branch in members.Skip(1)) {
                List<ChartPoint>? best = null;
                var before = Interaction(branch.Points, outside, fixedRoutes, null);
                // Keep the source port and its first leg. Join only a suffix, never reshape an authored named port.
                for (var index = 1; index < branch.Points.Count; index++) {
                    var start = branch.Points[index];
                    foreach (var bend in new[] { new ChartPoint(start.X, junction.Y), new ChartPoint(junction.X, start.Y) }) {
                        var prefix = branch.Points.Take(index + 1).Concat(new[] { bend, junction }).ToList();
                        prefix = Simplify(prefix);
                        if (prefix.Count < 2 || !SameFirstLeg(branch.Points, prefix)) continue;
                        var candidate = prefix.Concat(suffix).ToList();
                        var probe = new PlannedRoute(branch.Request, branch.Start, leader.End, candidate);
                        if (TouchesObstacle(scene, probe) || RouteLength(candidate) > RouteLength(branch.Points) * 1.3 + 96) continue;
                        var after = Interaction(candidate, outside, fixedRoutes, null);
                        if (after.Crossings > before.Crossings || after.Shared > before.Shared + 0.01) continue;
                        if (WorsensTrunkSiblings(branch, candidate, prefix, sharedSuffix, leader, leaderPrefix,
                            members, paintRoutes, trunkOwners)) continue;
                        if (best == null || RouteLength(candidate) < RouteLength(best)) best = candidate;
                    }
                }
                if (best == null) continue;
                branch.Points = best;
                var junctionIndex = best.FindIndex(point => Length(point, junction) < 0.01);
                paintRoutes[branch.Request.Edge] = best.Take(junctionIndex + 1).ToList();
                trunkOwners[branch.Request.Edge] = leader.Request.Edge;
                trunkOwners[leader.Request.Edge] = leader.Request.Edge;
                joined = true;
            }
            if (joined) {
                leader.Points.Insert(tailIndex + 1, junction);
                paintRoutes[leader.Request.Edge] = leader.Points.Take(tailIndex + 2).ToList();
            }
        }
    }

    private static bool WorsensTrunkSiblings(PlannedRoute branch, List<ChartPoint> candidate, List<ChartPoint> prefix,
        List<ChartPoint> suffix, PlannedRoute leader, List<ChartPoint> leaderPrefix, List<PlannedRoute> members,
        Dictionary<TopologyEdge, List<ChartPoint>> paintRoutes, Dictionary<TopologyEdge, TopologyEdge> trunkOwners) {
        foreach (var peer in members) {
            if (ReferenceEquals(peer, branch)) continue;
            var before = Interaction(branch.Points, peer.Points);
            var after = Interaction(candidate, peer.Points);
            if (ReferenceEquals(peer, leader) || trunkOwners.TryGetValue(peer.Request.Edge, out var owner) &&
                ReferenceEquals(owner, leader.Request.Edge)) {
                var peerPrefix = ReferenceEquals(peer, leader) ? leaderPrefix : paintRoutes[peer.Request.Edge];
                // Only the common suffix is intentional. Prefix crossings and prefix/tail overdraw still count.
                var prefixes = Interaction(prefix, peerPrefix);
                var branchTail = Interaction(prefix, suffix);
                var peerTail = Interaction(suffix, peerPrefix);
                after = (prefixes.Crossings + branchTail.Crossings + peerTail.Crossings,
                    prefixes.Shared + branchTail.Shared + peerTail.Shared);
            }
            if (after.Crossings > before.Crossings || after.Shared > before.Shared + 0.01) return true;
        }
        return false;
    }

    private static bool SameFirstLeg(IReadOnlyList<ChartPoint> original, IReadOnlyList<ChartPoint> candidate) =>
        Math.Sign(original[1].X - original[0].X) == Math.Sign(candidate[1].X - candidate[0].X) &&
        Math.Sign(original[1].Y - original[0].Y) == Math.Sign(candidate[1].Y - candidate[0].Y);

    private static string TrunkKey(PlannedRoute route, TopologyRenderOptions options, TopologyHighlightState highlight) {
        var edge = route.Request.Edge;
        return string.Join("|", edge.TargetNodeId.Length + ":" + edge.TargetNodeId, (int)edge.Kind, (int)edge.Status,
            (int)edge.Direction, (int)edge.Emphasis, edge.IsMuted, edge.Color?.Length + ":" + edge.Color, edge.StrokeWidth, edge.Opacity,
            (int?)edge.TargetMarker, (int)route.End.Side, edge.TargetPortId?.Length + ":" + edge.TargetPortId,
            options.SelectedEdgeIds.Contains(edge.Id), highlight.IsEdgeHighlighted(edge));
    }
}
