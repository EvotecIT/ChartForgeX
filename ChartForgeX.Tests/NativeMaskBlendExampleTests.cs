using ChartForgeX.Effects;
using ChartForgeX.SvgRaster;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeMaskBlendExampleTests {
    [Theory]
    [InlineData("compact", ThemeMode.Light)] [InlineData("compact", ThemeMode.Dark)]
    [InlineData("wide", ThemeMode.Light)] [InlineData("wide", ThemeMode.Dark)]
    public void CompletePublicProgramsShareNativeSourceAndKeepOptionalVisualsOutOfCore(string variant, ThemeMode mode) {
        var layer = NativeMaskExample.Create(variant, mode); MaskBlendFixture.Capture("native-mask-example-" + variant + "-" + mode, layer);
        var report = NativeMaskBlendCompositionExample.Create(variant, mode, VisualBlendMode.Screen);
        MaskBlendFixture.CaptureImage("native-mask-report-" + variant + "-" + mode, report.ToRgbaImage());
        var core = ExampleRecipeSource.EmitNativeMask(variant, mode); var visuals = ExampleRecipeSource.EmitNativeMaskComposition(variant, mode);
        Assert.DoesNotContain("{{", core); Assert.DoesNotContain("{{", visuals); Assert.DoesNotContain("ChartForgeX.Examples", core);
        Assert.DoesNotContain("ChartForgeX.Composition", core); Assert.Contains("public static class NativeMaskExample", core);
        Assert.Contains("ChartForgeX.Composition.ImageComposition", visuals); Assert.Contains("public static class NativeMaskExample", visuals);
        Assert.Contains("public static class NativeMaskBlendCompositionExample", visuals);
        Assert.Equal(variant == "compact" ? 320 : 740, layer.SourceBounds.Width); Assert.Contains(layer.Source.Regions, item => item.Role == "cartesian-plot");
        Assert.Empty(SvgRasterizer.Rasterize(NativeMaskExample.FilterSource(), strict: true, filters: new SvgRasterFilterOptions()).Diagnostics);
    }
}
