using System;
using System.Linq;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void TopologyAlignmentReviewContracts() {
        var chart = TopologyChart.Create().WithTitle("").WithViewport(900, 700).WithLayout(TopologyLayoutMode.Manual)
            .AddNode("source", "Source", 160, 80, width: 150, height: 72)
            .AddNode("obstacle", "Audit", 160, 168, width: 150, height: 72)
            .AddNode("target", "Bus", 206, 256, width: 58, height: 44)
            .AddEdge("handoff", "source", "target", routing: TopologyEdgeRouting.Orthogonal);
        foreach (var node in chart.Nodes) { node.DisplayMode = TopologyNodeDisplayMode.Card; node.PreserveDisplayModeSize = true; }
        chart.Edges[0].SourcePort = TopologyEdgePort.Bottom; chart.Edges[0].TargetPort = TopologyEdgePort.Top;
        chart.Alignments.Add(new TopologyAlignment("column", TopologyAlignmentKind.Column, new[] { "source", "obstacle", "target" }));
        var saved = chart.ToVisualArtifact().ToInterchangeEnvelope(); Clear(saved);
        var restored = VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToTopologyChart(); Clear(restored.ToVisualArtifact().ToInterchangeEnvelope());
        PackageAssertions.Require(restored.Edges[0].Routing == TopologyEdgeRouting.Orthogonal && restored.Edges[0].SourcePort == TopologyEdgePort.Bottom
            && restored.Edges[0].TargetPort == TopologyEdgePort.Top, "Packed body-clear route changed authored routing or ports.");
        var nested = TopologyChart.Create().WithTitle("").WithLayout(TopologyLayoutMode.Layered, TopologyLayoutDirection.TopToBottom)
            .AddAutoGroup("outer", "Outer").AddAutoGroup("inner", "Inner").AddAutoGroup("first", "First").AddAutoGroup("second", "Second")
            .AddAutoNode("a", "A", groupId: "first").AddAutoNode("b", "B", groupId: "second");
        nested.Groups.Single(group => group.Id == "outer").LayoutDirection = TopologyLayoutDirection.LeftToRight;
        nested.Groups.Single(group => group.Id == "inner").ParentGroupId = "outer";
        foreach (var group in nested.Groups.Where(group => group.Id == "first" || group.Id == "second")) { group.ParentGroupId = "inner"; group.Presentation = TopologyGroupPresentation.Compartment; }
        nested.Alignments.Add(new TopologyAlignment("column", TopologyAlignmentKind.Column, new[] { "a", "b" }));
        var prepared = nested.ToVisualArtifact().ToInterchangeEnvelope(); var a = prepared.Nodes.Single(node => node.Id == "a"); var b = prepared.Nodes.Single(node => node.Id == "b");
        PackageAssertions.Require(Math.Abs(a.X!.Value + a.Width!.Value / 2 - b.X!.Value - b.Width!.Value / 2) < .000001 && a.Y!.Value + a.Height!.Value < b.Y!.Value,
            "Packed inherited compartment direction produced a false alignment conflict or lost global column/order.");
    }

    private static void Clear(VisualArtifactInterchangeEnvelope saved) {
        var edge = saved.Edges.Single(); var body = saved.Nodes.Single(node => node.Id == "obstacle");
        PackageAssertions.Require(edge.ResolvedRoute.Count >= 2, "Packed body-clear edge omitted its resolved route.");
        var left = body.X!.Value + .000001; var right = left + body.Width!.Value - .000002;
        var top = body.Y!.Value + .000001; var bottom = top + body.Height!.Value - .000002;
        for (var i = 1; i < edge.ResolvedRoute.Count; i++) {
            var a = edge.ResolvedRoute[i - 1]; var b = edge.ResolvedRoute[i]; var enter = 0d; var exit = 1d;
            PackageAssertions.Require(!(Clip(-(b.X - a.X), a.X - left) && Clip(b.X - a.X, right - a.X)
                && Clip(-(b.Y - a.Y), a.Y - top) && Clip(b.Y - a.Y, bottom - a.Y)), "Packed aligned route crosses the real intervening body.");
            bool Clip(double direction, double distance) {
                if (Math.Abs(direction) < .000000001) return distance >= 0;
                var ratio = distance / direction;
                if (direction < 0) { if (ratio > exit) return false; enter = Math.Max(enter, ratio); }
                else { if (ratio < enter) return false; exit = Math.Min(exit, ratio); }
                return true;
            }
        }
    }
}
