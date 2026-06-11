using System;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void MermaidParserParsesEventModelingTimeline() {
        const string source = @"eventmodeling
tf 01 ui CartUI `json`{ ""cartId"": 42 }
tf 02 cmd AddItem
tf 03 evt Cart.ItemAdded [[ItemAdded01]]
tf 04 rmo Cart.CartView ->> 03
rf 05 pcr InventoryProcessor
timeframe 06 event Inventory.Reserved
data ItemAdded01 `json`{
  ""sku"": ""ABC"",
  ""quantity"": 1
}";

        var result = new MermaidParser().ParseEventModeling(source);

        Assert(!result.HasErrors, "Mermaid Event Modeling parser should parse timeline semantics: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid Event Modeling parser should produce a document.");
        Assert(document.Kind == MermaidDiagramKind.EventModeling && document.Frames.Count == 6, "Mermaid Event Modeling parser should preserve the header and frames.");
        Assert(document.Frames[0].EntityKind == MermaidEventModelingEntityKind.Ui && document.Frames[0].InlineData != null && document.Frames[0].DataType == "json", "Mermaid Event Modeling parser should parse inline data.");
        Assert(document.Frames[2].Namespace == "Cart" && document.Frames[2].Name == "ItemAdded" && document.Frames[2].DataReference == "ItemAdded01", "Mermaid Event Modeling parser should parse namespaces and data refs.");
        Assert(document.Frames[4].IsReset && document.Relations.Count == 3, "Mermaid Event Modeling parser should parse reset frames and inferred relations.");
        Assert(document.Relations.Exists(relation => relation.SourceNumber == "03" && relation.TargetNumber == "04" && !relation.IsInferred), "Mermaid Event Modeling parser should parse explicit relations.");
        Assert(document.DataBlocks.Count == 1 && document.DataBlocks[0].Id == "ItemAdded01", "Mermaid Event Modeling parser should parse data blocks.");
    }

    private static void MermaidEventModelingConvertsToTopologyArtifactAndRenders() {
        const string source = @"eventmodeling
tf 01 ui CartUI
tf 02 cmd AddItem
tf 03 evt ItemAdded
tf 04 rmo CartView ->> 03";

        var result = new MermaidParser().ParseEventModeling(source);
        Assert(!result.HasErrors, "Mermaid Event Modeling parser should parse renderable source: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid Event Modeling parser should produce a document.");
        var topology = document.ToTopologyChart(new MermaidTopologyRenderOptions { Id = "cart-event-model", Title = "Cart Event Model", Width = 960, Height = 560 });
        Assert(topology.Id == "cart-event-model" && topology.Nodes.Count == 4 && topology.Edges.Count == 3, "Mermaid Event Modeling conversion should map frames to topology nodes and relations.");

        var artifact = document.ToVisualArtifact(new MermaidTopologyRenderOptions { Id = "cart-event-model" });
        Assert(artifact.Kind == VisualArtifactKind.Mermaid, "Mermaid Event Modeling visual artifact should report Mermaid artifact kind.");
        Assert(artifact.Model is TopologyChart, "Mermaid Event Modeling visual artifact should carry a topology model.");
        Assert(artifact.Metadata["mermaid.frames"] == "4" && artifact.Metadata["mermaid.relations"] == "3", "Mermaid Event Modeling artifacts should expose model counts.");
        Assert(artifact.Metadata["render.model"] == nameof(TopologyChart), "Mermaid Event Modeling artifacts should expose the topology render model.");

        var svg = document.ToSvg(new MermaidTopologyRenderOptions { Id = "cart-event-model", Title = "Cart Event Model" });
        var png = document.ToPng(new MermaidTopologyRenderOptions { Id = "cart-event-model" });
        Assert(svg.Contains("Cart Event Model", StringComparison.Ordinal) && svg.Contains("CartUI", StringComparison.Ordinal), "Mermaid Event Modeling SVG rendering should include timeline labels.");
        Assert(png.Length > 64 && png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47, "Mermaid Event Modeling PNG rendering should emit a valid PNG.");
    }

    private static void MermaidEventModelingRejectsUnknownRelationReferences() {
        const string source = @"eventmodeling
tf 01 ui CartUI
tf 02 rmo CartView ->> 99";

        var result = new MermaidParser().ParseEventModeling(source);

        Assert(result.HasErrors, "Mermaid Event Modeling parser should reject explicit relations to unknown timeframes.");
        Assert(result.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("existing timeframe", StringComparison.Ordinal)), "Mermaid Event Modeling relation diagnostics should explain the timeframe contract.");
    }

    private static void MermaidEventModelingRejectsUnknownEntityTypesWithoutThrowing() {
        const string source = @"eventmodeling
tf 01 widget Checkout";

        var result = new MermaidParser().ParseEventModeling(source);

        Assert(result.HasErrors, "Mermaid Event Modeling parser should reject unknown entity types.");
        Assert(result.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("entity type is not recognized", StringComparison.Ordinal)), "Mermaid Event Modeling unknown entity diagnostics should explain the invalid type.");
        Assert(result.Document != null && result.Document.Frames.Count == 0, "Mermaid Event Modeling parser should skip invalid frames without throwing or adding null frame entries.");
    }

    private static void MermaidEventModelingPreservesInlineDataBeforeReferences() {
        const string source = @"eventmodeling
tf 01 evt CustomerUpdated `json`{ ""field"": ""[[customerId]]"" } [[Payload01]]";

        var result = new MermaidParser().ParseEventModeling(source);

        Assert(!result.HasErrors, "Mermaid Event Modeling parser should preserve inline data that contains reference-looking text: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid Event Modeling parser should produce a document.");
        Assert(document.Frames.Count == 1, "Mermaid Event Modeling parser should retain the frame.");
        var frame = document.Frames[0];
        var inlineData = frame.InlineData ?? throw new InvalidOperationException("Mermaid Event Modeling parser should preserve inline data.");
        Assert(inlineData.Contains("[[customerId]]", StringComparison.Ordinal), "Mermaid Event Modeling parser should not remove data-reference-looking text from inline data.");
        Assert(frame.DataReference == "Payload01", "Mermaid Event Modeling parser should still parse suffix data references outside inline data.");
    }

    private static void MermaidEventModelingPreservesNestedDataBlockBraces() {
        const string source = @"eventmodeling
tf 01 evt CustomerUpdated [[Payload01]]
data Payload01 `json`{
  {
    ""customer"": {
      ""id"": 42
    }
  }
}";

        var result = new MermaidParser().ParseEventModeling(source);

        Assert(!result.HasErrors, "Mermaid Event Modeling parser should preserve nested data block braces: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid Event Modeling parser should produce a document.");
        Assert(document.DataBlocks.Count == 1, "Mermaid Event Modeling parser should keep nested data as a single data block.");
        var content = document.DataBlocks[0].Content;
        Assert(content.Contains(@"""customer"": {", StringComparison.Ordinal) && content.Contains(@"""id"": 42", StringComparison.Ordinal), "Mermaid Event Modeling parser should keep nested object content.");
        Assert(content.TrimEnd().EndsWith("}", StringComparison.Ordinal), "Mermaid Event Modeling parser should keep the inner closing brace while excluding the outer data block brace.");
    }
}
