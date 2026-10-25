using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyAlignmentReviewCausalTests {
    [Fact]
    public void MaintainedAlignedArchitectureRouteClearsTheActualInterveningAuditBody() {
        var source = File.ReadAllText(Path.Combine(TestRepository.Root, "ChartForgeX.Tests", "Fixtures", "Mermaid", "architecture-ordered-alignments.mmd"));
        var rendered = MermaidRenderer.Render(source, MermaidArchitectureNativeDepthTests.Options(false, 1100));
        Assert.Empty(rendered.Diagnostics); var artifact = rendered.Artifact!;
        var saved = artifact.ToInterchangeEnvelope(); Capture(artifact, "alignment-review-route-causal");
        var edge = saved.Edges.Single(item => item.Id == "mermaid-architecture-edge-2");
        var audit = saved.Nodes.Single(item => item.Id == "audit");
        Assert.Equal("queue", edge.SourceId); Assert.Equal("bus", edge.TargetId);
        Assert.False(CrossesBody(edge, audit), "Queue-to-bus route crosses the unrelated Audit body.");
    }

    [Fact]
    public void NestedCompartmentsInheritNearestAncestorDirectionBeforeApplyingGlobalColumn() {
        var chart = CompartmentChart(TopologyLayoutDirection.TopToBottom, TopologyLayoutDirection.LeftToRight, TopologyAlignmentKind.Column);
        CaptureInput(chart, "alignment-review-inherited-direction-causal");
        PreparedTopology? prepared = null;
        var exception = Record.Exception(() => prepared = chart.Prepare(new TopologyRenderOptions { IncludeLegend = false, IncludeTitle = false }));
        if (prepared != null) Capture(prepared, "alignment-review-inherited-direction-causal");
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output)
            File.WriteAllText(Path.Combine(output, "alignment-review-inherited-direction-causal.exception.txt"), exception?.ToString() ?? "none");
        Assert.Null(exception); Assert.NotNull(prepared);
        var saved = prepared!.ToInterchangeEnvelope(); var first = saved.Groups.Single(item => item.Id == "first"); var second = saved.Groups.Single(item => item.Id == "second");
        Assert.True(first.Y!.Value + first.Height!.Value < second.Y!.Value); Assert.Equal(first.X, second.X); Assert.Equal(first.Width, second.Width);
        var a = saved.Nodes.Single(item => item.Id == "a"); var b = saved.Nodes.Single(item => item.Id == "b");
        Assert.Equal(a.X!.Value + a.Width!.Value / 2, b.X!.Value + b.Width!.Value / 2, 6);
        Assert.Equal("inner", first.Topology!.ParentGroupId); Assert.Equal("outer", saved.Groups.Single(item => item.Id == "inner").Topology!.ParentGroupId);
    }

    internal static TopologyChart CompartmentChart(TopologyLayoutDirection global, TopologyLayoutDirection inherited, TopologyAlignmentKind kind) {
        var chart = TopologyChart.Create().WithTitle("").WithViewport(900, 700).WithLayout(TopologyLayoutMode.Layered, global)
            .AddAutoGroup("outer", "Outer").AddAutoGroup("inner", "Inner")
            .AddAutoGroup("first", "First").AddAutoGroup("second", "Second")
            .AddAutoNode("a", "A", groupId: "first", width: 140, height: 60).AddAutoNode("b", "B", groupId: "second", width: 140, height: 60);
        chart.Groups.Single(group => group.Id == "outer").LayoutDirection = inherited;
        chart.Groups.Single(group => group.Id == "inner").ParentGroupId = "outer";
        foreach (var group in chart.Groups.Where(group => group.Id is "first" or "second")) { group.ParentGroupId = "inner"; group.Presentation = TopologyGroupPresentation.Compartment; }
        chart.Alignments.Add(new TopologyAlignment("sequence", kind, new[] { "a", "b" })); return chart;
    }

    internal static bool CrossesBody(VisualArtifactInterchangeEdge edge, VisualArtifactInterchangeNode node) {
        var left = node.X!.Value + .000001; var right = node.X!.Value + node.Width!.Value - .000001;
        var top = node.Y!.Value + .000001; var bottom = node.Y!.Value + node.Height!.Value - .000001;
        for (var i = 1; i < edge.ResolvedRoute.Count; i++) {
            var a = edge.ResolvedRoute[i - 1]; var b = edge.ResolvedRoute[i]; var enter = 0d; var exit = 1d;
            if (Clip(-(b.X - a.X), a.X - left) && Clip(b.X - a.X, right - a.X)
                && Clip(-(b.Y - a.Y), a.Y - top) && Clip(b.Y - a.Y, bottom - a.Y)) return true;
            bool Clip(double direction, double distance) {
                if (Math.Abs(direction) < .000000001) return distance >= 0;
                var ratio = distance / direction;
                if (direction < 0) { if (ratio > exit) return false; enter = Math.Max(enter, ratio); }
                else { if (ratio < enter) return false; exit = Math.Min(exit, ratio); }
                return true;
            }
        }
        return false;
    }

    internal static void Capture(VisualArtifact artifact, string name) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); var path = Path.Combine(output, name); File.WriteAllText(path + ".svg", artifact.ToSvg());
        File.WriteAllBytes(path + ".native.png", artifact.ToPng()); File.WriteAllText(path + ".native.json", artifact.ToInterchangeJson());
    }
    private static void Capture(PreparedTopology prepared, string name) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); var path = Path.Combine(output, name); File.WriteAllText(path + ".svg", prepared.ToSvg());
        File.WriteAllBytes(path + ".native.png", prepared.ToPng()); File.WriteAllText(path + ".native.json", prepared.ToInterchangeEnvelope().ToJson());
    }
    private static void CaptureInput(TopologyChart chart, string name) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, name + ".input.json"), JsonSerializer.Serialize(new {
            chart.LayoutDirection, groups = chart.Groups.Select(group => new { group.Id, group.ParentGroupId, group.Presentation, group.LayoutDirection }),
            nodes = chart.Nodes.Select(node => new { node.Id, node.GroupId }), alignments = chart.Alignments.Select(item => new { item.Id, item.Kind, item.NodeIds }) }));
    }
}
