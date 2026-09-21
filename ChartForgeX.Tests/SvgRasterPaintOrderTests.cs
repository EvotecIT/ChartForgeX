using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class SvgRasterPaintOrderTests {
    [Theory]
    [InlineData("<text x='15' y='45' font-size='30'>Readable</text>")]
    [InlineData("<rect x='20' y='20' width='60' height='25'/>")]
    [InlineData("<path d='M20 20 H80 V45 H20 Z'/>")]
    public void StrokeFirstPreservesFillUnderWideHalo(string shape) {
        var normal = Render(shape, "normal");
        var strokeFirst = Render(shape, "stroke");
        var explicitOrder = Render(shape, "stroke fill");
        Assert.True(RedPixels(strokeFirst) > RedPixels(normal));
        Assert.Equal(strokeFirst, explicitOrder);
    }

    [Theory]
    [InlineData("<path d='M20 20 H80 V45 H20 Z'/>")]
    [InlineData("<polygon points='20,20 80,20 80,45 20,45'/>")]
    [InlineData("<polyline points='20,20 80,20 80,45 20,45 20,20'/>")]
    public void MarkersRetainTheirPositionRelativeToFillAndStroke(string shape) {
        var first = RenderMarker(shape, "markers fill stroke");
        var middle = RenderMarker(shape, "fill markers stroke");
        var last = RenderMarker(shape, "fill stroke markers");
        Assert.True(BluePixels(first) < BluePixels(middle));
        Assert.True(BluePixels(middle) < BluePixels(last));
    }

    [Fact]
    public void LineCanPaintMarkersBeforeItsStroke() {
        const string line = "<line x1='20' y1='20' x2='80' y2='20'/>";
        Assert.True(BluePixels(RenderMarker(line, "markers")) < BluePixels(RenderMarker(line, "normal")));
    }

    private static byte[] RenderMarker(string shape, string order) {
        const string marker = "<defs><marker id='dot' markerUnits='userSpaceOnUse' markerWidth='20' markerHeight='20' viewBox='-10 -10 20 20' refX='0' refY='0'><rect x='-10' y='-10' width='20' height='20' fill='#0000ff' stroke='none'/></marker></defs>";
        return Render(marker + shape.Replace("/>", " marker-start='url(#dot)'/>", StringComparison.Ordinal), order);
    }

    private static int BluePixels(byte[] pixels) => Enumerable.Range(0, pixels.Length / 4)
        .Count(index => pixels[index * 4] < 80 && pixels[index * 4 + 1] < 80 && pixels[index * 4 + 2] > 200 && pixels[index * 4 + 3] > 200);

    [Fact]
    public void DescendantCanResetInheritedPaintOrder() {
        var normal = Render("<text style='paint-order:normal' x='15' y='45' font-size='30'>Readable</text>", "stroke");
        var reference = Render("<text x='15' y='45' font-size='30'>Readable</text>", "normal");
        Assert.Equal(reference, normal);
    }

    private static byte[] Render(string shape, string order) {
        Assert.True(SvgRasterRenderer.TryRenderFragment("<g fill='#ff0000' stroke='#ffffff' stroke-width='8' paint-order='" + order + "'>" + shape + "</g>",
            "0 0 200 70", "none", 200, 70, out var pixels));
        return pixels;
    }

    private static int RedPixels(byte[] pixels) => Enumerable.Range(0, pixels.Length / 4)
        .Count(index => pixels[index * 4] > 200 && pixels[index * 4 + 1] < 80 && pixels[index * 4 + 2] < 80 && pixels[index * 4 + 3] > 200);
}
