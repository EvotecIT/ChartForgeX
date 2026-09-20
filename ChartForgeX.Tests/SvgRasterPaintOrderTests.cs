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
