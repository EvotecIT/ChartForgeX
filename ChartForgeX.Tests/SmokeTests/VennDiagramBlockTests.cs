using System;
using ChartForgeX;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void VennDiagramBlockRendersStaticSvgAndPng() {
        var block = VennDiagramBlock.Create()
            .WithTitle("Capability Overlap")
            .WithSize(720, 420)
            .AddSet("API", "API", 60)
            .AddSet("UI", "UI", 55)
            .AddSet("Ops", "Ops", 45)
            .AddIntersection(new[] { "API", "UI" }, "Shared UX", 18)
            .AddIntersection(new[] { "API", "UI", "Ops" }, "Platform", 5);

        var svg = block.ToSvg();
        var png = block.ToPng();

        Assert(svg.Contains("data-cfx-role=\"venn-set\"", StringComparison.Ordinal), "Venn diagram SVG should expose set roles.");
        Assert(svg.Contains("data-set-id=\"API\"", StringComparison.Ordinal), "Venn diagram SVG should expose set ids.");
        Assert(svg.Contains("Shared UX", StringComparison.Ordinal), "Venn diagram SVG should render intersection labels.");
        Assert(png.Length > 64 && png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47, "Venn diagram PNG rendering should emit a valid PNG.");
    }

    private static void VennDiagramBlockRejectsUnknownIntersections() {
        var block = VennDiagramBlock.Create()
            .AddSet("A", "A")
            .AddIntersection(new[] { "A", "B" }, "Missing");

        AssertThrows<InvalidOperationException>(() => block.ToSvg(), "Venn diagram intersections should reject unknown set ids instead of silently dropping labels.");
    }
}
