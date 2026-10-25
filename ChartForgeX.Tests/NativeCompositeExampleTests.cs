using System.Linq;
using ChartForgeX;
using ChartForgeX.Effects;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeCompositeExampleTests {
    [Theory]
    [InlineData("compact", VisualThemeMode.Light)] [InlineData("compact", VisualThemeMode.Dark)]
    [InlineData("wide", VisualThemeMode.Light)] [InlineData("wide", VisualThemeMode.Dark)]
    public void OrdinaryCompleteCoreAndVisualsProgramsStayWithinTheirDeclaredPackageOwners(string variant, VisualThemeMode mode) {
        var composition = NativeCompositeCompositionExample.Create(variant, mode);
        var source = NativeCompositeSourceExample.Source(variant, mode);
        var native = NativeCompositeSourceExample.Rasterize(variant, mode);
        var raw = ChartForgeX.SvgRaster.SvgRasterizer.Rasterize(source, strict: true, filters: new ChartForgeX.SvgRaster.SvgRasterFilterOptions());
        var opacitySource = NativeCompositeSourceExample.SourceWithOpacity(variant, mode);
        var opacityResult = NativeCompositeSourceExample.RasterizeWithOpacity(variant, mode);
        FilterSourceOpacityFixture.Capture("source-opacity-example-" + variant + "-" + mode, opacitySource, opacityResult);
        Assert.Empty(opacityResult.Diagnostics);
        Assert.Equal(new byte[] { 255, 0, 0, 32 }, FilterSourceOpacityFixture.Pixel(opacityResult.Image, opacityResult.Image.Width / 2, 120));
        var program = ExampleRecipeSource.EmitNativeCompositeSource(variant, mode);
        var optional = ExampleRecipeSource.EmitNativeCompositeComposition(variant, mode);
        var layer = NativeRetainedMaskExample.PrepareSource(variant, mode).CaptureLayer(new ChartForgeX.Effects.VisualLayerBlur(0),
            new ChartForgeX.Rendering.VisualRenderOptions(supersampling: 1));
        CompositeOperatorFixture.Capture("composite-example-" + variant + "-" + mode, composition, layer);
        Assert.Empty(native.Diagnostics);
        Assert.Equal(raw.Image.Pixels, native.Image.Pixels);
        Assert.Equal(variant == "compact" ? 390 : 760, native.Image.Width);
        Assert.Equal(variant == "compact" ? 390 : 1100, composition.Width);
        Assert.DoesNotContain("{{", program);
        Assert.DoesNotContain("{{", optional);
        Assert.DoesNotContain("ChartForgeX.Composition", program);
        Assert.DoesNotContain("ChartForgeX.Mermaid", program);
        Assert.DoesNotContain("ChartForgeX.Examples", program);
        Assert.DoesNotContain("ChartForgeX.Examples", optional);
        Assert.Contains("VisualCompositeOperator", optional);
        Assert.Contains("SvgRasterFilterOptions", program);
        Assert.Contains("SourceWithOpacity", program);
        var cutout = NativeCompositeCompositionExample.Create(variant, mode, VisualCompositeOperator.DestinationOut);
        Assert.False(composition.ToImage().Pixels.SequenceEqual(cutout.ToImage().Pixels));
    }
}
