using ChartForgeX.SvgRaster;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeDropShadowExampleTests {
    [Theory]
    [InlineData("compact", VisualThemeMode.Light)] [InlineData("compact", VisualThemeMode.Dark)]
    [InlineData("wide", VisualThemeMode.Light)] [InlineData("wide", VisualThemeMode.Dark)]
    public void CompleteSharedProgramUsesTheSameNativeSourceAndExplicitFiniteImport(string variant, VisualThemeMode mode) {
        var layer = NativeDropShadowExample.Create(variant, mode);
        NativeEffectBeforeCapture.Write("native-shadow-example-" + variant + "-" + mode, layer.Visual.ToSvg(), layer.Visual.ToPng(), layer.ToImage());
        var program = ExampleRecipeSource.EmitNativeDropShadow(variant, mode);
        Assert.DoesNotContain("{{", program); Assert.DoesNotContain("ChartForgeX.Examples", program); Assert.DoesNotContain("ChartForgeX.Composition", program);
        Assert.Contains("public static class NativeDropShadowExample", program); Assert.Contains("new ChartForgeX.Effects.VisualDropShadow", program);
        Assert.Equal(variant == "compact" ? 320 : 740, layer.SourceBounds.Width);
        Assert.Contains(layer.Source.Regions, region => region.Role == "cartesian-plot");
        Assert.Empty(SvgRasterizer.Rasterize(NativeDropShadowExample.FilterSource(), strict: true, filters: new SvgRasterFilterOptions()).Diagnostics);
    }
}
