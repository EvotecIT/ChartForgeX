using System;
using System.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void MermaidArchitectureContracts() {
        MermaidArchitectureCaptionContracts();
        MermaidArchitectureAlignmentContracts();
        const string source = "architecture-beta\ngroup outer[Outer]\ngroup inner[Inner] in outer\nservice a[A] in inner\nservice b[B]\na{group}:B --> T:b";
        var direct = MermaidRenderer.Render(source);
        PackageAssertions.Require(!direct.HasErrors && direct.Diagnostics.Count == 0 && direct.Artifact != null, "Packed native architecture source was rejected.");
        var artifact = direct.Artifact ?? throw new InvalidOperationException("Packed architecture artifact was omitted.");
        var document = direct.Document as MermaidArchitectureDocument ?? throw new InvalidOperationException("Packed architecture AST was omitted.");
        var chart = artifact.Model as TopologyChart ?? throw new InvalidOperationException("Packed architecture topology owner was lost.");
        PackageAssertions.Require(document.Edges[0].Left.Id == "a" && document.Edges[0].Left.GroupBoundary && document.Edges[0].Span.Line == 6,
            "Packed architecture lost its source endpoint occurrence/span.");
        PackageAssertions.Require(chart.Groups.Single(group => group.Id == "inner").ParentGroupId == "outer" && chart.LayoutMode == TopologyLayoutMode.Layered,
            "Packed architecture lost native hierarchy ownership.");
        PackageAssertions.Require(chart.Nodes.Count == 2 && chart.Edges[0].SourceNodeId == "inner" && chart.Edges[0].SourcePort == TopologyEdgePort.Bottom,
            "Packed architecture substituted an anchor node or lost its authored boundary side.");
        var detached = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson()); var restored = detached.ToTopologyChart();
        PackageAssertions.Require(restored.Groups.Single(group => group.Id == "inner").ParentGroupId == "outer" && restored.Edges[0].SourceNodeId == "inner"
            && restored.Edges[0].Metadata["mermaid.source.id"] == "a", "Packed architecture portable round trip lost native/source endpoint meaning.");
        PackageAssertions.Png(artifact.ToPng());
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        PackageAssertions.Require(!fenced.HasErrors && fenced.Artifacts.Count == 1 && fenced.Artifacts[0].ToSvg() == artifact.ToSvg(),
            "Packed architecture fence did not use the canonical native scene.");
    }
}
