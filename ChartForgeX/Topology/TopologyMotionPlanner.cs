using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal static class TopologyMotionPlanner {
    public static TopologyMotionPlan? Build(TopologyChart chart, TopologyRenderOptions options) {
        if (options.Motion == null || chart.Edges.Count == 0) return null;
        options.Motion.Validate();
        var scenario = ResolveScenario(chart, options);
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var entries = new List<TopologyMotionEntry>();
        var nodeIds = new HashSet<string>(StringComparer.Ordinal);
        var explicitEdgeIds = options.Motion.EdgeIds;
        if (explicitEdgeIds.Count > 0) {
            foreach (var edgeId in explicitEdgeIds) AddEdges(chart, nodes, entries, edgeId);
            if (options.Motion.PulseRouteEndpoints) AddEndpointNodeIds(entries, nodeIds);
            return entries.Count == 0 ? null : new TopologyMotionPlan(MotionSourceId(options), MotionSourceColor(scenario), entries, nodeIds.ToArray());
        }

        if (scenario == null) return null;
        foreach (var step in scenario.Steps) {
            if (step.Kind == TopologyScenarioStepKind.Edge) AddEdges(chart, nodes, entries, step.Id);
            else if (step.Kind == TopologyScenarioStepKind.Node) nodeIds.Add(step.Id);
        }

        return entries.Count == 0 ? null : new TopologyMotionPlan(scenario.Id, MotionSourceColor(scenario), entries, nodeIds.ToArray());
    }

    public static TopologyMotionSample Sample(TopologyMotionPlan plan, TopologyRenderOptions options, TopologyTheme theme) {
        var motion = options.Motion!;
        var progress = Clamp(motion.Progress, 0, 1);
        var targetDistance = progress >= 1 ? plan.TotalLength : plan.TotalLength * progress;
        var walked = 0.0;
        foreach (var entry in plan.Entries) {
            if (walked + entry.Length >= targetDistance || ReferenceEquals(entry, plan.Entries[plan.Entries.Count - 1])) {
                var localDistance = Math.Max(0, targetDistance - walked);
                var point = PointAtDistance(entry.Points, localDistance);
                var color = MotionColor(entry.Edge, plan, options, theme);
                return new TopologyMotionSample(point, color);
            }

            walked += entry.Length;
        }

        var first = plan.Entries[0];
        return new TopologyMotionSample(first.Points[0], MotionColor(first.Edge, plan, options, theme));
    }

    private static void AddEdges(TopologyChart chart, IReadOnlyDictionary<string, TopologyNode> nodes, List<TopologyMotionEntry> entries, string edgeId) {
        foreach (var edge in chart.Edges.Where(candidate => string.Equals(candidate.Id, edgeId, StringComparison.Ordinal))) {
            if (!nodes.ContainsKey(edge.SourceNodeId) || !nodes.ContainsKey(edge.TargetNodeId)) continue;
            var points = EdgePoints(chart, edge, nodes);
            var routePoints = IsGeographicCurve(chart, edge, nodes)
                ? GeographicCurveSamplePoints(chart, edge, nodes, points)
                : points;
            if (routePoints.Count < 2) continue;
            entries.Add(new TopologyMotionEntry(edge, routePoints, PolylineLength(routePoints)));
        }
    }

    private static void AddEndpointNodeIds(IEnumerable<TopologyMotionEntry> entries, HashSet<string> nodeIds) {
        foreach (var entry in entries) {
            nodeIds.Add(entry.Edge.SourceNodeId);
            nodeIds.Add(entry.Edge.TargetNodeId);
        }
    }

    private static TopologyScenario? ResolveScenario(TopologyChart chart, TopologyRenderOptions options) {
        var id = options.Motion?.ScenarioId;
        if (string.IsNullOrWhiteSpace(id)) id = options.ActiveScenarioId;
        if (!string.IsNullOrWhiteSpace(id)) {
            return chart.Scenarios.FirstOrDefault(candidate => string.Equals(candidate.Id, id, StringComparison.Ordinal));
        }

        return chart.Scenarios.Count == 0 ? null : chart.Scenarios[0];
    }

    private static string MotionSourceId(TopologyRenderOptions options) =>
        string.IsNullOrWhiteSpace(options.Motion?.ScenarioId) ? "explicit-edges" : options.Motion!.ScenarioId!.Trim();

    private static string? MotionSourceColor(TopologyScenario? scenario) =>
        string.IsNullOrWhiteSpace(scenario?.Color) ? null : scenario!.Color!.Trim();

    private static string MotionColor(TopologyEdge edge, TopologyMotionPlan plan, TopologyRenderOptions options, TopologyTheme theme) {
        if (!string.IsNullOrWhiteSpace(options.Motion?.MarkerColor)) return options.Motion!.MarkerColor!.Trim();
        if (!string.IsNullOrWhiteSpace(plan.Color)) return plan.Color!;
        if (!string.IsNullOrWhiteSpace(edge.Color)) return edge.Color!.Trim();
        return theme.StatusColor(edge.Status);
    }

    private static double PolylineLength(IReadOnlyList<ChartPoint> points) {
        var total = 0.0;
        for (var i = 1; i < points.Count; i++) total += Distance(points[i - 1], points[i]);
        return Math.Max(0.0001, total);
    }

    private static ChartPoint PointAtDistance(IReadOnlyList<ChartPoint> points, double distance) {
        if (points.Count == 0) return new ChartPoint(0, 0);
        if (points.Count == 1) return points[0];
        var walked = 0.0;
        for (var i = 1; i < points.Count; i++) {
            var start = points[i - 1];
            var end = points[i];
            var segment = Distance(start, end);
            if (walked + segment >= distance) {
                var t = segment <= 0.0001 ? 0 : (distance - walked) / segment;
                return new ChartPoint(start.X + (end.X - start.X) * t, start.Y + (end.Y - start.Y) * t);
            }

            walked += segment;
        }

        return points[points.Count - 1];
    }

    private static double Distance(ChartPoint first, ChartPoint second) {
        var dx = second.X - first.X;
        var dy = second.Y - first.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static double Clamp(double value, double minimum, double maximum) =>
        value < minimum ? minimum : value > maximum ? maximum : value;
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

internal readonly struct TopologyMotionSample {
    public TopologyMotionSample(ChartPoint point, string color) {
        Point = point;
        Color = color;
    }

    public ChartPoint Point { get; }
    public string Color { get; }
}
