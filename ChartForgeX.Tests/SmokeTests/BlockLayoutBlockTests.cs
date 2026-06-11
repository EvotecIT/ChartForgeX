using System;
using ChartForgeX;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void BlockLayoutBlockRendersStaticSvgAndPng() {
        var block = BlockLayoutBlock.Create()
            .WithTitle("Service Path")
            .WithSize(720, 320)
            .WithColumns(3)
            .AddItem("frontend", "Frontend")
            .AddItem("api", "API", 1, BlockLayoutShape.Rounded)
            .AddItem("database", "Database", 1, BlockLayoutShape.Database)
            .AddEdge("frontend", "api")
            .AddEdge("api", "database");

        var svg = block.ToSvg();
        var png = block.ToPng();

        Assert(svg.Contains("data-cfx-role=\"block-layout-node\"", StringComparison.Ordinal), "Block layout SVG should expose node roles.");
        Assert(svg.Contains("data-cfx-role=\"block-layout-edge\"", StringComparison.Ordinal), "Block layout SVG should expose edge roles.");
        Assert(svg.Contains("data-block-id=\"database\"", StringComparison.Ordinal), "Block layout SVG should expose block ids.");
        Assert(png.Length > 64 && png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47, "Block layout PNG rendering should emit a valid PNG.");
    }

    private static void BlockLayoutBlockRejectsUnknownEdges() {
        var block = BlockLayoutBlock.Create()
            .AddItem("a", "A")
            .AddEdge("a", "missing");

        AssertThrows<InvalidOperationException>(() => block.ToSvg(), "Block layout edges should reject missing endpoints instead of silently dropping them.");
    }
}
