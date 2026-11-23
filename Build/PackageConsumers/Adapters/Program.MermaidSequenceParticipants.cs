using System;
using System.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void MermaidSequenceParticipantContracts() {
        const string source = "sequenceDiagram\nparticipant A@{type: database, alias: Storage}\nparticipant B\n" +
            "links A: {\"Guide\":\"/guide\"}\nproperties A: {\"owner\":\"Platform\"}\nA->>B: Read";
        var rendered = RenderFixture(source);
        PackageAssertions.Require(!rendered.HasErrors && rendered.Diagnostics.Count == 0 && rendered.Artifact != null,
            "Packed official participant configuration/menu/property source was rejected.");
        var artifact = rendered.Artifact ?? throw new InvalidOperationException("Packed participant source omitted its artifact.");
        var document = rendered.Document as MermaidSequenceDocument ?? throw new InvalidOperationException("Packed participant document was lost.");
        PackageAssertions.Require(document.PropertyStatements.Count == 1 && document.PropertyStatements[0].Span.Line == 5
            && document.PropertyStatements[0].RawJson == "{\"owner\":\"Platform\"}", "Packed properties lost their canonical raw declaration/span.");
        var sequence = artifact.Model as SequenceArtifact ?? throw new InvalidOperationException("Packed participant native owner was lost.");
        PackageAssertions.Require(sequence.Participants[0].Kind == SequenceArtifactParticipantKind.Database
            && sequence.Participants[0].Label == "Storage" && sequence.Participants[0].Href == "/guide",
            "Packed source participant configuration or single safe menu did not reach native presentation.");
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson());
        PackageAssertions.Require(envelope.Nodes[0].Sequence?.Kind == SequenceArtifactParticipantKind.Database
            && envelope.Nodes[0].Extensions.ContainsKey("mermaid.properties.statements") && envelope.Nodes[0].Extensions.ContainsKey("mermaid.menu.entries"),
            "Packed participant property/menu facts were lost in portable interchange.");
        PackageAssertions.Png(artifact.ToPng());
        var fenced = CreateFixtureParser().Parse("```mermaid\n" + source + "\n```");
        PackageAssertions.Require(!fenced.HasErrors && fenced.Artifacts.Count == 1 && artifact.ToSvg() == fenced.Artifacts[0].ToSvg(),
            "Packed fenced participant source did not use the shared native scene.");
    }
}
