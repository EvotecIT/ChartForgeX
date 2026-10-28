using System;
using System.IO;
using System.Linq;
using ChartForgeX;
using ChartForgeX.Effects;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeStandardBlendExampleTests {
    [Theory]
    [InlineData("compact", VisualThemeMode.Light)]
    [InlineData("compact", VisualThemeMode.Dark)]
    [InlineData("wide", VisualThemeMode.Light)]
    [InlineData("wide", VisualThemeMode.Dark)]
    public void CompleteProgramsUseTheirDeclaredCoreOrOptionalVisualsBoundary(string variant, VisualThemeMode mode) {
        var core = ExampleRecipeSource.EmitNativeMask(variant, mode);
        var composition = ExampleRecipeSource.EmitNativeMaskComposition(variant, mode);
        var directory = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "standard-blend-" + variant + "-" + mode + ".core.csharp.txt"), core);
            File.WriteAllText(Path.Combine(directory, "standard-blend-" + variant + "-" + mode + ".visuals.csharp.txt"), composition);
        }
        foreach (var blend in new[] { VisualBlendMode.Overlay, VisualBlendMode.SoftLight, VisualBlendMode.Hue, VisualBlendMode.Luminosity }) {
            var image = NativeMaskBlendCompositionExample.Create(variant, mode, blend);
            MaskBlendFixture.CaptureImage("standard-blend-report-" + variant + "-" + mode + "-" + blend, image.ToImage());
            Assert.Equal((variant == "compact" ? 320 : 740) + 64, image.Width);
            Assert.Equal(304, image.Height);
        }
        var source = NativeMaskExample.FilterSource("color");
        var imported = SvgRasterizer.Rasterize(source, strict: true, filters: new SvgRasterFilterOptions());
        StandardBlendBeforeFixture.Capture("standard-blend-core-example-" + variant + "-" + mode, source, imported);
        Assert.Empty(imported.Diagnostics);
        Assert.DoesNotContain("{{", core);
        Assert.DoesNotContain("{{", composition);
        Assert.DoesNotContain("ChartForgeX.Composition", core);
        Assert.DoesNotContain("ChartForgeX.Mermaid", core);
        Assert.DoesNotContain("ChartForgeX.Examples", core);
        Assert.DoesNotContain("ChartForgeX.Examples", composition);
        Assert.Contains("soft-light", core);
        Assert.Contains("luminosity", core);
        Assert.Contains("Enum.GetValues(typeof(VisualBlendMode))", composition);

    }
}
