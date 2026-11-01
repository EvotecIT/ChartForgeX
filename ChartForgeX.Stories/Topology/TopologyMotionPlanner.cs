using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal static class TopologyMotionPlanner {
    public static TopologyMotionPlan? Build(TopologyChart chart, TopologyRenderOptions options, TopologyMotionOptions motion, IReadOnlyDictionary<TopologyEdge, IReadOnlyList<ChartPoint>>? resolvedRoutes = null) {
        if (chart.Edges.Count == 0) return null;
        motion.Validate();
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var explicitEdgeIds = motion.EdgeIds;
        if (explicitEdgeIds.Count > 0) {
            var entries = new List<TopologyMotionEntry>();
            var nodeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var edgeId in explicitEdgeIds) AddEdges(chart, nodes, entries, edgeId, resolvedRoutes);
            if (motion.PulseRouteEndpoints) AddEndpointNodeIds(entries, nodeIds);
            return entries.Count == 0 ? null : new TopologyMotionPlan(MotionSourceId(motion), null, entries, OrderedNodeIds(nodeIds));
        }

        foreach (var scenario in ResolveScenarios(chart, options, motion)) {
            var plan = BuildScenarioPlan(chart, nodes, scenario, resolvedRoutes);
            if (plan != null) return plan;
        }

        return null;
    }

    private static TopologyMotionPlan? BuildScenarioPlan(TopologyChart chart, IReadOnlyDictionary<string, TopologyNode> nodes, TopologyScenario scenario, IReadOnlyDictionary<TopologyEdge, IReadOnlyList<ChartPoint>>? resolvedRoutes) {
        var entries = new List<TopologyMotionEntry>();
        var nodeIds = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < scenario.Steps.Count; index++) {
            var step = scenario.Steps[index];
            if (step.Kind == TopologyScenarioStepKind.Edge) {
                var previousNode = index > 0 && scenario.Steps[index - 1].Kind == TopologyScenarioStepKind.Node
                    ? scenario.Steps[index - 1].Id : null;
                var followingNode = index + 1 < scenario.Steps.Count && scenario.Steps[index + 1].Kind == TopologyScenarioStepKind.Node
                    ? scenario.Steps[index + 1].Id : null;
                AddEdges(chart, nodes, entries, step.Id, resolvedRoutes, previousNode, followingNode,
                    step.Reverse || step.Interaction != TopologyScenarioInteraction.None ? step.Reverse : null);
            }
            else if (step.Kind == TopologyScenarioStepKind.Node) nodeIds.Add(step.Id);
        }

        return entries.Count == 0 ? null : new TopologyMotionPlan(scenario.Id, MotionSourceColor(scenario), entries, OrderedNodeIds(nodeIds));
    }

    private static void AddEdges(TopologyChart chart, IReadOnlyDictionary<string, TopologyNode> nodes, List<TopologyMotionEntry> entries, string edgeId,
        IReadOnlyDictionary<TopologyEdge, IReadOnlyList<ChartPoint>>? resolvedRoutes, string? previousNode = null, string? followingNode = null, bool? reverse = null) {
        foreach (var edge in chart.Edges.Where(candidate => string.Equals(candidate.Id, edgeId, StringComparison.Ordinal))) {
            if (!nodes.ContainsKey(edge.SourceNodeId) || !nodes.ContainsKey(edge.TargetNodeId)) continue;
            var routePoints = resolvedRoutes != null && resolvedRoutes.TryGetValue(edge, out var resolved) ? resolved : RenderedEdgeSamplePoints(chart, edge, nodes, EdgePoints(chart, edge, nodes));
            if (routePoints.Count < 2) continue;
            if (reverse ?? TopologyScenarioTraversal.ByNeighbours(edge, previousNode, followingNode)) {
                var reversed = routePoints.ToArray();
                Array.Reverse(reversed);
                routePoints = reversed;
            }
            entries.Add(new TopologyMotionEntry(edge, routePoints, PolylineLength(routePoints)));
        }
    }

    private static void AddEndpointNodeIds(IEnumerable<TopologyMotionEntry> entries, HashSet<string> nodeIds) {
        foreach (var entry in entries) {
            nodeIds.Add(entry.Edge.SourceNodeId);
            nodeIds.Add(entry.Edge.TargetNodeId);
        }
    }

    private static string[] OrderedNodeIds(HashSet<string> nodeIds) =>
        nodeIds.OrderBy(id => id, StringComparer.Ordinal).ToArray();

    private static IEnumerable<TopologyScenario> ResolveScenarios(TopologyChart chart, TopologyRenderOptions options, TopologyMotionOptions motion) {
        var id = motion.ScenarioId;
        if (!string.IsNullOrWhiteSpace(id)) {
            var scenarioId = id!.Trim();
            var scenario = FindScenario(chart, scenarioId);
            if (scenario == null) throw new ArgumentException("Topology motion scenario id '" + scenarioId + "' does not match any scenario.", nameof(motion));
            yield return scenario;
            yield break;
        }

        id = options.ActiveScenarioId;
        TopologyScenario? activeScenario = null;
        if (!string.IsNullOrWhiteSpace(id)) {
            activeScenario = FindScenario(chart, id!.Trim());
            if (activeScenario != null) yield return activeScenario;
        }

        foreach (var scenario in chart.Scenarios) {
            if (activeScenario != null && string.Equals(scenario.Id, activeScenario.Id, StringComparison.Ordinal)) continue;
            yield return scenario;
        }
    }

    private static TopologyScenario? FindScenario(TopologyChart chart, string scenarioId) =>
        chart.Scenarios.FirstOrDefault(candidate => string.Equals(candidate.Id, scenarioId, StringComparison.Ordinal));

    private static string MotionSourceId(TopologyMotionOptions motion) =>
        string.IsNullOrWhiteSpace(motion.ScenarioId) ? "explicit-edges" : motion.ScenarioId!.Trim();

    private static string? MotionSourceColor(TopologyScenario? scenario) =>
        string.IsNullOrWhiteSpace(scenario?.Color) ? null : scenario!.Color!.Trim();

    private static double PolylineLength(IReadOnlyList<ChartPoint> points) {
        var total = 0.0;
        for (var i = 1; i < points.Count; i++) total += Distance(points[i - 1], points[i]);
        return Math.Max(0.0001, total);
    }

    private static double Distance(ChartPoint first, ChartPoint second) {
        var dx = second.X - first.X;
        var dy = second.Y - first.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

}

internal sealed class TopologyMotionPlan {
    public TopologyMotionPlan(string sourceId, string? color, IReadOnlyList<TopologyMotionEntry> entries, IReadOnlyList<string> nodeIds) {
        SourceId = sourceId;
        Color = color;
        Entries = entries;
        NodeIds = nodeIds;
        TotalLength = entries.Sum(entry => entry.Length);
    }

    public string SourceId { get; }
    public string? Color { get; }
    public IReadOnlyList<TopologyMotionEntry> Entries { get; }
    public IReadOnlyList<string> NodeIds { get; }
    public double TotalLength { get; }
}

internal sealed class TopologyMotionEntry {
    public TopologyMotionEntry(TopologyEdge edge, IReadOnlyList<ChartPoint> points, double length) {
        Edge = edge;
        Points = points;
        Length = length;
    }

    public TopologyEdge Edge { get; }
    public IReadOnlyList<ChartPoint> Points { get; }
    public double Length { get; }
}
