using System;
using System.Text;
using ChartForgeX;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void MermaidParserParsesWardleyMapCoreSemantics() {
        const string source = @"wardley-beta
title Platform map
size [900, 600]
evolution Genesis -> Custom Built -> Product -> Commodity
anchor User [0.95, 0.05]
component Portal [0.80, 0.35] inertia
component API [0.70, 0.45] (build)
component Identity [0.55, 0.65] label [-12, 18]
User -> Portal
Portal +'uses'> API
API -.-> Identity;optional
evolve API 0.75
note ""Operational pressure"" [0.35, 0.75]
annotations [0.12, 0.88]
annotation 1, [0.60, 0.50] ""Improve automation""
accelerator Faster [0.42, 0.62]
deaccelerator Risk [0.48, 0.68]
pipeline API {
  component Build [0.30]
  component Buy [0.55]
}";

        var result = new MermaidParser().ParseWardley(source);

        Assert(!result.HasErrors, "Mermaid Wardley parser should parse core map semantics: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid Wardley parser should produce a document.");
        Assert(document.Kind == MermaidDiagramKind.Wardley && document.Header == "wardley-beta", "Mermaid Wardley parser should preserve the header.");
        Assert(document.Title == "Platform map" && document.CanvasWidth == 900 && document.CanvasHeight == 600, "Mermaid Wardley parser should preserve title and size statements.");
        Assert(document.Stages.Count == 4, "Mermaid Wardley parser should preserve evolution stages.");
        Assert(document.Nodes.Count == 4 && document.Nodes[0].Kind == WardleyMapNodeKind.Anchor, "Mermaid Wardley parser should parse anchors and components.");
        Assert(document.Nodes.Exists(node => node.Id == "Portal" && node.Inertia), "Mermaid Wardley parser should parse inertia markers.");
        Assert(document.Nodes.Exists(node => node.Id == "API" && node.Strategy == "build"), "Mermaid Wardley parser should parse source strategy decorators.");
        Assert(document.Links.Count == 3 && document.Links[1].Label == "uses" && document.Links[2].Dashed, "Mermaid Wardley parser should parse links, flow labels, and dashed links.");
        Assert(document.Evolutions.Count == 1 && document.Notes.Count == 1 && document.Annotations.Count == 1 && document.Markers.Count == 2, "Mermaid Wardley parser should parse map overlays.");
        Assert(document.Pipelines.Count == 1 && document.Pipelines[0].Components.Count == 2, "Mermaid Wardley parser should parse pipeline child components.");
    }

    private static void MermaidWardleyConvertsToVisualBlockArtifactAndRenders() {
        const string source = @"wardley-beta
title Platform map
anchor User [0.95, 0.05]
component Portal [0.80, 0.35]
component API [0.70, 0.45]
User -> Portal
Portal -> API
evolve API 0.75";

        var result = new MermaidParser().ParseWardley(source);
        Assert(!result.HasErrors, "Mermaid Wardley parser should parse renderable source: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid Wardley parser should produce a document.");
        var block = document.ToWardleyMapBlock(new MermaidWardleyRenderOptions { Id = "platform-map", Title = "Platform Map", Width = 840, Height = 520 });
        Assert(block.Title == "Platform Map", "Mermaid Wardley conversion should preserve caller-provided titles.");
        Assert(block.Nodes.Count == 3 && block.Links.Count == 2 && block.Evolutions.Count == 1, "Mermaid Wardley conversion should map source into a reusable Wardley map block.");

        var artifact = document.ToVisualArtifact(new MermaidWardleyRenderOptions { Id = "platform-map" });
        Assert(artifact.Kind == VisualArtifactKind.Mermaid, "Mermaid Wardley visual artifact should report Mermaid artifact kind.");
        Assert(artifact.Model is WardleyMapBlock, "Mermaid Wardley visual artifact should carry the Wardley map block model.");
        Assert(artifact.Metadata["mermaid.nodes"] == "3" && artifact.Metadata["mermaid.links"] == "2", "Mermaid Wardley artifacts should expose model counts.");
        Assert(artifact.Metadata["render.model"] == nameof(WardleyMapBlock), "Mermaid Wardley artifacts should expose the visual block render model.");

        var svg = document.ToSvg(new MermaidWardleyRenderOptions { Id = "platform-map" });
        var png = document.ToPng(new MermaidWardleyRenderOptions { Id = "platform-map" });
        Assert(svg.Contains("data-cfx-role=\"wardley-plot\"", StringComparison.Ordinal), "Mermaid Wardley SVG rendering should include the map plot.");
        Assert(svg.Contains("Platform map", StringComparison.Ordinal), "Mermaid Wardley SVG rendering should include title text.");
        Assert(png.Length > 64 && png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47, "Mermaid Wardley PNG rendering should emit a valid PNG.");
    }

    private static void MermaidWardleyUsesSourceSizeByDefault() {
        const string source = @"wardley-beta
size [1200, 800]
anchor User [0.95, 0.05]
component API [0.70, 0.45]
User -> API";

        var result = new MermaidParser().ParseWardley(source);

        Assert(!result.HasErrors, "Mermaid Wardley parser should parse source size statements: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid Wardley parser should produce a document.");
        var block = document.ToWardleyMapBlock();
        var artifact = document.ToVisualArtifact();
        var naturalSize = artifact.NaturalSize ?? throw new InvalidOperationException("Mermaid Wardley artifacts should expose a natural size.");
        Assert(block.Options.Size.Width == 1200 && block.Options.Size.Height == 800, "Mermaid Wardley conversion should use source size when render options do not override it.");
        Assert(naturalSize.Width == 1200 && naturalSize.Height == 800, "Mermaid Wardley artifacts should expose source size as their natural size by default.");
    }

    private static void MermaidWardleyRejectsLinksToUnknownNodes() {
        const string source = @"wardley-beta
component API [0.70, 0.45]
API -> Missing";

        var result = new MermaidParser().ParseWardley(source);

        Assert(result.HasErrors, "Mermaid Wardley parser should reject links to unknown nodes.");
        Assert(result.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("previously declared nodes", StringComparison.Ordinal)), "Mermaid Wardley link diagnostics should explain the current declaration contract.");
    }

    private static void MermaidWardleyAcceptsIntegerAndPercentageCoordinates() {
        const string source = @"wardley-beta
anchor User [1, 0]
component API [95, 5]
note ""Boundary"" [100, 0]
annotation 1, [60, 50] ""Improve automation""
accelerator Faster [42, 62]";

        var result = new MermaidParser().ParseWardley(source);

        Assert(!result.HasErrors, "Mermaid Wardley parser should accept integer and percentage-style coordinates: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid Wardley parser should produce a document.");
        Assert(document.Nodes.Count == 2 && document.Nodes[0].Visibility == 1 && document.Nodes[0].Evolution == 0, "Mermaid Wardley parser should normalize boundary integer coordinates.");
        Assert(Math.Abs(document.Nodes[1].Visibility - 0.95) < 0.001 && Math.Abs(document.Nodes[1].Evolution - 0.05) < 0.001, "Mermaid Wardley parser should normalize percentage coordinates.");
        Assert(document.Notes.Count == 1 && document.Annotations.Count == 1 && document.Markers.Count == 1, "Mermaid Wardley parser should allow integer coordinates for overlays.");
    }

    private static void MermaidWardleyPipelineReportsInvalidChildCoordinates() {
        const string source = @"wardley-beta
component Platform [0.70, 0.45]
pipeline Platform {
  component Build [150]
}";

        var result = new MermaidParser().ParseWardley(source);

        Assert(result.HasErrors, "Mermaid Wardley parser should report invalid pipeline child coordinates instead of throwing.");
        Assert(result.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("between 0-1 or 0-100", StringComparison.Ordinal)), "Mermaid Wardley invalid pipeline diagnostics should explain the coordinate range.");
        Assert(result.Document != null && result.Document.Pipelines.Count == 1 && result.Document.Pipelines[0].Components.Count == 0, "Mermaid Wardley parser should keep the pipeline and skip the invalid child component.");
    }

    private static void MermaidWardleyRejectsRenderLimitOverflowDuringParsing() {
        var nodes = new StringBuilder("wardley-beta\n");
        for (var index = 0; index <= 256; index++) nodes.Append("component C").Append(index).Append(" [0.5, 0.5]\n");

        var nodeResult = new MermaidParser().ParseWardley(nodes.ToString());

        Assert(nodeResult.HasErrors, "Mermaid Wardley parser should reject node counts beyond the renderer limit.");
        Assert(nodeResult.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("256", StringComparison.Ordinal) && diagnostic.Message.Contains("nodes", StringComparison.Ordinal)), "Oversized Wardley node diagnostics should name the renderable node cap.");

        var links = new StringBuilder("wardley-beta\ncomponent A [0.5, 0.4]\ncomponent B [0.4, 0.6]\n");
        for (var index = 0; index <= 512; index++) links.Append("A -> B\n");

        var linkResult = new MermaidParser().ParseWardley(links.ToString());

        Assert(linkResult.HasErrors, "Mermaid Wardley parser should reject link counts beyond the renderer limit.");
        Assert(linkResult.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("512", StringComparison.Ordinal) && diagnostic.Message.Contains("links", StringComparison.Ordinal)), "Oversized Wardley link diagnostics should name the renderable link cap.");
    }
}
