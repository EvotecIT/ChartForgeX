using System;
using System.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void MermaidSequencePolicyContracts() {
        const string source = "sequenceDiagram\ntitle Worker lifetime\nbox transparent Services\nparticipant API\nend\n" +
            "create actor W as Worker\nAPI->>W: Start\ndestroy W\nW-->>API: Finished";
        var options = new MermaidRenderOptions {
            WarningPolicy = MermaidWarningPolicy.Reject,
            Presentation = new MermaidPresentationOptions { ThemeMode = VisualThemeMode.Dark, FontSize = 15 },
            Sequence = new MermaidSequenceRenderOptions { Title = "Packed lifecycle" }
        };
        var rendered = MermaidRenderer.Render(source, options);
        PackageAssertions.Require(!rendered.HasErrors && rendered.Diagnostics.Count == 0 && rendered.Artifact != null,
            "Packed sequence lifecycle did not produce exact native output.");
        var artifact = rendered.Artifact ?? throw new InvalidOperationException("Packed sequence lifecycle omitted its artifact.");
        var sequence = artifact.Model as SequenceArtifact ?? throw new InvalidOperationException("Packed sequence lost its native owner.");
        PackageAssertions.Require(sequence.Title == "Packed lifecycle" && sequence.ThemeMode == VisualThemeMode.Dark
            && Math.Abs(sequence.Theme.Typography.DataLabelSize - 15) < 0.001 && sequence.ParticipantGroups.Count == 1,
            "Packed shared host presentation or participant box did not reach the native sequence model.");
        var worker = sequence.Participants.Single(participant => participant.Id == "W");
        PackageAssertions.Require(worker.CreatedAtMessageIndex == 0 && worker.DestroyedAtMessageIndex == 1,
            "Packed sequence creation and destruction did not retain their message indices.");
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson());
        PackageAssertions.Require(envelope.Nodes.Single(node => node.Label == "Worker").Sequence?.DestroyedAtMessageIndex == 1
            && envelope.Groups.Single().Role == VisualArtifactInterchangeGroupRole.SequenceParticipantGroup,
            "Packed sequence lifetime or participant group was lost during typed JSON round-trip.");
        PackageAssertions.Require(artifact.ToSvg().Contains("sequence-destruction", StringComparison.Ordinal),
            "Packed sequence omitted its native destruction mark.");
        PackageAssertions.Png(artifact.ToPng());
        var fenced = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```");
        PackageAssertions.Require(!fenced.HasErrors && fenced.Diagnostics.Count == 0 && fenced.Artifacts.Count == 1,
            "Packed Markdown bridge did not retain the exact sequence lifecycle.");
        PackageAssertions.Png(fenced.Artifacts[0].ToPng());

        const string invalid = "sequenceDiagram\nautonumber nope\nA->>B: Invalid number";
        var invalidDirect = MermaidRenderer.Render(invalid);
        var invalidFence = new MermaidVisualMarkupParser().Parse("```mermaid\n" + invalid + "\n```");
        PackageAssertions.Require(invalidDirect.HasErrors && invalidDirect.Artifact == null
            && invalidDirect.Diagnostics.Any(item => item.Code == MermaidDiagnosticCodes.InvalidStatement)
            && invalidFence.HasErrors && invalidFence.Artifacts.Count == 0,
            "Packed invalid sequence numbering escaped parse validation into a lazy export failure.");

        MermaidRequirementStyleContracts();
        var bounded = new MermaidRenderOptions {
            ParseOptions = new MermaidParseOptions { ResourceLimits = new MermaidResourceLimits { MaximumSourceCharacters = 16 } }
        };
        var limitedDirect = MermaidRenderer.Render(source, bounded);
        var limitedFence = new MermaidVisualMarkupParser(bounded).Parse("```mermaid\n" + source + "\n```");
        PackageAssertions.Require(limitedDirect.HasErrors && limitedDirect.Artifact == null
            && limitedDirect.Diagnostics.Any(item => item.Code == MermaidDiagnosticCodes.ResourceLimitExceeded)
            && limitedFence.HasErrors && limitedFence.Artifacts.Count == 0,
            "Packed direct and fenced rendering did not preserve the shared source resource limit.");
        Console.WriteLine("Packed Mermaid sequence lifetime, shared presentation, strict fidelity and direct/fenced validation passed.");
    }
}
