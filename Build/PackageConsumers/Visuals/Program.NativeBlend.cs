using System;
using ChartForgeX.Composition;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

internal static partial class Program {
    private static void NativeBlendContracts() {
        var source = PackageAssertions.Chart().Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(320, 200), 16),
            frame: new VisualFrame(showLegend: false, transparentBackground: true)));
        var mask = new VisualMask(ImageComposition.Create(320, 200, ChartColor.FromRgba(0, 0, 0, 128)).ToImage(), orderedDither: false);
        var layer = source.CaptureLayer(mask, new VisualRenderOptions(supersampling: 1));
        foreach (var mode in (VisualBlendMode[])Enum.GetValues(typeof(VisualBlendMode))) {
            var report = ImageComposition.Create(352, 232, ChartColor.FromRgba(20, 60, 120, 128)).DrawLayer(layer, 16, 16, mode, orderedDither: false);
            PackageAssertions.Png(report.ToPng()); PackageAssertions.Require(report.ToImage().Pixels[3] == 128, "Explicit blend changed exterior backdrop.");
        }
        var original = layer.ToImage().Pixels; var canceled = ImageComposition.CreateTransparent(352, 232); var before = (byte[])canceled.ToImage().Pixels.Clone();
        try { canceled.DrawLayer(layer, 16, 16, VisualBlendMode.Screen, cancellationToken: new System.Threading.CancellationToken(true));
            throw new InvalidOperationException("Canceled blend committed."); }
        catch (OperationCanceledException) { }
        PackageAssertions.Require(System.Linq.Enumerable.SequenceEqual(before, canceled.ToImage().Pixels)
            && System.Linq.Enumerable.SequenceEqual(original, layer.ToImage().Pixels), "Canceled blending changed an owner.");
    }
}
