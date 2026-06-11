using System;
using ChartForgeX;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void FishboneDiagramBlockRendersStaticSvgAndPng() {
        var block = FishboneDiagramBlock.Create("Delayed release")
            .WithTitle("Release Root Cause")
            .WithSize(840, 460);
        block.AddCause("People").AddChild("Handoffs").AddChild("Timezone");
        block.AddCause("Process").AddChild("Late review");
        block.AddCause("Platform").AddChild("Slow build");

        var svg = block.ToSvg();
        var png = block.ToPng();

        Assert(svg.Contains("data-cfx-role=\"fishbone-spine\"", StringComparison.Ordinal), "Fishbone SVG should expose the spine role.");
        Assert(svg.Contains("data-cfx-role=\"fishbone-branch\"", StringComparison.Ordinal), "Fishbone SVG should expose branch roles.");
        Assert(svg.Contains("Delayed release", StringComparison.Ordinal), "Fishbone SVG should render the effect label.");
        Assert(png.Length > 64 && png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47, "Fishbone PNG rendering should emit a valid PNG.");
    }

    private static void FishboneDiagramBlockRejectsEmptyEffect() {
        var block = FishboneDiagramBlock.Create("");
        block.AddCause("Process");

        AssertThrows<InvalidOperationException>(() => block.ToSvg(), "Fishbone diagrams should reject an empty effect instead of rendering a misleading artifact.");
    }
}
