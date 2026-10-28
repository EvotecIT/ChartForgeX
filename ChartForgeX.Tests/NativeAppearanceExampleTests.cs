using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeAppearanceExampleTests {
    [Theory]
    [InlineData("compact", VisualThemeMode.Light)] [InlineData("compact", VisualThemeMode.Dark)]
    [InlineData("wide", VisualThemeMode.Light)] [InlineData("wide", VisualThemeMode.Dark)]
    public void CompleteCoreAndOptionalVisualsProgramsShareTheDisplayedSourcesAndDetachedNativePixels(string variant, VisualThemeMode mode) {
        foreach (var effect in new[] { "outline", "bloom", "glow", "inner" }) {
            var layer = NativeAppearanceExample.Create(effect, variant, mode);
            AppearanceFixture.Capture("appearance-example-" + effect + "-" + variant + "-" + mode, layer);
            var document = XDocument.Parse(layer.Visual.ToSvg());
            var image = Assert.Single(document.Descendants(), node => node.Name.LocalName == "image");
            var uri = image.Attribute("href")!.Value;
            var decoded = RasterImageDecoder.Decode(Convert.FromBase64String(uri[(uri.IndexOf(',') + 1)..]));
            Assert.Equal(layer.ToImage().Pixels, decoded.Pixels);
            var program = ExampleRecipeSource.EmitNativeAppearance(effect, variant, mode);
            Assert.DoesNotContain("{{", program);
            Assert.DoesNotContain("ChartForgeX.Composition", program);
            Assert.DoesNotContain("ChartForgeX.Examples", program);
            Assert.DoesNotContain("ChartForgeX.Mermaid", program);
            Assert.Contains("Capacity signals", program);
            Assert.True(layer.Source.Regions.Count > 0);
        }
        var composition = NativeGlassCompositionExample.Create(variant, mode);
        AppearanceFixture.Capture("appearance-example-glass-" + variant + "-" + mode, composition);
        Assert.Equal(variant == "compact" ? 390 : 1100, composition.ToImage().Width);
        Assert.Equal(400, composition.ToImage().Height);
        var optional = ExampleRecipeSource.EmitNativeGlassComposition(variant, mode);
        Assert.Contains("BlurRegion", optional);
        Assert.DoesNotContain("{{", optional);
        Assert.DoesNotContain("ChartForgeX.Examples", optional);
        Assert.DoesNotContain("\\n\\n", optional);
    }
}
