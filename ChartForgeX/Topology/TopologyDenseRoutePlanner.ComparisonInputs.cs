using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyDenseRoutePlanner {
    private static List<Request> CreateRequests(TopologyChart chart, Scene scene, List<TopologyEdge> edges,
        Dictionary<string, TopologyNode> nodes) {
        var requests = new List<Request>(edges.Count);
        for (var i = 0; i < edges.Count; i++) {
            var edge = edges[i];
            var source = nodes[edge.SourceNodeId];
            var target = nodes[edge.TargetNodeId];
            var starts = Terminals(chart, scene, source, target, edge.SourcePort, edge.SourcePortId, (edge.LayoutInference & TopologyEdgeLayoutInference.SourcePort) != 0);
            var ends = Terminals(chart, scene, target, source, edge.TargetPort, edge.TargetPortId, (edge.LayoutInference & TopologyEdgeLayoutInference.TargetPort) != 0);
            if (starts.Count == 0 || ends.Count == 0) continue;
            var distance = Math.Abs(Center(source).X - Center(target).X) + Math.Abs(Center(source).Y - Center(target).Y);
            requests.Add(new Request(edge, i, source, target, starts, ends, distance));
        }

        return requests;
    }

    /// <summary>Captures exact actual obstacle, terminal, fixed-route and trunk inputs without invoking route search.</summary>
    internal static object? CaptureComparisonInputs(TopologyChart chart, bool recordedInputs = false) {
        var scene = Scene.Create(chart); if (scene == null) return null;
        var nodes = TopologyGroupEndpoints.Resolve(chart).ToDictionary(pair => pair.Key, pair => pair.Value);
        var edges = chart.Edges.Where(edge => Handles(chart, edge) && nodes.ContainsKey(edge.SourceNodeId) && nodes.ContainsKey(edge.TargetNodeId)).ToList();
        var requests = CreateRequests(chart, scene, edges, nodes);
        return PlanKey.Create(chart, scene, edges, requests, FixedRoutes(chart, nodes), recordedInputs);
    }
}
