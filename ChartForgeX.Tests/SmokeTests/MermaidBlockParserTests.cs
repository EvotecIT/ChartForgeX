using System;
using System.Text;
using ChartForgeX;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void MermaidParserParsesBlockLayoutsEdgesAndSpaces() {
        const string source = @"block-beta
columns 3
frontend[""Frontend""] api(""API"") database[(""Database"")]
space:1 worker:2
frontend --> api
api --> database";

        var result = new MermaidParser().ParseBlock(source);

        Assert(!result.HasErrors, "Mermaid block parser should parse renderable block layouts: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid block parser should produce a document.");
        Assert(document.Kind == MermaidDiagramKind.Block, "Mermaid block parser should produce a block document.");
        Assert(document.Columns == 3, "Mermaid block parser should preserve column statements.");
        Assert(document.Items.Count == 5, "Mermaid block parser should parse blocks and spaces.");
        Assert(document.Items[1].Shape == BlockLayoutShape.Rounded, "Mermaid block parser should preserve rounded shapes.");
        Assert(document.Items[2].Shape == BlockLayoutShape.Database, "Mermaid block parser should preserve database shapes.");
        Assert(document.Items[4].ColumnSpan == 2, "Mermaid block parser should preserve column spans.");
        Assert(document.Edges.Count == 2, "Mermaid block parser should parse basic edges.");
        Assert(document.Statements.Count == 5, "Mermaid block parser should retain raw body statements.");
    }

    private static void MermaidBlockConvertsToVisualBlockArtifactAndRenders() {
        const string source = @"block-beta
columns 2
a[""Frontend""] b[""API""]
c[""Worker""] d[""Queue""]
a --> b
b --> d";

        var result = new MermaidParser().ParseBlock(source);
        Assert(!result.HasErrors, "Mermaid block parser should parse renderable source: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid block parser should produce a document.");
        var block = document.ToBlockLayoutBlock(new MermaidBlockRenderOptions { Id = "service-path", Title = "Service Path", Width = 720, Height = 320 });
        Assert(block.Title == "Service Path", "Mermaid block conversion should preserve caller-provided titles.");
        Assert(block.Columns == 2 && block.Items.Count == 4 && block.Edges.Count == 2, "Mermaid block conversion should map source into a reusable block layout block.");

        var artifact = document.ToVisualArtifact(new MermaidBlockRenderOptions { Id = "service-path" });
        Assert(artifact.Kind == VisualArtifactKind.Mermaid, "Mermaid block visual artifact should report Mermaid artifact kind.");
        Assert(artifact.Model is BlockLayoutBlock, "Mermaid block visual artifact should carry the block layout model.");
        Assert(artifact.Metadata["mermaid.blocks"] == "4" && artifact.Metadata["mermaid.edges"] == "2", "Mermaid block artifacts should expose model counts.");
        Assert(artifact.Metadata["render.model"] == nameof(BlockLayoutBlock), "Mermaid block artifacts should expose the visual block render model.");

        var svg = document.ToSvg(new MermaidBlockRenderOptions { Id = "service-path" });
        var png = document.ToPng(new MermaidBlockRenderOptions { Id = "service-path" });
        Assert(svg.Contains("data-cfx-role=\"block-layout-node\"", StringComparison.Ordinal), "Mermaid block SVG rendering should include block nodes.");
        Assert(svg.Contains("data-cfx-role=\"block-layout-edge\"", StringComparison.Ordinal), "Mermaid block SVG rendering should include block edges.");
        Assert(png.Length > 64 && png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47, "Mermaid block PNG rendering should emit a valid PNG.");
    }

    private static void MermaidBlockWarnsForRetainedStyleStatements() {
        const string source = @"block-beta
columns 2
a b
style a fill:#f9f";

        var result = new MermaidParser().ParseBlock(source);

        Assert(!result.HasErrors, "Mermaid block styles should be retained as warnings, not hard errors.");
        Assert(result.Diagnostics.Exists(diagnostic => diagnostic.Severity == MermaidDiagnosticSeverity.Warning && diagnostic.Message.Contains("retained", StringComparison.Ordinal)), "Mermaid block parser should warn when style statements are not rendered yet.");
        var document = result.Document ?? throw new InvalidOperationException("Mermaid block parser should produce a document.");
        Assert(document.StyleStatements.Count == 1, "Mermaid block parser should retain style statements for future support.");
    }

    private static void MermaidBlockParsesIdsStartingWithSpaceAsBlocks() {
        const string source = @"block-beta
spaceService[""Space service""] space:2";

        var result = new MermaidParser().ParseBlock(source);

        Assert(!result.HasErrors, "Mermaid block parser should parse normal ids that start with space: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid block parser should produce a document.");
        Assert(document.Items.Count == 2, "Mermaid block parser should parse one visible block and one spacer.");
        Assert(!document.Items[0].IsSpace && document.Items[0].Id == "spaceService" && document.Items[0].Label == "Space service", "Mermaid block parser should only treat exact space tokens as spacers.");
        Assert(document.Items[1].IsSpace && document.Items[1].ColumnSpan == 2, "Mermaid block parser should still parse explicit space:num spacer tokens.");
    }

    private static void MermaidBlockIgnoresSpanSuffixesInsideQuotedLabels() {
        const string source = @"block-beta
service[""HTTP:2""]";

        var result = new MermaidParser().ParseBlock(source);

        Assert(!result.HasErrors, "Mermaid block parser should not treat label colons as span suffixes: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid block parser should produce a document.");
        Assert(document.Items.Count == 1 && document.Items[0].Label == "HTTP:2", "Mermaid block parser should preserve colon-number text inside quoted labels.");
        Assert(document.Items[0].ColumnSpan == 1, "Mermaid block parser should only consume column spans outside shape labels.");
    }

    private static void MermaidBlockIgnoresArrowsInsideQuotedLabels() {
        const string source = @"block-beta
api[""Calls --> service""]";

        var result = new MermaidParser().ParseBlock(source);

        Assert(!result.HasErrors, "Mermaid block parser should not treat label arrows as edge operators: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid block parser should produce a document.");
        Assert(document.Items.Count == 1 && document.Items[0].Id == "api" && document.Items[0].Label == "Calls --> service", "Mermaid block parser should preserve arrows inside quoted labels.");
        Assert(document.Edges.Count == 0, "Mermaid block parser should only parse edge operators outside shape labels.");
    }

    private static void MermaidBlockPreservesDeclarationsAfterImplicitEdgeEndpoints() {
        const string source = @"block-beta
A --> B
A[""Start""]
B((""Done"")):2";

        var result = new MermaidParser().ParseBlock(source);

        Assert(!result.HasErrors, "Mermaid block parser should preserve later explicit declarations after implicit edge endpoints: " + MermaidDiagnostics(result));
        Assert(!result.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("already declared", StringComparison.Ordinal)), "Mermaid block parser should replace implicit placeholders without duplicate warnings.");
        var document = result.Document ?? throw new InvalidOperationException("Mermaid block parser should produce a document.");
        Assert(document.Edges.Count == 1 && document.Edges[0].SourceId == "A" && document.Edges[0].TargetId == "B", "Mermaid block parser should retain edges declared before nodes.");
        Assert(document.Items.Count == 2 && document.Items[0].Label == "Start", "Mermaid block parser should replace implicit source nodes with their explicit metadata.");
        Assert(document.Items[1].Label == "Done" && document.Items[1].Shape == BlockLayoutShape.Circle && document.Items[1].ColumnSpan == 2, "Mermaid block parser should replace implicit target nodes with their explicit shape and span.");
    }

    private static void MermaidBlockRejectsRenderLimitOverflowDuringParsing() {
        var items = new StringBuilder("block-beta\n");
        for (var index = 0; index <= 10000; index++) items.Append('n').Append(index).Append(' ');

        var itemResult = new MermaidParser().ParseBlock(items.ToString());

        Assert(itemResult.HasErrors, "Mermaid block parser should reject item counts beyond the renderer limit.");
        Assert(itemResult.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("10000", StringComparison.Ordinal) && diagnostic.Message.Contains("items", StringComparison.Ordinal)), "Oversized block item diagnostics should name the renderable item cap.");

        var edges = new StringBuilder("block-beta\nA B\n");
        for (var index = 0; index <= 20000; index++) edges.Append("A --> B\n");

        var edgeResult = new MermaidParser().ParseBlock(edges.ToString());

        Assert(edgeResult.HasErrors, "Mermaid block parser should reject edge counts beyond the renderer limit.");
        Assert(edgeResult.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("20000", StringComparison.Ordinal) && diagnostic.Message.Contains("edges", StringComparison.Ordinal)), "Oversized block edge diagnostics should name the renderable edge cap.");
    }
}
