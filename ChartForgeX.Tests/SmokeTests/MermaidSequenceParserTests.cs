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
API->>DB: Store
API--)DB: Queue asynchronously
DB--xAPI: Reject";

        var result = new MermaidParser().ParseSequence(source);

        Assert(!result.HasErrors, "Mermaid sequence parser should parse participants, aliases, and messages: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid sequence parser should produce a document.");
        Assert(document.Kind == MermaidDiagramKind.Sequence, "Mermaid sequence parser should produce a sequence document.");
        Assert(document.Participants.Count == 3, "Mermaid sequence parser should retain declared participants in source order.");
        Assert(document.Participants[0].Id == "U" && document.Participants[0].Alias == "User", "Mermaid sequence parser should parse external participant aliases.");
        Assert(document.Participants[1].Kind == MermaidSequenceParticipantKind.Actor && document.Participants[1].Alias == "Native API", "Mermaid sequence parser should parse actors and aliases.");
        Assert(document.Participants[2].Kind == MermaidSequenceParticipantKind.Database && document.Participants[2].Alias == "Data Store", "Mermaid sequence parser should parse participant configuration aliases and types.");
        Assert(document.Messages.Count == 5, "Mermaid sequence parser should parse sequence messages.");
        Assert(document.Messages[0].SourceId == "U" && document.Messages[0].TargetId == "API", "Mermaid sequence parser should parse message endpoints.");
        Assert(document.Messages[0].Operator == "->>" && document.Messages[0].Text == "Request", "Mermaid sequence parser should preserve message operators and text.");
        Assert(document.Messages[1].Operator == "-->>" && document.Messages[1].Text == "Response", "Mermaid sequence parser should preserve dotted response operators.");
        SequenceArtifact sequence = document.ToSequenceArtifact();
        Assert(sequence.Messages[0].Kind == SequenceArtifactMessageKind.Call &&
               sequence.Messages[1].Kind == SequenceArtifactMessageKind.Call &&
               sequence.Messages[1].LineStyle == SequenceArtifactMessageLineStyle.Dashed &&
               sequence.Messages[1].Metadata["mermaid.operator"] == "-->>" &&
               sequence.Messages[3].Kind == SequenceArtifactMessageKind.Async &&
               sequence.Messages[3].LineStyle == SequenceArtifactMessageLineStyle.Dashed &&
               sequence.Messages[4].Kind == SequenceArtifactMessageKind.Call &&
               sequence.Messages[4].Metadata["mermaid.operator"] == "--x",
            "Mermaid sequence conversion should not infer message purpose from dashed presentation while retaining the exact source operator.");
    }

    private static void MermaidParserParsesSequenceNotesActivationsBlocksAutonumberAndLinks() {
        const string source = @"sequenceDiagram
autonumber 10 0.5
Alice->>+Bob: Hello; activate Bob; Note right of Bob: Processing; loop Every minute
  Bob-->>-Alice: Done; deactivate Bob; end
link Bob: Dashboard @ https://example.com/bob";

        var result = new MermaidParser().ParseSequence(source);

        Assert(!result.HasErrors, "Mermaid sequence parser should parse notes, activations, blocks, autonumber, and links: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid sequence parser should produce a document.");
        Assert(document.Autonumber != null && document.Autonumber.Start == "10" && document.Autonumber.Increment == "0.5", "Mermaid sequence parser should parse autonumber start and increment.");
        Assert(document.Messages.Count == 2, "Mermaid sequence parser should parse messages around blocks.");
        Assert(document.Messages[0].ActivatesTarget, "Mermaid sequence parser should preserve activation shortcut metadata.");
        Assert(document.Messages[1].Deactivates, "Mermaid sequence parser should preserve deactivation shortcut metadata.");
        Assert(document.Activations.Count == 2 && document.Activations[0].ParticipantId == "Bob" && document.Activations[0].Active && !document.Activations[1].Active,
            "Mermaid sequence parser should parse standalone activation and deactivation declarations.");
        Assert(document.Notes.Count == 1 && document.Notes[0].Placement == "right of" && document.Notes[0].ParticipantIds[0] == "Bob", "Mermaid sequence parser should parse notes and note targets.");
        Assert(document.Blocks.Count == 2 && document.Blocks[0].Kind == MermaidSequenceBlockKind.Loop && document.Blocks[1].Kind == MermaidSequenceBlockKind.End, "Mermaid sequence parser should parse block start and end statements.");
        Assert(document.Links.Count == 1 && document.Links[0].ParticipantId == "Bob" && document.Links[0].Url == "https://example.com/bob", "Mermaid sequence parser should parse actor menu links.");

        var sequence = document.ToSequenceArtifact();
        var bob = sequence.Participants.Single(participant => participant.Id == "Bob");
        Assert(bob.Href == "https://example.com/bob", "Mermaid sequence conversion should expose simple participant links through the reusable navigation contract.");
        Assert(sequence.Activations.Count == 2 && sequence.Activations[0].ParticipantId == "Bob" && sequence.Activations[0].Active && sequence.Activations[0].StepIndex == 1 &&
               !sequence.Activations[1].Active && sequence.Activations[1].StepIndex == 2,
            "Mermaid sequence conversion should retain standalone activation state changes at their semantic steps.");
        Assert(sequence.Notes.Single().StepIndex == 1 && sequence.Blocks.Single().StartStepIndex == 1 && sequence.Blocks.Single().EndStepIndex == 1 && !sequence.Blocks.Single().IsEmpty,
            "Mermaid sequence conversion should order same-line notes and inclusive block boundaries by source column as well as line.");

        var visual = document.ToVisualArtifact();
        Assert(visual.Metadata["mermaid.activations"] == "2" && visual.Regions.Single(region => region.Id == "Bob").Href == "https://example.com/bob",
            "Mermaid participant links should reach the host-inspectable visual region contract.");
        var envelope = visual.ToInterchangeEnvelope();
        var bobNode = envelope.Nodes.Single(node => node.Label == "Bob");
        var activation = envelope.Annotations.Single(annotation => annotation.Kind == "SequenceActivation");
        var deactivation = envelope.Annotations.Single(annotation => annotation.Kind == "SequenceDeactivation");
        Assert(bobNode.Href == "https://example.com/bob", "Mermaid participant links should reach the product-neutral interchange node href.");
        Assert(envelope.Kind == VisualArtifactKind.Mermaid && envelope.Family == VisualArtifactInterchangeFamily.Sequence && envelope.Sequence != null,
            "Mermaid authoring identity should remain separate from the reusable sequence semantic family.");
        Assert(envelope.Annotations.Count(annotation => annotation.Role == VisualArtifactInterchangeAnnotationRole.SequenceActivation) == 2 &&
               activation.TargetIds.SequenceEqual(new[] { bobNode.Id }) && activation.StartIndex == 1 && activation.EndIndex == 1 &&
               deactivation.TargetIds.SequenceEqual(new[] { bobNode.Id }) && deactivation.StartIndex == 2 && deactivation.EndIndex == 2,
            "Standalone Mermaid activation changes should reach the product-neutral interchange annotation contract.");
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
        Assert(artifact.Branches.Count == 2 && artifact.Branches[0].Kind == "Primary" && artifact.Branches[1].Kind == "Else" &&
               artifact.Branches[0].EndStepIndex + 1 == artifact.Branches[1].StartStepIndex &&
               artifact.Branches.All(branch => branch.EndStepIndex >= branch.StartStepIndex && branch.EndStepIndex < artifact.Messages.Count),
            "Mermaid sequence conversion should preserve non-overlapping inclusive branch spans in the reusable sequence model.");
        var branchEnvelope = document.ToVisualArtifact().ToInterchangeEnvelope();
        Assert(branchEnvelope.Annotations.Count(annotation => annotation.Kind.StartsWith("SequenceBranch:", StringComparison.Ordinal)) == 2,
            "Mermaid sequence branches should reach the product-neutral interchange annotation contract.");

        const string siblingBranches = @"sequenceDiagram
A->>B: Start
par First path
  A->>B: One
and Second path
  B-->>A: Two
end
critical Service call
  A->>B: Call
option Timeout
  B-->>A: Retry
end";
        var siblingResult = new MermaidParser().ParseSequence(siblingBranches);
        Assert(!siblingResult.HasErrors, "Mermaid sequence parser should parse parallel and critical branch statements: " + MermaidDiagnostics(siblingResult));
        var siblingArtifact = siblingResult.Document!.ToSequenceArtifact();
        Assert(siblingArtifact.Branches.Any(branch => branch.Kind == "And" && branch.ParentKind == SequenceArtifactBlockKind.Par) &&
               siblingArtifact.Branches.Any(branch => branch.Kind == "Option" && branch.ParentKind == SequenceArtifactBlockKind.Critical),
            "Mermaid sequence conversion should preserve parallel and critical sibling branches, not only else branches.");
        Assert(siblingArtifact.Branches.All(branch => branch.EndStepIndex >= branch.StartStepIndex && branch.EndStepIndex < siblingArtifact.Messages.Count),
            "Parallel and critical sequence branch spans should end at their last covered message without overlapping the next branch.");
        Assert(siblingArtifact.Blocks.Single(block => block.Kind == SequenceArtifactBlockKind.Par).StartStepIndex == 1 &&
               siblingArtifact.Blocks.Single(block => block.Kind == SequenceArtifactBlockKind.Par).EndStepIndex == 2 &&
               siblingArtifact.Blocks.Single(block => block.Kind == SequenceArtifactBlockKind.Critical).StartStepIndex == 3 &&
               siblingArtifact.Blocks.Single(block => block.Kind == SequenceArtifactBlockKind.Critical).EndStepIndex == 4,
            "Switching Mermaid branches should retain each enclosing block's original start and complete it at the last covered message.");

        const string emptyBranch = @"sequenceDiagram
A->>B: Before
alt Primary
  A->>B: Inside
else No action
end
B-->>A: After";
        var emptyBranchResult = new MermaidParser().ParseSequence(emptyBranch);
        Assert(!emptyBranchResult.HasErrors, "Mermaid sequence parser should retain empty branches: " + MermaidDiagnostics(emptyBranchResult));
        var emptyBranchDocument = emptyBranchResult.Document ?? throw new InvalidOperationException("Empty-branch Mermaid sequence should produce a document.");
        var emptyBranchArtifact = emptyBranchDocument.ToSequenceArtifact();
        Assert(emptyBranchArtifact.Blocks.Single().StartStepIndex == 1 && emptyBranchArtifact.Blocks.Single().EndStepIndex == 1 && !emptyBranchArtifact.Blocks.Single().IsEmpty,
            "Nonempty Mermaid blocks should end at their last covered message rather than the following message.");
        Assert(emptyBranchArtifact.Branches.Count == 2 && emptyBranchArtifact.Branches.Single(branch => branch.Kind == "Else").IsEmpty,
            "Mermaid sequence conversion should retain explicitly empty sibling branches and their labels.");
        var emptyBranchEnvelope = emptyBranchDocument.ToVisualArtifact().ToInterchangeEnvelope();
        var emptyElse = emptyBranchEnvelope.Annotations.Single(annotation => annotation.Kind == "SequenceBranch:Else");
        Assert(emptyElse.StartIndex == 2 && emptyElse.EndIndex == null && emptyElse.Sequence!.IsEmpty,
            "Interchange projection should expose an empty branch boundary without claiming that it covers the following message.");
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
