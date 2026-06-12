using System;
using System.Text;
using ChartForgeX;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void MermaidParserParsesIshikawaHierarchy() {
        const string source = @"ishikawa-beta
Delayed release
  People
    Handoffs
      Timezone gaps
  Process
    Late review
  Platform
    Slow build";

        var result = new MermaidParser().ParseIshikawa(source);

        Assert(!result.HasErrors, "Mermaid Ishikawa parser should parse indentation hierarchy: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid Ishikawa parser should produce a document.");
        Assert(document.Kind == MermaidDiagramKind.Ishikawa && document.Header == "ishikawa-beta", "Mermaid Ishikawa parser should preserve the header.");
        Assert(document.Root != null && document.Root.Text == "Delayed release", "Mermaid Ishikawa parser should preserve the root effect.");
        Assert(document.Root!.Children.Count == 3, "Mermaid Ishikawa parser should parse top-level causes.");
        Assert(document.Root.Children[0].Children[0].Children[0].Text == "Timezone gaps", "Mermaid Ishikawa parser should preserve nested sub-causes.");
        Assert(document.Statements.Count == 8, "Mermaid Ishikawa parser should retain raw body statements.");
    }

    private static void MermaidIshikawaConvertsToFishboneArtifactAndRenders() {
        const string source = @"ishikawa
Delayed release
  People
    Handoffs
  Process
    Late review";

        var result = new MermaidParser().ParseIshikawa(source);
        Assert(!result.HasErrors, "Mermaid Ishikawa parser should parse renderable source: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid Ishikawa parser should produce a document.");
        var block = document.ToFishboneDiagramBlock(new MermaidIshikawaRenderOptions { Id = "release-root-cause", Title = "Release Root Cause", Width = 840, Height = 460 });
        Assert(block.Title == "Release Root Cause", "Mermaid Ishikawa conversion should preserve caller-provided titles.");
        Assert(block.Effect == "Delayed release" && block.Causes.Count == 2, "Mermaid Ishikawa conversion should map source into a reusable fishbone block.");

        var artifact = document.ToVisualArtifact(new MermaidIshikawaRenderOptions { Id = "release-root-cause" });
        Assert(artifact.Kind == VisualArtifactKind.Mermaid, "Mermaid Ishikawa visual artifact should report Mermaid artifact kind.");
        Assert(artifact.Model is FishboneDiagramBlock, "Mermaid Ishikawa visual artifact should carry the fishbone model.");
        Assert(artifact.Metadata["mermaid.causes"] == "2", "Mermaid Ishikawa artifacts should expose cause counts.");
        Assert(artifact.Metadata["render.model"] == nameof(FishboneDiagramBlock), "Mermaid Ishikawa artifacts should expose the visual block render model.");

        var svg = document.ToSvg(new MermaidIshikawaRenderOptions { Id = "release-root-cause" });
        var png = document.ToPng(new MermaidIshikawaRenderOptions { Id = "release-root-cause" });
        Assert(svg.Contains("data-cfx-role=\"fishbone-spine\"", StringComparison.Ordinal), "Mermaid Ishikawa SVG rendering should include the fishbone spine.");
        Assert(svg.Contains("Late review", StringComparison.Ordinal), "Mermaid Ishikawa SVG rendering should include sub-cause labels.");
        Assert(png.Length > 64 && png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47, "Mermaid Ishikawa PNG rendering should emit a valid PNG.");
    }

    private static void MermaidIshikawaRejectsEffectWithoutCauses() {
        const string source = @"ishikawa
Only effect";

        var result = new MermaidParser().ParseIshikawa(source);

        Assert(result.HasErrors, "Mermaid Ishikawa parser should reject an effect without causes.");
        Assert(result.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("at least one cause", StringComparison.Ordinal)), "Mermaid Ishikawa errors should explain the missing cause contract.");
    }

    private static void MermaidIshikawaRejectsOversizedTreesDuringParsing() {
        var builder = new StringBuilder();
        builder.AppendLine("ishikawa");
        builder.AppendLine("Effect");
        for (var index = 0; index < 512; index++) builder.AppendLine("  Cause " + index.ToString(System.Globalization.CultureInfo.InvariantCulture));

        var result = new MermaidParser().ParseIshikawa(builder.ToString());

        Assert(result.HasErrors, "Mermaid Ishikawa parser should reject trees that exceed the fishbone renderer node limit.");
        Assert(result.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("no more than 512 nodes", StringComparison.Ordinal)), "Mermaid Ishikawa oversized tree diagnostics should explain the node limit.");
    }

    private static void MermaidIshikawaRejectsTooDeepTreesDuringParsing() {
        var builder = new StringBuilder();
        builder.AppendLine("ishikawa");
        builder.AppendLine("Effect");
        for (var level = 1; level <= 13; level++) builder.AppendLine(new string(' ', level * 2) + "Cause " + level.ToString(System.Globalization.CultureInfo.InvariantCulture));

        var result = new MermaidParser().ParseIshikawa(builder.ToString());

        Assert(result.HasErrors, "Mermaid Ishikawa parser should reject trees that exceed the fishbone renderer depth limit.");
        Assert(result.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("depth must not exceed 12", StringComparison.Ordinal)), "Mermaid Ishikawa depth diagnostics should explain the depth limit.");
    }

    private static void MermaidIshikawaCountsNestingLevelsInsteadOfSpaces() {
        var builder = new StringBuilder();
        builder.AppendLine("ishikawa");
        builder.AppendLine("Effect");
        for (var level = 1; level <= 7; level++) builder.AppendLine(new string(' ', level * 2) + "Cause " + level.ToString(System.Globalization.CultureInfo.InvariantCulture));

        var result = new MermaidParser().ParseIshikawa(builder.ToString());

        Assert(!result.HasErrors, "Mermaid Ishikawa parser should accept renderable two-space nested cause levels: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid Ishikawa parser should produce a document.");
        Assert(document.Root != null && document.Root.Children[0].Children[0].Children[0].Text == "Cause 3", "Mermaid Ishikawa parser should preserve hierarchy depth from indentation stack.");
    }
}
