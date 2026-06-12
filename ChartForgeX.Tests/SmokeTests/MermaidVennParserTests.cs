using System;
using System.Text;
using ChartForgeX;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void MermaidParserParsesVennSetsUnionsTextAndStyles() {
        const string source = @"venn-beta
title Capability overlap
set API [""API""] : 60
set UI [""UI""] : 55
set Ops [""Operations""] : 45
union API,UI [""Shared UX""] : 18
union API,UI,Ops [""Platform""] : 5
text API,UI note [""Reviewed""]
style API fill:#E0F2FE,stroke:#0284C7,color:#0F172A";

        var result = new MermaidParser().ParseVenn(source);

        Assert(!result.HasErrors, "Mermaid Venn parser should parse renderable Venn source: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid Venn parser should produce a document.");
        Assert(document.Kind == MermaidDiagramKind.Venn && document.Header == "venn-beta", "Mermaid Venn parser should preserve the venn-beta header.");
        Assert(document.Title == "Capability overlap", "Mermaid Venn parser should preserve title statements.");
        Assert(document.Sets.Count == 3 && document.Intersections.Count == 2, "Mermaid Venn parser should parse sets and intersections.");
        Assert(document.TextNodes.Count == 1, "Mermaid Venn parser should parse text statements.");
        Assert(document.Sets[0].Fill.HasValue && document.Sets[0].Stroke.HasValue && document.Sets[0].TextColor.HasValue, "Mermaid Venn parser should apply renderable style colors.");
        Assert(document.Statements.Count == 8, "Mermaid Venn parser should retain raw body statements.");
    }

    private static void MermaidVennConvertsToVisualBlockArtifactAndRenders() {
        const string source = @"venn-beta
set API [""API""] : 60
set UI [""UI""] : 55
union API,UI [""Shared UX""] : 18";

        var result = new MermaidParser().ParseVenn(source);
        Assert(!result.HasErrors, "Mermaid Venn parser should parse renderable source: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid Venn parser should produce a document.");
        var block = document.ToVennDiagramBlock(new MermaidVennRenderOptions { Id = "capability-overlap", Title = "Capability Overlap", Width = 720, Height = 420 });
        Assert(block.Title == "Capability Overlap", "Mermaid Venn conversion should preserve caller-provided titles.");
        Assert(block.Sets.Count == 2 && block.Intersections.Count == 1, "Mermaid Venn conversion should map source into a reusable Venn block.");

        var artifact = document.ToVisualArtifact(new MermaidVennRenderOptions { Id = "capability-overlap" });
        Assert(artifact.Kind == VisualArtifactKind.Mermaid, "Mermaid Venn visual artifact should report Mermaid artifact kind.");
        Assert(artifact.Model is VennDiagramBlock, "Mermaid Venn visual artifact should carry the Venn block model.");
        Assert(artifact.Metadata["mermaid.sets"] == "2" && artifact.Metadata["mermaid.intersections"] == "1", "Mermaid Venn artifacts should expose model counts.");
        Assert(artifact.Metadata["render.model"] == nameof(VennDiagramBlock), "Mermaid Venn artifacts should expose the visual block render model.");

        var svg = document.ToSvg(new MermaidVennRenderOptions { Id = "capability-overlap" });
        var png = document.ToPng(new MermaidVennRenderOptions { Id = "capability-overlap" });
        Assert(svg.Contains("data-cfx-role=\"venn-set\"", StringComparison.Ordinal), "Mermaid Venn SVG rendering should include set circles.");
        Assert(svg.Contains("Shared UX", StringComparison.Ordinal), "Mermaid Venn SVG rendering should include intersection labels.");
        Assert(png.Length > 64 && png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47, "Mermaid Venn PNG rendering should emit a valid PNG.");
    }

    private static void MermaidVennRejectsUnknownUnionIds() {
        const string source = @"venn-beta
set API [""API""] : 60
union API,Missing [""Shared""] : 4";

        var result = new MermaidParser().ParseVenn(source);

        Assert(result.HasErrors, "Mermaid Venn parser should reject unknown union ids.");
        Assert(result.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("unknown set id", StringComparison.Ordinal)), "Mermaid Venn union errors should name unknown set id references.");
    }

    private static void MermaidVennStylesUnionTargetsIndependentOfOrderAndRenders() {
        const string source = @"venn-beta
set A [""A""] : 60
set B [""B""] : 55
union A,B [""Overlap""] : 20
style B,A fill:#FF0000,stroke:#990000,color:#111111";

        var result = new MermaidParser().ParseVenn(source);

        Assert(!result.HasErrors, "Mermaid Venn parser should match union style targets independent of set order: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid Venn parser should produce a document.");
        Assert(document.Intersections.Count == 1 && document.Intersections[0].Fill.HasValue && document.Intersections[0].Stroke.HasValue && document.Intersections[0].TextColor.HasValue, "Mermaid Venn parser should apply fill, stroke, and text styles to union targets.");
        var svg = document.ToSvg(new MermaidVennRenderOptions { Id = "styled-venn" });
        var png = document.ToPng(new MermaidVennRenderOptions { Id = "styled-venn" });
        Assert(svg.Contains("data-cfx-role=\"venn-intersection-style\"", StringComparison.Ordinal), "Mermaid Venn SVG rendering should make styled union fills and strokes visible.");
        Assert(svg.Contains("#990000", StringComparison.OrdinalIgnoreCase), "Mermaid Venn SVG rendering should include styled union stroke colors.");
        Assert(png.Length > 64 && png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47, "Mermaid Venn PNG rendering should still emit a valid PNG for styled unions.");
    }

    private static void MermaidVennRejectsMoreThanThreeSets() {
        const string source = @"venn-beta
set A [""A""] : 10
set B [""B""] : 10
set C [""C""] : 10
set D [""D""] : 10";

        var result = new MermaidParser().ParseVenn(source);

        Assert(result.HasErrors, "Mermaid Venn parser should reject unsupported four-set diagrams.");
        Assert(result.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("no more than three sets", StringComparison.Ordinal)), "Mermaid Venn parser should explain the renderable set limit.");
    }

    private static void MermaidVennPreservesQuotedIdentifiers() {
        const string source = @"venn-beta
set ""Foo Bar"" [""Foo""] : 10
set Baz [""Baz""] : 8
union ""Foo Bar"",Baz [""Overlap""] : 3";

        var result = new MermaidParser().ParseVenn(source);

        Assert(!result.HasErrors, "Mermaid Venn parser should preserve quoted set identifiers: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid Venn parser should produce a document.");
        Assert(document.Sets[0].Id == "Foo Bar" && document.Intersections[0].SetIds[0] == "Foo Bar", "Mermaid Venn parser should use unquoted identifiers consistently for sets and unions.");
    }

    private static void MermaidVennAcceptsIndentedTextNodes() {
        const string source = @"venn-beta
set A [""A""] : 10
  text [""Only A""]";

        var result = new MermaidParser().ParseVenn(source);

        Assert(!result.HasErrors, "Mermaid Venn parser should attach indented text nodes to the current region: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid Venn parser should produce a document.");
        Assert(document.TextNodes.Count == 1 && document.TextNodes[0].SetIds[0] == "A" && document.TextNodes[0].Label == "Only A", "Mermaid Venn parser should preserve indented text labels on the current region.");
    }

    private static void MermaidVennStylesTextNodeTargets() {
        const string source = @"venn-beta
set A [""A""] : 10
text A note [""Only A""]
style note color:#FF0000";

        var result = new MermaidParser().ParseVenn(source);

        Assert(!result.HasErrors, "Mermaid Venn parser should style text node targets: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid Venn parser should produce a document.");
        Assert(document.TextNodes.Count == 1 && document.TextNodes[0].TextColor.HasValue, "Mermaid Venn parser should apply color styles to text node targets.");
        var svg = document.ToSvg();
        Assert(svg.Contains("#FF0000", StringComparison.OrdinalIgnoreCase), "Mermaid Venn SVG rendering should include styled text node color.");
    }

    private static void MermaidVennRejectsRenderLimitOverflowDuringParsing() {
        var intersections = new StringBuilder("venn-beta\nset A [\"A\"] : 10\nset B [\"B\"] : 10\nset C [\"C\"] : 10\n");
        for (var index = 0; index <= 32; index++) intersections.Append("union A,B [\"AB\"] : 1\n");

        var intersectionResult = new MermaidParser().ParseVenn(intersections.ToString());

        Assert(intersectionResult.HasErrors, "Mermaid Venn parser should reject intersection counts beyond the renderer limit.");
        Assert(intersectionResult.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("32", StringComparison.Ordinal) && diagnostic.Message.Contains("intersections", StringComparison.Ordinal)), "Oversized Venn intersection diagnostics should name the renderable intersection cap.");

        var textNodes = new StringBuilder("venn-beta\nset A [\"A\"] : 10\n");
        for (var index = 0; index <= 64; index++) textNodes.Append("text A note").Append(index).Append(" [\"Only A\"]\n");

        var textResult = new MermaidParser().ParseVenn(textNodes.ToString());

        Assert(textResult.HasErrors, "Mermaid Venn parser should reject text node counts beyond the renderer limit.");
        Assert(textResult.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("64", StringComparison.Ordinal) && diagnostic.Message.Contains("text nodes", StringComparison.Ordinal)), "Oversized Venn text diagnostics should name the renderable text-node cap.");
    }
}
