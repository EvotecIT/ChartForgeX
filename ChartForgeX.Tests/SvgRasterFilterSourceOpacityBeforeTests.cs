using System;
using System.IO;
using System.Linq;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Only incumbent public source APIs are used; the causal rows capture before diagnostics/assertions.</summary>
public sealed class SvgRasterFilterSourceOpacityBeforeTests {
    [Theory]
    [InlineData("group")]
    [InlineData("fill-stroke")]
    [InlineData("use")]
    public void CompletedSourceOpacityEntersTheFilterOnce(string sourceKind) {
        var source = FilterSourceOpacityFixture.Source(sourceKind);
        var actual = SvgRasterizer.Rasterize(source, filters: new SvgRasterFilterOptions());
        var ordinary = SvgRasterizer.Rasterize(FilterSourceOpacityFixture.Source(sourceKind, filtered: false), strict: true);
        FilterSourceOpacityFixture.Capture("source-opacity-before-" + sourceKind, source, actual);
        FilterSourceOpacityFixture.Capture("source-opacity-before-" + sourceKind + "-ordinary", null, ordinary);
        Assert.Empty(actual.Diagnostics);
        Assert.Equal(ordinary.Image.Pixels, actual.Image.Pixels);
        Assert.Equal(new byte[] { 255, 0, 0, 128 }, FilterSourceOpacityFixture.Pixel(actual.Image, 16, 16));
        Assert.Equal(new byte[] { 255, 0, 0, 128 }, FilterSourceOpacityFixture.Pixel(actual.Image, 11, 16));
        Assert.Equal(actual.Image.Pixels, SvgRasterizer.Rasterize(source, strict: true,
            filters: new SvgRasterFilterOptions()).Image.Pixels);
    }

    [Fact]
    public void NullOptionsKeepTheSameOrdinaryPixelsAndLocatedLoss() {
        var source = FilterSourceOpacityFixture.Source("group");
        var ordinary = SvgRasterizer.Rasterize(source);
        var explicitNull = SvgRasterizer.Rasterize(source, filters: null);
        FilterSourceOpacityFixture.Capture("source-opacity-before-null", source, ordinary);
        Assert.Equal(ordinary.Image.Pixels, explicitNull.Image.Pixels);
        var warning = Assert.Single(ordinary.Diagnostics);
        Assert.Equal("SFR002", warning.Code);
        Assert.Equal("filtered", warning.ElementId);
        Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(source, strict: true, filters: null));
    }

    [Fact]
    public void UnfilteredOpacityAndExistingFlatSourceStayOnTheirOrdinaryPaths() {
        var unfiltered = FilterSourceOpacityFixture.Source("group", filtered: false);
        var plain = SvgRasterizer.Rasterize(unfiltered, strict: true);
        var optedIn = SvgRasterizer.Rasterize(unfiltered, strict: true, filters: new SvgRasterFilterOptions());
        var flat = FilterSourceOpacityFixture.Source("flat");
        var flatResult = SvgRasterizer.Rasterize(flat, strict: true, filters: new SvgRasterFilterOptions());
        FilterSourceOpacityFixture.Capture("source-opacity-before-flat", flat, flatResult);
        FilterSourceOpacityFixture.Capture("source-opacity-before-unfiltered", unfiltered, plain);
        Assert.Empty(optedIn.Diagnostics);
        Assert.Empty(flatResult.Diagnostics);
        Assert.Equal(plain.Image.Pixels, optedIn.Image.Pixels);
        Assert.Equal(SvgRasterizer.ToImage(FilterSourceOpacityFixture.Source("flat", filtered: false)).Pixels,
            flatResult.Image.Pixels);
    }
}

internal static class FilterSourceOpacityFixture {
    internal static string Source(string kind, bool filtered = true) {
        const string definitions = "<defs><filter id='blur' filterUnits='userSpaceOnUse' primitiveUnits='userSpaceOnUse' x='0' y='0' width='32' height='32' color-interpolation-filters='sRGB'>"
            + "<feGaussianBlur in='SourceGraphic' stdDeviation='0' edgeMode='none'/></filter>"
            + "<g id='payload' style='--ink:currentColor;color:red;opacity:.5'><rect x='8' y='8' width='12' height='16' fill='var(--ink)'/>"
            + "<rect x='12' y='8' width='12' height='16' fill='var(--ink)'/></g></defs>";
        var body = kind switch {
            "group" => "<g opacity='.5'><rect x='8' y='8' width='12' height='16' fill='red'/><rect x='12' y='8' width='12' height='16' fill='red'/></g>",
            "fill-stroke" => "<rect x='10' y='10' width='12' height='12' fill='red' stroke='red' stroke-width='6' opacity='.5'/>",
            "use" => "<use href='#payload'/>",
            "flat" => "<rect x='8' y='8' width='16' height='16' fill='red'/>",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        return "<svg xmlns='http://www.w3.org/2000/svg' width='32' height='32'>" + definitions
            + "<g id='filtered'" + (filtered ? " filter='url(#blur)'" : "") + ">" + body + "</g></svg>";
    }

    internal static byte[] Pixel(RgbaImage image, int x, int y) =>
        image.Pixels.Skip((y * image.Width + x) * 4).Take(4).ToArray();

    internal static void Capture(string stem, string? source, SvgRasterizationResult result) {
        var output = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output);
        if (source != null) File.WriteAllText(Path.Combine(output, stem + ".source.svg"), source);
        File.WriteAllBytes(Path.Combine(output, stem + ".native.png"), PngWriter.WriteRgba(result.Image));
        File.WriteAllText(Path.Combine(output, stem + ".diagnostics.txt"),
            string.Join("\n", result.Diagnostics.Select(item => item.Code + ":" + item.ElementId + ":" + item.Message)));
    }
}
