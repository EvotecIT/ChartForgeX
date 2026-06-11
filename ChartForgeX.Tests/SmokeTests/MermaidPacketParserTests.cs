using System;
using ChartForgeX;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void MermaidParserParsesPacketFieldsAndRelativeLengths() {
        const string source = @"packet-beta
title TCP Header
0-15: ""Source Port""
+16: ""Destination Port""
32-63: ""Sequence Number""
+32: ""Acknowledgment Number""";

        var result = new MermaidParser().ParsePacket(source);

        Assert(!result.HasErrors, "Mermaid packet parser should parse ranges and relative bit lengths: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid packet parser should produce a document.");
        Assert(document.Kind == MermaidDiagramKind.Packet, "Mermaid packet parser should produce a packet document.");
        Assert(document.Title == "TCP Header", "Mermaid packet parser should preserve title statements.");
        Assert(document.Fields.Count == 4, "Mermaid packet parser should parse packet fields.");
        Assert(document.Fields[1].StartBit == 16 && document.Fields[1].EndBit == 31, "Mermaid packet parser should expand relative bit lengths from the expected next bit.");
        Assert(document.Fields[3].StartBit == 64 && document.Fields[3].EndBit == 95, "Mermaid packet parser should continue relative bit fields across rows.");
        Assert(document.Statements.Count == 5, "Mermaid packet parser should retain raw packet statements.");
    }

    private static void MermaidPacketConvertsToVisualBlockArtifactAndRenders() {
        const string source = @"packet-beta
0-3: ""Version""
+4: ""IHL""
+8: ""DSCP/ECN""
+16: ""Total Length""";

        var result = new MermaidParser().ParsePacket(source);
        Assert(!result.HasErrors, "Mermaid packet parser should parse renderable source: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid packet parser should produce a document.");
        var block = document.ToPacketLayoutBlock(new MermaidPacketRenderOptions { Id = "ipv4-header", Title = "IPv4 Header", Width = 720, Height = 260 });
        Assert(block.Title == "IPv4 Header", "Mermaid packet conversion should preserve caller-provided titles.");
        Assert(block.Fields.Count == 4 && block.Fields[2].StartBit == 8, "Mermaid packet conversion should map fields into a reusable packet layout block.");

        var artifact = document.ToVisualArtifact(new MermaidPacketRenderOptions { Id = "ipv4-header" });
        Assert(artifact.Kind == VisualArtifactKind.Mermaid, "Mermaid packet visual artifact should report Mermaid artifact kind.");
        Assert(artifact.Model is PacketLayoutBlock, "Mermaid packet visual artifact should carry the packet layout model.");
        Assert(artifact.Metadata["mermaid.fields"] == "4" && artifact.Metadata["mermaid.bits"] == "32", "Mermaid packet artifacts should expose model counts.");
        Assert(artifact.Metadata["render.model"] == nameof(PacketLayoutBlock), "Mermaid packet artifacts should expose the visual block render model.");

        var svg = document.ToSvg(new MermaidPacketRenderOptions { Id = "ipv4-header" });
        var png = document.ToPng(new MermaidPacketRenderOptions { Id = "ipv4-header" });
        Assert(svg.Contains("data-cfx-role=\"packet-field\"", StringComparison.Ordinal), "Mermaid packet SVG rendering should include packet fields.");
        Assert(png.Length > 64 && png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47, "Mermaid packet PNG rendering should emit a valid PNG.");
    }

    private static void MermaidParserRejectsNonContiguousPacketFields() {
        const string source = @"packet-beta
0-7: ""First""
16-23: ""Gap""";

        var result = new MermaidParser().ParsePacket(source);

        Assert(result.HasErrors, "Mermaid packet parser should reject non-contiguous packet fields.");
        Assert(result.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("contiguous", StringComparison.Ordinal)), "Invalid packet diagnostics should explain the contiguous-bit contract.");
    }

    private static void MermaidParserRejectsOversizedPacketRanges() {
        const string source = @"packet-beta
0-2147483647: ""payload""";

        var result = new MermaidParser().ParsePacket(source);

        Assert(result.HasErrors, "Mermaid packet parser should reject oversized explicit bit ranges.");
        Assert(result.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("too large", StringComparison.Ordinal)), "Oversized packet diagnostics should explain the bit-range guard.");
        Assert(result.Document != null && result.Document.Fields.Count == 0, "Mermaid packet parser should not retain oversized packet fields.");
    }

    private static void MermaidParserRejectsOversizedPacketTotals() {
        const string source = @"packet-beta
+10000: ""first""
+1: ""second""";

        var result = new MermaidParser().ParsePacket(source);

        Assert(result.HasErrors, "Mermaid packet parser should reject oversized total bit lengths.");
        Assert(result.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("total bit length", StringComparison.Ordinal)), "Oversized packet total diagnostics should explain the total-bit guard.");
        Assert(result.Document != null && result.Document.Fields.Count == 1, "Mermaid packet parser should keep valid prefix fields and reject only the field that exceeds the total bit cap.");
    }
}
