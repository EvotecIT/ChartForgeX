using System;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void MermaidArchitectureCaptionContracts() {
        const string source = "architecture-beta\ngroup scope(cloud)\nservice node(database) in scope";
        var direct = RenderFixture(source); PackageAssertions.Require(!direct.HasErrors && direct.Diagnostics.Count == 0, "Packed optional architecture captions were rejected.");
        var artifact = direct.Artifact ?? throw new InvalidOperationException("Packed optional architecture artifact was omitted.");
        var document = direct.Document as MermaidArchitectureDocument ?? throw new InvalidOperationException("Packed optional architecture AST was lost.");
        PackageAssertions.Require(document.Groups[0].Title == "" && document.Services[0].Title == "", "Packed optional source fabricated authored captions.");
        var chart = artifact.Model as TopologyChart ?? throw new InvalidOperationException("Packed optional architecture native owner was lost.");
        PackageAssertions.Require(!chart.Groups[0].ShowLabel && chart.Groups[0].Label == "scope" && chart.Groups[0].IconId == "cloud:cloud"
            && !chart.Nodes[0].ShowLabel && chart.Nodes[0].Label == "node" && chart.Nodes[0].IconId == "common:database", "Packed optional captions lost semantic fallback, visibility or native glyph identity.");
        var restored = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson()).ToTopologyChart();
        PackageAssertions.Require(!restored.Groups[0].ShowLabel && restored.Groups[0].Label == "scope" && !restored.Nodes[0].ShowLabel,
            "Packed optional-caption portable restoration lost native flags or semantic labels.");
        PackageAssertions.Png(artifact.ToPng());
        var fenced = CreateFixtureParser().Parse("```mermaid\n" + source + "\n```");
        PackageAssertions.Require(!fenced.HasErrors && fenced.Artifacts.Count == 1 && artifact.ToSvg() == fenced.Artifacts[0].ToSvg(), "Packed optional-caption fence diverged from its native scene.");
    }
}
