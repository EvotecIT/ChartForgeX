using System;
using ChartForgeX;
using ChartForgeX.Composition;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

internal static class Program {
    private static void Main() {
        PackageAssertions.CoreFixtures();
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
        PackageAssertions.Png(composition.ToPng());
        PackageAssertions.Require(composition.ToImage().Pixels[3] == 0, "Composition lost its transparent edge.");
        var canvas = VisualCanvas.Create(340, 220).WithBackground(ChartColor.Transparent)
            .AddText(12, 14, 300, "Packed canvas", 20, ChartColors.DarkGreen);
        PackageAssertions.Require(PackageAssertions.Contains(canvas.ToSvg(), "Packed canvas"), "Canvas text is missing.");
        PackageAssertions.Png(canvas.ToPng());
        var metric = MetricCard.Create().WithMetric("Ready", 98, "P0").WithSize(320, 170);
        PackageAssertions.Png(metric.ToPng());
        var list = ChartList.Create().AddItem("Ready");
        PackageAssertions.Png(list.ToPng());
        var table = TableArtifact.Create("packed-table").AddColumn("state", "State").AddRow("one", "Ready");
        var artifact = table.ToVisualArtifact();
        var text = VisualWatermark.FromText("First");
        var artwork = VisualWatermark.FromImage(new RgbaImage(1, 1, new byte[] { 20, 80, 120, 128 }).ToPng(), "image/png");
        artifact.WithWatermarks(text, artwork);
        var svg = artifact.ToSvg();
        PackageAssertions.Require(PackageAssertions.Contains(svg, "First"), "Text watermark is missing.");
        PackageAssertions.Require(PackageAssertions.Contains(svg, "data:image/png"), "Image watermark is missing.");
        PackageAssertions.Png(artifact.ToPng());
        PackageAssertions.Payload("ChartForgeX", "ChartForgeX.Visuals");
        Console.WriteLine("Visuals package boundaries, composition, factual content and watermark exports passed.");
    }
}
