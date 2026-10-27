using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Rendering;
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
        var rendered = RenderFixture(source, options);
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
        var prepared = sequence.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(1200, 900)), sequence.Theme, sequence.ThemeMode));
        var footer = prepared.Regions.Single(region => region.Role == "sequence-participant-footer" && region.Id == "W");
        var survivingFooter = prepared.Regions.Single(region => region.Role == "sequence-participant-footer" && region.Id == "API");
        var svg = XDocument.Parse(prepared.ToSvg());
        var workerLine = svg.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "sequence-lifeline"
            && Math.Abs(double.Parse((string)node.Attribute("x1")!, CultureInfo.InvariantCulture) - footer.Bounds.X - footer.Bounds.Width / 2) < .001);
        PackageAssertions.Require(sequence.Layout?.MirrorParticipants == true && footer.Bounds.Bottom < survivingFooter.Bounds.Top
            && Math.Abs(double.Parse((string)workerLine.Attribute("y2")!, CultureInfo.InvariantCulture) - footer.Bounds.Top) < .001,
            "Packed mirrored sequence did not end the worker lifeline at its destruction footer before the surviving participant.");
        PackageAssertions.Png(artifact.ToPng());
        var fenced = CreateFixtureParser(options).Parse("```mermaid\n" + source + "\n```");
        PackageAssertions.Require(!fenced.HasErrors && fenced.Diagnostics.Count == 0 && fenced.Artifacts.Count == 1,
            "Packed Markdown bridge did not retain the exact sequence lifecycle.");
        PackageAssertions.Png(fenced.Artifacts[0].ToPng());

        const string invalid = "sequenceDiagram\nautonumber nope\nA->>B: Invalid number";
        var invalidDirect = RenderFixture(invalid);
        var invalidFence = CreateFixtureParser().Parse("```mermaid\n" + invalid + "\n```");
        PackageAssertions.Require(invalidDirect.HasErrors && invalidDirect.Artifact == null
            && invalidDirect.Diagnostics.Any(item => item.Code == MermaidDiagnosticCodes.InvalidStatement)
            && invalidFence.HasErrors && invalidFence.Artifacts.Count == 0,
            "Packed invalid sequence numbering escaped parse validation into a lazy export failure.");

        MermaidSequenceDepthContracts();
        MermaidRequirementStyleContracts();
        var bounded = new MermaidRenderOptions {
            ParseOptions = new MermaidParseOptions { ResourceLimits = new MermaidResourceLimits { MaximumSourceCharacters = 16 } }
        };
        var limitedDirect = RenderFixture(source, bounded);
        var limitedFence = CreateFixtureParser(bounded).Parse("```mermaid\n" + source + "\n```");
        PackageAssertions.Require(limitedDirect.HasErrors && limitedDirect.Artifact == null
            && limitedDirect.Diagnostics.Any(item => item.Code == MermaidDiagnosticCodes.ResourceLimitExceeded)
            && limitedFence.HasErrors && limitedFence.Artifacts.Count == 0,
            "Packed direct and fenced rendering did not preserve the shared source resource limit.");
        Console.WriteLine("Packed Mermaid sequence lifetime, shared presentation, strict fidelity and direct/fenced validation passed.");
    }
}
