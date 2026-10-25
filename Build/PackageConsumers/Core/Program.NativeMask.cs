using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

internal static partial class Program {
    private static void NativeMaskContracts() {
        var source = Chart.Create().WithSize(320, 200).AddLine("Ready", new[] { new ChartPoint(0, 1), new ChartPoint(1, 3) })
            .Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(320, 200), 16),
                frame: new VisualFrame(showLegend: false, transparentBackground: true)));
        var options = new VisualRenderOptions(supersampling: 1); var original = source.ToRgba(options); var before = source.ToSvg();
        var bytes = new byte[320 * 200 * 4]; for (var at = 3; at < bytes.Length; at += 4) bytes[at] = 128;
        var mask = new VisualMask(new RgbaImage(320, 200, bytes), orderedDither: false);
        Array.Clear(bytes, 0, bytes.Length);
        var layer = source.CaptureLayer(mask, options); var actual = layer.ToImage();
        var positive = false;
        for (var at = 0; at < actual.Pixels.Length; at += 4) if (actual.Pixels[at + 3] > 0) {
            positive = true; PackageAssertions.Require(actual.Pixels[at] == original.Pixels[at]
                && actual.Pixels[at + 1] == original.Pixels[at + 1] && actual.Pixels[at + 2] == original.Pixels[at + 2], "Mask changed unassociated RGB.");
        }
        PackageAssertions.Require(positive && ReferenceEquals(source, layer.Source) && before == source.ToSvg(), "Mask lost source ownership.");
        var getter = mask.ToImage(); Array.Clear(getter.Pixels, 0, getter.Pixels.Length);
        PackageAssertions.Require(mask.ToImage().Pixels.Any(value => value > 0), "Mask exposed mutable snapshot pixels.");
        var again = layer.WithMask(mask); PackageAssertions.Require(again.Bounds.Equals(layer.Bounds), "Mask changed captured bounds.");
        PackageAssertions.Png(layer.Visual.ToPng());
    }
}
