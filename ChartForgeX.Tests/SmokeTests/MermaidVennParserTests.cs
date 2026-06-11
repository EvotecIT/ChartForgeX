using System;
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
}
