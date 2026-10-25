using System;
using System.IO;
using System.Linq;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Current public SVG import is the causal boundary; no new enum member or future fixture is required.</summary>
public sealed class SvgRasterStandardBlendBeforeTests {
    [Theory]
    [InlineData("overlay", 85, 0, 170, 192)]
    [InlineData("darken", 85, 0, 85, 192)]
    [InlineData("lighten", 170, 0, 170, 192)]
    [InlineData("color-dodge", 85, 0, 170, 192)]
    [InlineData("color-burn", 85, 0, 170, 192)]
    [InlineData("hard-light", 170, 0, 85, 192)]
    [InlineData("soft-light", 85, 0, 170, 192)]
    [InlineData("difference", 170, 0, 170, 192)]
    [InlineData("exclusion", 170, 0, 170, 192)]
    [InlineData("hue", 116, 0, 85, 192)]
    [InlineData("saturation", 85, 0, 170, 192)]
    [InlineData("color", 116, 0, 85, 192)]
    [InlineData("luminosity", 103, 18, 170, 192)]
    public void StandardSourceModesUseTheKnownFloodBackdropAndSourceOverAlpha(string mode, int red, int green, int blue, int alpha) {
        var source = StandardBlendBeforeFixture.Source(mode);
        var actual = SvgRasterizer.Rasterize(source, filters: new SvgRasterFilterOptions());
        StandardBlendBeforeFixture.Capture("standard-blend-source-before-" + mode, source, actual);
        Assert.Empty(actual.Diagnostics);
        Assert.Equal(new byte[] { (byte)red, (byte)green, (byte)blue, (byte)alpha },
            StandardBlendBeforeFixture.Pixel(actual.Image, 16, 16));
        Assert.Equal(new byte[] { 0, 0, 255, 128 }, StandardBlendBeforeFixture.Pixel(actual.Image, 2, 2));
        Assert.Equal(actual.Image.Pixels, SvgRasterizer.Rasterize(source, strict: true,
            filters: new SvgRasterFilterOptions()).Image.Pixels);
    }

    [Fact]
    public void NullOptionsKeepOrdinaryPixelsAndStrictLossWhileCurrentStandardModesRemainOptedIn() {
        var source = StandardBlendBeforeFixture.Source("overlay");
        var ordinary = SvgRasterizer.Rasterize(source);
        var explicitNull = SvgRasterizer.Rasterize(source, filters: null);
        StandardBlendBeforeFixture.Capture("standard-blend-source-null-control", source, ordinary);
        Assert.Equal(ordinary.Image.Pixels, explicitNull.Image.Pixels);
        Assert.Contains(ordinary.Diagnostics, item => item.Code == "SFR002");
        Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(source, strict: true));
        foreach (var current in new[] { "normal", "multiply", "screen" }) {
            var present = StandardBlendBeforeFixture.Source(current);
            var result = SvgRasterizer.Rasterize(present, strict: true, filters: new SvgRasterFilterOptions());
            StandardBlendBeforeFixture.Capture("standard-blend-source-existing-" + current, present, result);
            Assert.Empty(result.Diagnostics);
        }
    }
}

internal static class StandardBlendBeforeFixture {
    internal static string Source(string mode) =>
        "<svg xmlns='http://www.w3.org/2000/svg' width='32' height='32'><defs>"
        + "<filter id='blend' filterUnits='userSpaceOnUse' primitiveUnits='userSpaceOnUse' x='0' y='0' width='32' height='32' color-interpolation-filters='sRGB'>"
        + "<feFlood flood-color='#0000ff' flood-opacity='0.5019607843137255' result='backdrop'/>"
        + "<feBlend in='SourceGraphic' in2='backdrop' mode='" + mode + "'/></filter></defs>"
        + "<rect id='source' x='8' y='8' width='16' height='16' fill='red' fill-opacity='0.5019607843137255' filter='url(#blend)'/></svg>";

    internal static byte[] Pixel(RgbaImage image, int x, int y) =>
        image.Pixels.Skip((y * image.Width + x) * 4).Take(4).ToArray();

    internal static void Capture(string stem, string source, SvgRasterizationResult result) {
        var directory = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, stem + ".source.svg"), source);
        File.WriteAllBytes(Path.Combine(directory, stem + ".native.png"), PngWriter.WriteRgba(result.Image));
        File.WriteAllBytes(Path.Combine(directory, stem + ".rgba"), result.Image.Pixels);
        File.WriteAllText(Path.Combine(directory, stem + ".diagnostics.txt"),
            string.Join("\n", result.Diagnostics.Select(item => item.Code + ":" + item.ElementId + ":" + item.Message)));
    }
}
