using System;
using ChartForgeX;
using ChartForgeX.Composition;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;
using ChartForgeX.Themes;

internal static partial class Program {
    private static void Main() {
        PackageAssertions.CoreFixtures();
        FiniteCompositeContracts();
        AppearanceCompositionContracts();
        NativeBlendContracts();
        PackageAssertions.Owner(typeof(VisualCanvas), "ChartForgeX.Visuals");
        PackageAssertions.Owner(typeof(ImageComposition), "ChartForgeX.Visuals");
        PackageAssertions.Owner(typeof(ChartTable), "ChartForgeX.Visuals");
        PackageAssertions.Owner(typeof(VisualWatermark), "ChartForgeX.Visuals");
        PackageAssertions.References(typeof(VisualCanvas).Assembly, "ChartForgeX");

        var chart = PackageAssertions.Chart();
        var image = chart.ToRgbaImage();
        var composition = ImageComposition.CreateTransparent(340, 220)
            .DrawImage(image, 10, 10, 320, 200, VisualCanvasImageFit.Contain)
            .DrawText(12, 12, 150, "Packed composition", 12, ChartColors.DarkGreen);
        PackageAssertions.Raster(composition.ToPng());
        PackageAssertions.Require(composition.ToRgbaImage().Pixels[3] == 0, "Composition lost its transparent edge.");
        var canvas = VisualCanvas.Create(340, 220).BackgroundTop(ChartColor.Transparent).BackgroundBottom(ChartColor.Transparent)
            .AddText(12, 14, 300, "Packed canvas", 20, ChartColors.DarkGreen);
        PackageAssertions.Require(PackageAssertions.Contains(canvas.ToSvg(), "Packed canvas"), "Canvas text is missing.");
        PackageAssertions.Raster(canvas.ToPng());
        var shared = VisualTheme.Graphite().Configure(builder => builder.Dark.Foreground = ChartColor.FromHex("#00FE7F"));
        var themedCanvas = VisualCanvas.Create(340, 120).Theme(shared).ThemeMode(ThemeMode.Dark)
            .BackdropStyle(VisualCanvasBackdropStyle.Transparent).AddText(12, 14, 300, "Shared theme", 24);
        PackageAssertions.Require(ReferenceEquals(shared, themedCanvas.Theme)
            && PackageAssertions.Contains(themedCanvas.ToSvg(), "#00FE7F"), "Canvas lost its shared dark foreground.");
        var darkPixels = themedCanvas.RenderRgba().Pixels;
        var foundForeground = false;
        for (var offset = 0; offset < darkPixels.Length; offset += 4)
            if (darkPixels[offset] == 0 && darkPixels[offset + 1] == 254 && darkPixels[offset + 2] == 127 && darkPixels[offset + 3] == 255) foundForeground = true;
        PackageAssertions.Require(foundForeground, "Canvas native text did not paint the shared foreground.");
        themedCanvas.ThemeMode(ThemeMode.Light);
        PackageAssertions.Require(!PackageAssertions.Contains(themedCanvas.ToSvg(), "#00FE7F"), "Canvas did not switch to the explicit light mode.");
        var metric = MetricCard.Create().Metric("Ready", 98, "P0").Size(320, 170);
        PackageAssertions.Raster(metric.ToPng());
        var list = ChartList.Create().AddItem("Ready");
        PackageAssertions.Raster(list.ToPng());
        var table = TableArtifact.Create("packed-table").AddColumn("state", "State").AddRow("one", "Ready");
        var artifact = table.ToVisualArtifact();
        var text = VisualWatermark.FromText("First");
        var artwork = VisualWatermark.FromImage(new RgbaImage(1, 1, new byte[] { 20, 80, 120, 128 }).ToPng(), "image/png");
        artifact.AddWatermarks(text, artwork);
        var svg = artifact.ToSvg();
        PackageAssertions.Require(PackageAssertions.Contains(svg, "First"), "Text watermark is missing.");
        PackageAssertions.Require(PackageAssertions.Contains(svg, "data:image/png"), "Image watermark is missing.");
        PackageAssertions.Raster(artifact.ToPng());
        PackageAssertions.Payload("ChartForgeX", "ChartForgeX.Visuals");
        Console.WriteLine("Visuals package boundaries, composition, factual content and watermark exports passed.");
    }
}
