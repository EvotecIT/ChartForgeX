using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void UseCaseSourceDepthContracts() {
        using var stream = typeof(Program).Assembly.GetManifestResourceStream("UseCaseSourceDepth.mmd")
            ?? throw new InvalidOperationException("Packed adapter probe omitted its canonical UseCase source.");
        using var reader = new StreamReader(stream); var source = reader.ReadToEnd().Replace("direction LR", "direction TB", StringComparison.Ordinal);
        var options = new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject,
            UseCase = new MermaidFlowchartRenderOptions { Width = 390, Height = 620,
                Presentation = new MermaidPresentationOptions { TopologySizing = MermaidTopologySizing.FixedViewport } } };
        var direct = RenderFixture(source, options); var fence = CreateFixtureParser(options).Parse("```mermaid\n" + source + "\n```");
        PackageAssertions.Require(!direct.HasErrors && direct.Diagnostics.Count == 0 && direct.Artifact != null && !fence.HasErrors && fence.Artifacts.Count == 1,
            "Packed UseCase notes/stereotypes/association settings did not lower to one native model.");
        var chart = (TopologyChart)direct.Artifact!.Model!; var edge = chart.Edges.Single(item => item.Id == "request");
        PackageAssertions.Require(chart.Nodes.Count == 3 && chart.Nodes[0].Subtitle == "«primary»" && chart.Annotations.Single().TargetId == "Publish"
            && edge.StrokeWidth == 2.5 && edge.Opacity == .8 && edge.DashPattern.SequenceEqual(new[] { 6d, 3, 2, 6, 3, 2 }),
            "Packed source mapping lost actual graph identities, visible stereotypes, note attachment or independent association paint.");
        var svg = direct.Artifact.ToSvg(); var png = direct.Artifact.ToPng();
        PackageAssertions.Require(svg == fence.Artifacts[0].ToSvg() && png.SequenceEqual(fence.Artifacts[0].ToPng()), "Packed UseCase direct/fence exports differ.");
        PackageAssertions.Require(XDocument.Parse(svg).Descendants().Any(node => (string?)node.Attribute("data-cfx-role") == "topology-node-subtitle"),
            "Packed source stereotype metadata did not become native visible caption paint.");
        var portable = VisualArtifactInterchangeEnvelope.FromJson(direct.Artifact.ToInterchangeJson());
        PackageAssertions.Require(portable.Extensions["mermaid.source"] == source && portable.Edges.Any(item => item.Id == "request") && portable.Annotations.Count == 1,
            "Packed portable UseCase facts lost raw source, named association or note.");
        PackageAssertions.Png(png);
    }
}
