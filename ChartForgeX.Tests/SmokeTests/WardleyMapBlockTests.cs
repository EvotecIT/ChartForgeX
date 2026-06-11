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

    private static void WardleyMapBlockRejectsInvalidOverlayCoordinates() {
        var noteMap = WardleyMapBlock.Create();
        noteMap.AddNode("A", "A", 0.5, 0.5);
        noteMap.AddNote("Bad note", double.NaN, 0.5);
        AssertThrows<InvalidOperationException>(() => noteMap.ToSvg(), "Wardley maps should reject invalid note coordinates.");

        var annotationMap = WardleyMapBlock.Create();
        annotationMap.AddNode("A", "A", 0.5, 0.5);
        annotationMap.AddAnnotation(1, "Bad annotation", 0.5, 2);
        AssertThrows<InvalidOperationException>(() => annotationMap.ToSvg(), "Wardley maps should reject invalid annotation coordinates.");

        var markerMap = WardleyMapBlock.Create();
        markerMap.AddNode("A", "A", 0.5, 0.5);
        markerMap.AddMarker("Bad marker", 0.5, double.PositiveInfinity, WardleyMapMarkerKind.Accelerator);
        AssertThrows<InvalidOperationException>(() => markerMap.ToSvg(), "Wardley maps should reject invalid marker coordinates.");
    }

    private static void WardleyMapPngStageLabelsUseCenteredSlots() {
        var source = System.IO.File.ReadAllText(System.IO.Path.Combine(FindRepositoryRoot(), "ChartForgeX", "VisualBlocks", "PngVisualBlockRenderer.WardleyMap.cs"));
        Assert(source.Contains("(index + 0.5) / stages.Count", StringComparison.Ordinal), "Wardley map PNG stage labels should use the same centered stage slots as SVG rendering.");
    }

    private static void WardleyMapRendersDashedAndFlowLinksAcrossSvgAndPng() {
        var map = WardleyMapBlock.Create()
            .WithTitle("Flow Map")
            .WithSize(640, 420);
        map.AddNode("Portal", "Portal", 0.80, 0.35);
        map.AddNode("API", "API", 0.70, 0.45);
        map.AddNode("Database", "Database", 0.55, 0.65);
        map.AddLink("Portal", "API", dashed: true);
        map.AddLink("API", "Database", flow: WardleyMapFlow.Forward);
        map.AddLink("Database", "Portal", flow: WardleyMapFlow.Bidirectional);

        var svg = map.ToSvg();
        var png = map.ToPng();

        Assert(svg.Contains("stroke-dasharray=\"5 5\"", StringComparison.Ordinal), "Wardley map SVG rendering should preserve dashed dependency links.");
        Assert(svg.Contains("data-cfx-role=\"wardley-flow-forward\"", StringComparison.Ordinal), "Wardley map SVG rendering should include forward flow hints.");
        Assert(svg.Contains("data-cfx-role=\"wardley-flow-backward\"", StringComparison.Ordinal), "Wardley map SVG rendering should include backward flow hints for bidirectional links.");
        Assert(png.Length > 64 && png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47, "Wardley map PNG rendering should emit a valid PNG with dashed and flow link styling.");

        var pngSource = System.IO.File.ReadAllText(System.IO.Path.Combine(FindRepositoryRoot(), "ChartForgeX", "VisualBlocks", "PngVisualBlockRenderer.WardleyMap.cs"));
        Assert(pngSource.Contains("DrawDashedLine(from.X, from.Y, to.X, to.Y", StringComparison.Ordinal), "Wardley map PNG rendering should preserve dashed dependency links.");
        Assert(pngSource.Contains("DrawWardleyFlowHint(canvas, link", StringComparison.Ordinal), "Wardley map PNG rendering should preserve flow hints.");
    }
}
