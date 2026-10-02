using ChartForgeX.Raster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RasterImageMinificationTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReducingAnImageAveragesTheSourceFootprint(bool affine) {
        const int sourceSize = 300, targetSize = 100;
        var source = new byte[sourceSize * sourceSize * 4];
        for (int y = 0; y < sourceSize; y++) for (int x = 0; x < sourceSize; x++) {
            int index = (y * sourceSize + x) * 4;
            byte value = (byte)(((x + y) & 1) == 0 ? 0 : 255);
            source[index] = source[index + 1] = source[index + 2] = value;
            source[index + 3] = 255;
        }
        var canvas = new RgbaCanvas(targetSize, targetSize, 1);
        if (affine) canvas.DrawImageTransformed(sourceSize, sourceSize, source, 1D / 3, 0, 0, 1D / 3, 0, 0);
        else canvas.DrawImageScaled(0, 0, targetSize, targetSize, sourceSize, sourceSize, source);
        for (int y = 0; y < targetSize; y++) for (int x = 0; x < targetSize; x++) {
            int index = (y * targetSize + x) * 4;
            Assert.Equal((byte)(((x + y) & 1) == 0 ? 113 : 142), canvas.Pixels[index]);
            Assert.Equal((byte)255, canvas.Pixels[index + 3]);
        }
    }

    [Fact]
    public void ImageFootprintFilteringUsesPremultipliedAlpha() {
        var canvas = new RgbaCanvas(1, 1, 1);
        canvas.DrawImageScaled(0, 0, 1, 1, 3, 1, new byte[] { 255, 0, 0, 255, 0, 0, 255, 0, 0, 0, 255, 0 });
        Assert.Equal(new byte[] { 255, 0, 0, 85 }, canvas.Pixels);
    }
}
