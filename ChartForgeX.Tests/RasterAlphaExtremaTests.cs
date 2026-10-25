using ChartForgeX.Raster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RasterAlphaExtremaTests {
    [Theory]
    [InlineData(0, 0, true)] [InlineData(1, 1, true)] [InlineData(1, 2, false)] [InlineData(20, 20, false)]
    public void SharedByteAlphaOwnerMatchesIndependentTransparentWindowExtrema(int rx, int ry, bool maximize) {
        const int width = 4, height = 3;
        var alpha = new byte[] { 0, 17, 80, 0, 32, 255, 90, 20, 0, 110, 15, 0 };
        var rgba = alpha.SelectMany(value => new byte[] { 9, 40, 100, value }).ToArray();
        var actual = RasterAlphaExtrema.Filter(rgba, width, height, 4, 3, rx, ry, maximize);
        var expected = new byte[alpha.Length];
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) {
            var value = maximize ? (byte)0 : (byte)255;
            for (var dy = -ry; dy <= ry; dy++) for (var dx = -rx; dx <= rx; dx++) {
                var sx = x + dx; var sy = y + dy;
                var sample = sx < 0 || sy < 0 || sx >= width || sy >= height ? (byte)0 : alpha[sy * width + sx];
                value = maximize ? Math.Max(value, sample) : Math.Min(value, sample);
            }
            expected[y * width + x] = value;
        }
        Assert.Equal(expected, actual);
        Assert.Equal(actual, RasterAlphaExtrema.Filter(alpha, width, height, 1, 0, rx, ry, maximize));
        Assert.Equal(new byte[] { 0, 17, 80, 0, 32, 255, 90, 20, 0, 110, 15, 0 }, alpha);
    }
}
