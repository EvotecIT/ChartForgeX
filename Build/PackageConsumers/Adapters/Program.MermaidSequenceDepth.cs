using System;
using System.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void MermaidSequenceDepthContracts() {
        MermaidSequenceRectNumberingContracts();
        MermaidSequenceParOverContracts();
        MermaidSequenceParticipantContracts();
        const string source = "%%{init: {'sequence':{'showSequenceNumbers':true},'themeVariables':{'actorBkg':'#e3efff','signalColor':'#276749'}}}%%\n" +
            "sequenceDiagram\nbox hsla(210,60%,92%,0.5) Services\nparticipant A as Native<br/>API\nparticipant B\nend\n" +
            "par Primary\nA-|/B: Request<br/>record\nand Optional\nNote over A,B: No further message\nend";
        var direct = MermaidRenderer.Render(source);
        PackageAssertions.Require(!direct.HasErrors && direct.Diagnostics.Count == 0 && direct.Artifact != null,
            "Packed source sequence paint, HSL box, half-arrow, captions or numbering was rejected.");
        var artifact = direct.Artifact ?? throw new InvalidOperationException("Packed sequence depth omitted its artifact.");
        var sequence = artifact.Model as SequenceArtifact ?? throw new InvalidOperationException("Packed depth lost the native sequence owner.");
        PackageAssertions.Require(sequence.Paint.ParticipantFill == ChartColor.FromRgb(227, 239, 255)
            && sequence.Participants[0].Label == "Native\nAPI" && sequence.Messages[0].Metadata["mermaid.operator"] == "-|/",
            "Packed source presentation did not reach the reusable sequence owner.");
        var svg = artifact.ToSvg();
        PackageAssertions.Require(svg.Contains("sequence-half-call-arrow", StringComparison.Ordinal)
            && HasNativeSequenceNumber(svg, "Request", "1") && !svg.Contains("<foreignObject", StringComparison.OrdinalIgnoreCase),
            "Packed sequence did not use the native half-arrow, numbering and safe text scene.");
        PackageAssertions.Png(artifact.ToPng());
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson());
        PackageAssertions.Require(envelope.Extensions["chartforgex.sequence.paint.participantFill"] == "#E3EFFF"
            && envelope.Nodes[0].Extensions["mermaid.label"] == "Native<br/>API",
            "Packed authored paint or raw source caption was lost in portable interchange.");
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        PackageAssertions.Require(!fenced.HasErrors && fenced.Diagnostics.Count == 0 && fenced.Artifacts.Count == 1,
            "Packed fenced depth did not retain exact source mappings.");
        PackageAssertions.Png(fenced.Artifacts[0].ToPng());

        const string invalid = "%%{init: {'themeVariables':{'actorBkg':'hsl(nope,20%,30%)'}}}%%\nsequenceDiagram\nA->>B: Signal";
        var invalidDirect = MermaidRenderer.Render(invalid);
        var invalidFenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + invalid + "\n```");
        PackageAssertions.Require(invalidDirect.HasErrors && invalidDirect.Artifact == null && invalidDirect.Diagnostics.Any(item => item.Span.Line == 1)
            && invalidFenced.HasErrors && invalidFenced.Artifacts.Count == 0,
            "Packed invalid authored sequence color escaped located direct/fenced validation.");
    }
}
