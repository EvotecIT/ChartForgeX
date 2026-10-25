using System;
using System.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void MermaidSequenceRectNumberingContracts() {
        const string source = "sequenceDiagram\nA->>B: Before\nrect rgba(200,220,255,0.5)\n" +
            "autonumber 5 0.5\nA->>B: Start\nautonumber off\nB-->>A: Hidden\nautonumber\nA->>B: Resume\nend\n" +
            "autonumber 10\nB-->>A: Reset\nA->>B: Next";
        var direct = MermaidRenderer.Render(source);
        PackageAssertions.Require(!direct.HasErrors && direct.Diagnostics.Count == 0 && direct.Artifact != null,
            "Packed authored rect/numbering source was rejected.");
        var document = direct.Document as MermaidSequenceDocument ?? throw new InvalidOperationException("Packed sequence lost its typed document.");
        PackageAssertions.Require(document.AutonumberStatements.Count == 4 && ReferenceEquals(document.Autonumber, document.AutonumberStatements.Last()),
            "Packed numbering controls did not retain one canonical ordered collection.");
        var artifact = direct.Artifact ?? throw new InvalidOperationException("Packed sequence omitted its artifact.");
        var sequence = artifact.Model as SequenceArtifact ?? throw new InvalidOperationException("Packed sequence lost its native owner.");
        PackageAssertions.Require(sequence.Blocks.Single().BackgroundColor.HasValue
            && sequence.Messages.Select(message => message.Metadata["mermaid.autonumber.value"]).SequenceEqual(new[] { "1", "5", "5.5", "6", "10", "11" }),
            "Packed source background or per-message number facts were lost.");
        var svg = artifact.ToSvg();
        PackageAssertions.Require(svg.Contains("sequence-block-background", StringComparison.Ordinal)
            && HasNativeSequenceNumber(svg, "Resume", "6") && HasNativeSequenceNumber(svg, "Next", "11"),
            "Packed background or occurrence numbering did not reach the shared scene.");
        PackageAssertions.Png(artifact.ToPng());
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson());
        PackageAssertions.Require(envelope.Annotations.Single(annotation => annotation.Role == VisualArtifactInterchangeAnnotationRole.SequenceBlock)
            .Extensions.ContainsKey("chartforgex.sequence.background") && envelope.Edges[2].Extensions["mermaid.autonumber.visible"] == "false",
            "Packed background or hidden counter facts were lost in portable interchange.");
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        PackageAssertions.Require(!fenced.HasErrors && fenced.Diagnostics.Count == 0 && fenced.Artifacts.Count == 1,
            "Packed fenced authored rect/numbering source was rejected.");
        PackageAssertions.Png(fenced.Artifacts[0].ToPng());
    }
}
