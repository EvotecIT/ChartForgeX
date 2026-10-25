using System;
using System.Linq;
using ChartForgeX.Composition;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

internal static partial class Program {
    private static void AppearanceCompositionContracts() {
        var image = ImageComposition.CreateTransparent(32, 32)
            .FillRectangle(8, 8, 16, 16, ChartColor.FromRgba(255, 0, 0, 128));
        var before = image.ToImage();
        var coverage = new VisualMask(new RgbaImage(32, 32, new byte[32 * 32 * 4]));
        image.BlurRegion(coverage, new VisualLayerBlur(2));
        PackageAssertions.Require(before.Pixels.SequenceEqual(image.ToImage().Pixels), "Zero coverage changed the explicit backdrop.");
        image.Outline(new VisualOutline(ChartColor.Black, 1)).Bloom(new VisualBloom(1, .1, .4));
        PackageAssertions.Require(!before.Pixels.SequenceEqual(image.ToImage().Pixels), "Explicit appearance composition painted no effect.");
        PackageAssertions.Png(image.ToPng());
    }
}
