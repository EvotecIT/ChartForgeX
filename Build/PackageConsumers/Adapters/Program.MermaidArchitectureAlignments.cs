using System;
using System.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void MermaidArchitectureAlignmentContracts() {
        const string source = "architecture-beta\nalign row a b\ngroup outer[Outer]\ngroup inner[Inner] in outer\nservice c[C] in outer\nservice b[B] in inner\nservice a[A] in inner\nalign column b c";
        var direct = RenderFixture(source);
        PackageAssertions.Require(!direct.HasErrors && direct.Diagnostics.Count == 0 && direct.Artifact != null, "Packed ordered architecture source was rejected.");
        var artifact = direct.Artifact ?? throw new InvalidOperationException("Packed aligned architecture artifact was omitted.");
        var document = direct.Document as MermaidArchitectureDocument ?? throw new InvalidOperationException("Packed aligned architecture AST was omitted.");
        PackageAssertions.Require(document.SourceText == source && document.Alignments.Count == 2 && document.Alignments[0].Span.Line == 2
            && document.Alignments[0].Members.SequenceEqual(new[] { "a", "b" }), "Packed alignment occurrence/member/span facts were lost.");
        var saved = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson());
        PackageAssertions.Require(saved.Topology != null && saved.Topology.Alignments.Count == 2, "Packed alignment portable owner was omitted.");
        var a = saved.Nodes.Single(node => node.Id == "a"); var b = saved.Nodes.Single(node => node.Id == "b"); var c = saved.Nodes.Single(node => node.Id == "c");
        PackageAssertions.Require(Math.Abs(a.Y!.Value + a.Height!.Value / 2 - b.Y!.Value - b.Height!.Value / 2) < .000001 && a.X!.Value + a.Width!.Value < b.X!.Value,
            "Packed row lost its global body-center equality or authored order.");
        PackageAssertions.Require(Math.Abs(b.X!.Value + b.Width!.Value / 2 - c.X!.Value - c.Width!.Value / 2) < .000001 && b.Y!.Value + b.Height!.Value < c.Y!.Value,
            "Packed column lost its global body-center equality or authored order.");
        var restored = saved.ToTopologyChart(); PackageAssertions.Require(restored.LayoutMode == TopologyLayoutMode.Manual && restored.Alignments.Count == 2
            && restored.Alignments[0].NodeIds.SequenceEqual(new[] { "a", "b" }), "Packed typed import lost alignment semantics.");
        var after = restored.ToVisualArtifact().ToInterchangeEnvelope(); PackageAssertions.Require(saved.Nodes.Select(node => (node.Id, node.X, node.Y)).SequenceEqual(after.Nodes.Select(node => (node.Id, node.X, node.Y))),
            "Packed aligned Manual import moved saved scene coordinates.");
        var fence = CreateFixtureParser().Parse("```mermaid\n" + source + "\n```");
        PackageAssertions.Require(!fence.HasErrors && fence.Artifacts.Count == 1 && fence.Artifacts[0].ToSvg() == artifact.ToSvg(), "Packed aligned fence did not use the native scene owner.");
        PackageAssertions.Png(artifact.ToPng());
        var native = TopologyChart.Create().WithLayout(TopologyLayoutMode.Layered).AddAutoNode("first", "First").AddAutoNode("second", "Second");
        native.Alignments.Add(new TopologyAlignment("request", TopologyAlignmentKind.Row, new[] { "first", "second" }));
        var portable = native.ToVisualArtifact().ToInterchangeEnvelope();
        PackageAssertions.Require(portable.Topology!.Alignments[0].Id == "request" && portable.Topology.Alignments[0].Kind == TopologyAlignmentKind.Row,
            "Packed ordinary native alignment API did not reach typed interchange.");
        TopologyAlignmentReviewContracts();
    }
}
