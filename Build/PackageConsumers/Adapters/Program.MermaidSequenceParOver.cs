using System;
using System.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void MermaidSequenceParOverContracts() {
        const string source = "sequenceDiagram\npar_over First\nA->>B: Request\nand Notes only\nNote over A,B: No reply\nend\nB-->>A: After";
        var direct = RenderFixture(source);
        PackageAssertions.Require(!direct.HasErrors && direct.Diagnostics.Count == 0 && direct.Artifact != null,
            "Packed par_over source was rejected.");
        var document = direct.Document as MermaidSequenceDocument ?? throw new InvalidOperationException("Packed par_over lost its typed document.");
        PackageAssertions.Require(document.Blocks[0].Kind == MermaidSequenceBlockKind.ParOver && document.Blocks[0].Span.Line == 2,
            "Packed par_over lost its authored kind or source occurrence.");
        var artifact = direct.Artifact ?? throw new InvalidOperationException("Packed par_over omitted its artifact.");
        var sequence = artifact.Model as SequenceArtifact ?? throw new InvalidOperationException("Packed par_over lost its native owner.");
        PackageAssertions.Require(sequence.Blocks.Single().Kind == SequenceArtifactBlockKind.ParOver
            && sequence.Branches.Count == 2 && sequence.Branches.All(branch => branch.ParentKind == SequenceArtifactBlockKind.ParOver)
            && sequence.Blocks.Single().BackgroundColor == null,
            "Packed par_over did not use the existing typed block/branch owner and null defaults.");
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson());
        PackageAssertions.Require(envelope.Annotations.Count(annotation => annotation.Sequence?.BlockKind == SequenceArtifactBlockKind.ParOver) == 1
            && envelope.Annotations.Count(annotation => annotation.Sequence?.ParentBlockKind == SequenceArtifactBlockKind.ParOver) == 2,
            "Packed par_over facts were lost in typed portable interchange.");
        var ordinary = RenderFixture(source.Replace("par_over ", "par ", StringComparison.Ordinal));
        PackageAssertions.Require(!ordinary.HasErrors && ordinary.Artifact != null && artifact.ToSvg() == ordinary.Artifact.ToSvg(),
            "Packed par_over must reuse the measured parallel scene.");
        PackageAssertions.Png(artifact.ToPng());
        var fenced = CreateFixtureParser().Parse("```mermaid\n" + source + "\n```");
        PackageAssertions.Require(!fenced.HasErrors && fenced.Artifacts.Count == 1 && artifact.ToSvg() == fenced.Artifacts[0].ToSvg(),
            "Packed fenced par_over diverged from the shared native scene.");
        PackageAssertions.Png(fenced.Artifacts[0].ToPng());
    }
}
