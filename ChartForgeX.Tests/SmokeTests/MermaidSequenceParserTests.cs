using System;
using System.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void MermaidParserParsesSequenceParticipantsAliasesAndMessages() {
        const string source = @"sequenceDiagram
participant U as User
actor API as Native API
participant DB {""type"": ""database"", ""alias"": ""Data Store""}
U->>API: Request
API-->>U: Response
API->>DB: Store";

        var result = new MermaidParser().ParseSequence(source);

        Assert(!result.HasErrors, "Mermaid sequence parser should parse participants, aliases, and messages: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid sequence parser should produce a document.");
        Assert(document.Kind == MermaidDiagramKind.Sequence, "Mermaid sequence parser should produce a sequence document.");
        Assert(document.Participants.Count == 3, "Mermaid sequence parser should retain declared participants in source order.");
        Assert(document.Participants[0].Id == "U" && document.Participants[0].Alias == "User", "Mermaid sequence parser should parse external participant aliases.");
        Assert(document.Participants[1].Kind == MermaidSequenceParticipantKind.Actor && document.Participants[1].Alias == "Native API", "Mermaid sequence parser should parse actors and aliases.");
        Assert(document.Participants[2].Kind == MermaidSequenceParticipantKind.Database && document.Participants[2].Alias == "Data Store", "Mermaid sequence parser should parse participant configuration aliases and types.");
        Assert(document.Messages.Count == 3, "Mermaid sequence parser should parse sequence messages.");
        Assert(document.Messages[0].SourceId == "U" && document.Messages[0].TargetId == "API", "Mermaid sequence parser should parse message endpoints.");
        Assert(document.Messages[0].Operator == "->>" && document.Messages[0].Text == "Request", "Mermaid sequence parser should preserve message operators and text.");
        Assert(document.Messages[1].Operator == "-->>" && document.Messages[1].Text == "Response", "Mermaid sequence parser should preserve dotted response operators.");
    }

    private static void MermaidParserParsesSequenceNotesActivationsBlocksAutonumberAndLinks() {
        const string source = @"sequenceDiagram
autonumber 10 0.5
Alice->>+Bob: Hello
activate Bob
Note right of Bob: Processing
loop Every minute
  Bob-->>-Alice: Done
end
link Bob: Dashboard @ https://example.com/bob";

        var result = new MermaidParser().ParseSequence(source);

        Assert(!result.HasErrors, "Mermaid sequence parser should parse notes, activations, blocks, autonumber, and links: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid sequence parser should produce a document.");
        Assert(document.Autonumber != null && document.Autonumber.Start == "10" && document.Autonumber.Increment == "0.5", "Mermaid sequence parser should parse autonumber start and increment.");
        Assert(document.Messages.Count == 2, "Mermaid sequence parser should parse messages around blocks.");
        Assert(document.Messages[0].ActivatesTarget, "Mermaid sequence parser should preserve activation shortcut metadata.");
        Assert(document.Messages[1].Deactivates, "Mermaid sequence parser should preserve deactivation shortcut metadata.");
        Assert(document.Activations.Count == 1 && document.Activations[0].ParticipantId == "Bob" && document.Activations[0].Active, "Mermaid sequence parser should parse activation declarations.");
        Assert(document.Notes.Count == 1 && document.Notes[0].Placement == "right of" && document.Notes[0].ParticipantIds[0] == "Bob", "Mermaid sequence parser should parse notes and note targets.");
        Assert(document.Blocks.Count == 2 && document.Blocks[0].Kind == MermaidSequenceBlockKind.Loop && document.Blocks[1].Kind == MermaidSequenceBlockKind.End, "Mermaid sequence parser should parse block start and end statements.");
        Assert(document.Links.Count == 1 && document.Links[0].ParticipantId == "Bob" && document.Links[0].Url == "https://example.com/bob", "Mermaid sequence parser should parse actor menu links.");
    }

    private static void MermaidParserParsesSequenceAltBreakAndAdvancedLinks() {
        const string source = @"sequenceDiagram
participant Alice
participant Bob
links Bob: { ""Dashboard"": ""https://example.com/bob"" }
alt successful case
  Alice->>Bob: Request
else failure case
  break something failed
    Bob-->>Alice: Error
  end
end";

        var result = new MermaidParser().ParseSequence(source);

        Assert(!result.HasErrors, "Mermaid sequence parser should parse alt, break, and advanced link statements: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid sequence parser should produce a document.");
        Assert(document.Links.Count == 1 && document.Links[0].RawJson != null, "Mermaid sequence parser should preserve advanced actor-menu links as raw JSON.");
        Assert(document.Blocks.Exists(block => block.Kind == MermaidSequenceBlockKind.Alt), "Mermaid sequence parser should parse alt blocks.");
        Assert(document.Blocks.Exists(block => block.Kind == MermaidSequenceBlockKind.Break), "Mermaid sequence parser should parse break blocks.");
        var artifact = document.ToSequenceArtifact();
        Assert(artifact.Blocks.Any(block => block.Kind == SequenceArtifactBlockKind.Break), "Mermaid sequence conversion should keep break blocks in the reusable sequence artifact.");
    }

    private static void MermaidSequenceConvertsToSequenceArtifactAndRenders() {
        const string source = @"---
title: Incident Sequence
---
sequenceDiagram
participant U as User
actor API as Native API
U->>API: Request
Note right of API: Processing
API-->>U: Response";

        var result = new MermaidParser().ParseSequence(source);
        Assert(!result.HasErrors, "Mermaid sequence parser should parse renderable source: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid sequence parser should produce a document.");

        var sequence = document.ToSequenceArtifact(new MermaidSequenceRenderOptions { Id = "incident-sequence", Width = 720, Height = 420 });
        Assert(sequence.Id == "incident-sequence", "Mermaid sequence conversion should preserve caller-provided ids.");
        Assert(sequence.Title == "Incident Sequence", "Mermaid sequence conversion should use frontmatter title by default.");
        Assert(sequence.Participants.Count == 2 && sequence.Messages.Count == 2, "Mermaid sequence conversion should map participants and messages.");
        Assert(sequence.Notes.Count == 1, "Mermaid sequence conversion should map notes.");
        Assert(sequence.Participants[1].Metadata["mermaid.kind"] == MermaidSequenceParticipantKind.Actor.ToString(), "Mermaid sequence conversion should preserve Mermaid participant metadata.");

        var artifact = document.ToVisualArtifact(new MermaidSequenceRenderOptions { Id = "incident-sequence" });
        Assert(artifact.Kind == VisualArtifactKind.Mermaid, "Mermaid sequence visual artifact should report Mermaid artifact kind.");
        Assert(artifact.SourceLanguage == VisualArtifactSourceLanguage.Mermaid, "Mermaid sequence visual artifact should preserve source language.");
        Assert(artifact.Model is SequenceArtifact, "Mermaid sequence visual artifact should carry a renderable sequence model.");
        Assert(artifact.Metadata["render.model"] == nameof(SequenceArtifact), "Mermaid sequence visual artifact should expose its render model.");

        var svg = document.ToSvg(new MermaidSequenceRenderOptions { Id = "incident-sequence" });
        var png = document.ToPng(new MermaidSequenceRenderOptions { Id = "incident-sequence" });
        Assert(svg.Contains("data-cfx-role=\"sequence-message\"", StringComparison.Ordinal), "Mermaid sequence SVG rendering should emit sequence message roles.");
        Assert(png.Length > 64 && png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47, "Mermaid sequence PNG rendering should emit a valid PNG.");
    }
}
