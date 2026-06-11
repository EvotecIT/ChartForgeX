using System;
using ChartForgeX;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void WardleyMapBlockRendersStaticSvgAndPng() {
        var map = WardleyMapBlock.Create()
            .WithTitle("Platform Map")
            .WithSize(900, 560);
        map.SetStages(new[] { "Genesis", "Custom", "Product", "Commodity" });
        map.AddNode("User", "User", 0.95, 0.05, WardleyMapNodeKind.Anchor);
        map.AddNode("Portal", "Portal", 0.80, 0.35).Inertia = true;
        map.AddNode("API", "API", 0.70, 0.45).Strategy = "build";
        map.AddLink("User", "Portal");
        map.AddLink("Portal", "API", "uses", dashed: false, WardleyMapFlow.Forward);
        map.AddEvolution("API", 0.75);
        map.AddNote("Operational pressure", 0.35, 0.75);

        var svg = map.ToSvg();
        var png = map.ToPng();

        Assert(svg.Contains("data-cfx-role=\"wardley-plot\"", StringComparison.Ordinal), "Wardley map SVG should expose the plot role.");
        Assert(svg.Contains("data-cfx-role=\"wardley-node\"", StringComparison.Ordinal), "Wardley map SVG should expose node roles.");
        Assert(svg.Contains("Platform Map", StringComparison.Ordinal), "Wardley map SVG should render the title.");
        Assert(png.Length > 64 && png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47, "Wardley map PNG rendering should emit a valid PNG.");
    }

    private static void WardleyMapBlockRejectsLinksToUnknownNodes() {
        var map = WardleyMapBlock.Create();
        map.AddNode("A", "A", 0.5, 0.5);
        map.AddLink("A", "B");

        AssertThrows<InvalidOperationException>(() => map.ToSvg(), "Wardley maps should reject links to unknown nodes instead of rendering misleading dependencies.");
    }
}
