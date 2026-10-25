using ChartForgeX.Raster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class RasterAnimationEncoderTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ColourRichFrameMatchesBruteForceBeyondTheExactCache(bool transparent) {
        const int width = 513, height = 257;
        var pixels = new byte[width * height * 4];
        var unique = new HashSet<int>();
        for (var i = 0; i < width * height; i++) {
            var color = (i * 7919) & 0xffffff;
            unique.Add(color);
            pixels[i * 4] = (byte)(color >> 16);
            pixels[i * 4 + 1] = (byte)(color >> 8);
            pixels[i * 4 + 2] = (byte)color;
            pixels[i * 4 + 3] = transparent && i % 101 == 0 ? (byte)0 : (byte)255;
        }
        Assert.True(unique.Count > 65536);
        var image = new RgbaImage(width, height, pixels);
        var palette = GifPaletteQuantizer.BuildPalette(new[] { image });
        var expected = new byte[width * height];
        for (var i = 0; i < expected.Length; i++) {
            if (transparent && pixels[i * 4 + 3] == 0) { expected[i] = (byte)palette.TransparentIndex; continue; }
            var bestDistance = int.MaxValue;
            for (var slot = 0; slot < (transparent ? 255 : 256); slot++) {
                var dr = pixels[i * 4] - palette.Colors[slot * 3];
                var dg = pixels[i * 4 + 1] - palette.Colors[slot * 3 + 1];
                var db = pixels[i * 4 + 2] - palette.Colors[slot * 3 + 2];
                var distance = dr * dr + dg * dg + db * db;
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                expected[i] = (byte)slot;
            }
        }
        Assert.Equal(expected, GifPaletteQuantizer.Quantize(image, palette));
    }

    [Fact]
    public void NearestColourTiesKeepTheFirstPaletteEntry() {
        var colors = new byte[768];
        Array.Fill(colors, (byte)255);
        colors[0] = colors[1] = colors[2] = 0;
        colors[3] = 2; colors[4] = colors[5] = 0;
        // A duplicate palette colour must not displace the earlier entry either.
        colors[6] = colors[7] = colors[8] = 0;
        var indexed = GifPaletteQuantizer.Quantize(new RgbaImage(2, 1,
            new byte[] { 1, 0, 0, 255, 0, 0, 0, 255 }), new GifPalette(colors));
        Assert.Equal(new byte[] { 0, 0 }, indexed);
    }
}
