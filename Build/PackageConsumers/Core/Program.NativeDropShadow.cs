using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

internal static partial class Program {
    private static void NativeDropShadowContracts() {
        var chart = Chart.Create().WithSize(320, 240).WithTitle("Finished requests").WithXAxis("Cycle").WithYAxis("Finished")
            .AddLine("Finished", new[] { new ChartPoint(1, 9), new ChartPoint(2, 24), new ChartPoint(3, 17) });
        var source = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(320, 240)),
            frame: new VisualFrame(showLegend: false, transparentBackground: true)));
        var before = source.ToSvg();
        var layer = source.CaptureLayer(new VisualDropShadow(ChartColor.FromRgba(0, 0, 0, 112), -3, 5, 2, spread: 1));
        var svg = layer.Visual.ToSvg(); var png = layer.Visual.ToPng();
        PackageAssertions.Require(ReferenceEquals(source, layer.Source) && source.ToSvg() == before, "Native shadow mutated its prepared source.");
        PackageAssertions.Require(layer.Bounds.X < 0 && layer.Bounds.Width > 320 && layer.Bounds.Height > 240, "Native shadow discarded propagated union support.");
        PackageAssertions.Require(svg.Contains("data:image/png;base64,") && !svg.Contains("<filter"), "Native shadow confused baked output with an SVG filter profile.");
        var image = layer.ToImage(); Array.Clear(image.Pixels, 0, image.Pixels.Length);
        PackageAssertions.Require(layer.ToImage().Pixels.Any(value => value > 0) && svg == layer.Visual.ToSvg(), "Native shadow exposed its private snapshot pixels.");
        PackageAssertions.Png(png);
    }
}
