using System;
using ChartForgeX;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void PacketLayoutBlockRendersStaticSvgAndPng() {
        var block = PacketLayoutBlock.Create()
            .WithTitle("TCP Header")
            .WithSize(720, 260)
            .WithBitsPerRow(32)
            .AddField(0, 15, "Source Port")
            .AddField(16, 31, "Destination Port")
            .AddField(32, 63, "Sequence Number");

        var svg = block.ToSvg();
        var png = block.ToPng();

        Assert(svg.Contains("data-cfx-role=\"packet-field\"", StringComparison.Ordinal), "Packet layout SVG should expose packet field roles.");
        Assert(svg.Contains("data-bit-start=\"16\"", StringComparison.Ordinal), "Packet layout SVG should expose bit start metadata.");
        Assert(svg.Contains("Destination Port", StringComparison.Ordinal), "Packet layout SVG should render field labels.");
        Assert(png.Length > 64 && png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47, "Packet layout PNG rendering should emit a valid PNG.");
    }

    private static void PacketLayoutBlockRejectsNonContiguousFields() {
        var block = PacketLayoutBlock.Create()
            .AddField(0, 7, "First")
            .AddField(16, 23, "Gap");

        AssertThrows<InvalidOperationException>(() => block.ToSvg(), "Packet layout blocks should reject gaps instead of silently hiding missing bits.");
    }

    private static void PacketLayoutBlockRejectsOversizedTotalBits() {
        var block = PacketLayoutBlock.Create()
            .WithBitsPerRow(1)
            .AddField(0, VisualBlockRendering.MaximumPacketBits, "Too large");

        AssertThrows<InvalidOperationException>(() => block.ToSvg(), "Packet layout blocks should reject oversized total bit lengths before materializing slices.");
    }
}
