using System;
using System.Threading;
using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Admission reuses the ordinary compositor and its existing cumulative allocation/instance owner.</summary>
public sealed class SvgRasterFilterSourceOpacityPostTests {
    [Fact]
    public void NestedGroupArraysRemainChargedAtTheConcretePublicWorkflowLimit() {
        var source = FilterSourceOpacityFixture.Source("group");
        var generous = SvgRasterizer.Rasterize(source, strict: true, filters: new SvgRasterFilterOptions());
        // 32x32 RGBA: root + capture + output; zero-sigma plan conservatively reserves its
        // existing two16-bit RGBA buffers + result. The inner completed group adds one
        // real RGBA canvas and one opacity copy: 4+4+4+20+4+4 =40 bytes per pixel.
        const long exactBytes = 32 * 32 * 40;
        var exact = SvgRasterizer.Rasterize(source, strict: true,
            filters: new SvgRasterFilterOptions(maximumLiveBytes: exactBytes));
        FilterSourceOpacityFixture.Capture("source-opacity-post-exact-budget", source, exact);
        Assert.Empty(exact.Diagnostics);
        Assert.Equal(generous.Image.Pixels, exact.Image.Pixels);
        Assert.Equal(new byte[] { 255, 0, 0, 128 }, FilterSourceOpacityFixture.Pixel(exact.Image, 16, 16));
        Assert.Contains("safety limit", Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(source,
            strict: true, filters: new SvgRasterFilterOptions(maximumLiveBytes: exactBytes - 1))).Message);
        Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(source, strict: true,
            filters: new SvgRasterFilterOptions(maximumWorkSamples: 1)));
        Assert.Throws<OperationCanceledException>(() => SvgRasterizer.Rasterize(source, strict: true,
            filters: new SvgRasterFilterOptions(cancellationToken: new CancellationToken(true))));
    }

    [Fact]
    public void SameSourceOpacityContractUsesTheActualSelectedCompositeAndOneSharedInstanceCounter() {
        var source = FilterSourceOpacityFixture.Source("group")
            .Replace("<feGaussianBlur in='SourceGraphic' stdDeviation='0' edgeMode='none'/>",
                "<feFlood flood-color='blue' flood-opacity='.5' result='backdrop'/><feComposite in='SourceGraphic' in2='backdrop' operator='in'/>");
        var result = SvgRasterizer.Rasterize(source, strict: true, filters: new SvgRasterFilterOptions());
        FilterSourceOpacityFixture.Capture("source-opacity-post-composite", source, result);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(new byte[] { 255, 0, 0, 64 }, FilterSourceOpacityFixture.Pixel(result.Image, 16, 16));
        Assert.Equal(new byte[4], FilterSourceOpacityFixture.Pixel(result.Image, 2, 2));
        var repeated = source.Replace("</svg>", "<use href='#filtered'/></svg>");
        var twice = SvgRasterizer.Rasterize(repeated, strict: true, filters: new SvgRasterFilterOptions(maximumApplications: 2));
        FilterSourceOpacityFixture.Capture("source-opacity-post-two-instances", repeated, twice);
        Assert.Empty(twice.Diagnostics);
        Assert.InRange(FilterSourceOpacityFixture.Pixel(twice.Image, 16, 16)[3], (byte)65, byte.MaxValue);
        Assert.Contains("instance count", Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(repeated,
            strict: true, filters: new SvgRasterFilterOptions(maximumApplications: 1))).Message);
    }
}
