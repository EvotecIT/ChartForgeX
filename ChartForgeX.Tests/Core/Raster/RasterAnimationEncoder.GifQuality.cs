using ChartForgeX.Raster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class RasterAnimationEncoderTests {
    [Fact]
    public void CloseSourceColoursSurviveExactlyAndLzwCrossesEveryCodeWidth() {
        var random = new Random(4281);
        var pixels = new byte[256 * 256 * 4];
        for (var i = 0; i < pixels.Length; i += 4) {
            var color = random.Next(256);
            pixels[i] = (byte)(16 + color % 8);
            pixels[i + 1] = (byte)(16 + color / 8 % 8);
            pixels[i + 2] = (byte)(16 + color / 64);
            pixels[i + 3] = 255;
        }
        var frame = new RasterAnimationFrame(new RgbaImage(256, 256, pixels), TimeSpan.FromSeconds(1));
        var bytes = RasterAnimationEncoder.Encode(new[] { frame }, RasterAnimationFormat.Gif);
        Assert.Equal(pixels, RasterImageDecoder.Decode(bytes).Pixels);
        Assert.Equal(bytes, RasterAnimationEncoder.Encode(new RasterAnimationSource(256, 256, 1, (_, _) => frame), RasterAnimationFormat.Gif));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4)]
    public void LocalPaletteKeepsLowColourFramesExactWhenAnimationExceeds256Colours(int unusedBytes) {
        var original = ColourRow(0);
        var padded = original.Image.Pixels.Concat(new byte[unusedBytes]).ToArray();
        var frames = new[] { new RasterAnimationFrame(new RgbaImage(256, 1, padded), original.Duration), ColourRow(1) };
        var bytes = RasterAnimationEncoder.Encode(frames, RasterAnimationFormat.Gif);
        // Each row has 256 colours; their union has 512. The decoder must use the first local table.
        Assert.Equal(original.Image.Pixels, RasterImageDecoder.Decode(bytes).Pixels);
        Assert.Equal(2, ReadControls(bytes, RasterAnimationFormat.Gif).Durations.Count);
        Assert.Equal(bytes, RasterAnimationEncoder.Encode(new RasterAnimationSource(256, 1, 2, (index, _) => frames[index]), RasterAnimationFormat.Gif));
    }

    [Fact]
    public void GradientDoesNotDitherRepeatedGlyphInk() {
        var pixels = new byte[512 * 8 * 4];
        for (var y = 0; y < 8; y++) {
            for (var x = 0; x < 512; x++) {
                var at = (y * 512 + x) * 4;
                pixels[at] = x < 400 ? (byte)(x / 2) : (byte)37;
                pixels[at + 1] = x < 400 ? (byte)(x % 2) : (byte)99;
                pixels[at + 2] = x < 400 ? (byte)100 : (byte)235;
                pixels[at + 3] = 255;
            }
        }
        var bytes = RasterAnimationEncoder.Encode(new[] { new RasterAnimationFrame(new RgbaImage(512, 8, pixels), TimeSpan.FromSeconds(1)) }, RasterAnimationFormat.Gif);
        var decoded = RasterImageDecoder.Decode(bytes);
        for (var y = 0; y < 8; y++) for (var x = 400; x < 512; x++) {
            var at = (y * 512 + x) * 4;
            Assert.Equal(pixels.AsSpan(at, 4).ToArray(), decoded.Pixels.AsSpan(at, 4).ToArray());
        }
    }

    [Fact]
    public void DuplicateScreensMergeWithoutLosingDelayOrOverflowingGifClock() {
        var first = ColourRow(0);
        var frames = new[] {
            new RasterAnimationFrame(first.Image, TimeSpan.FromSeconds(400)),
            new RasterAnimationFrame(first.Image, TimeSpan.FromSeconds(200)),
            new RasterAnimationFrame(first.Image, TimeSpan.FromSeconds(100)),
            ColourRow(1)
        };
        var bytes = RasterAnimationEncoder.Encode(frames, RasterAnimationFormat.Gif, new RasterAnimationOptions { PlayCount = 1 });
        Assert.Equal(new[] { 600_000d, 100_000d, 1000d }, ReadControls(bytes, RasterAnimationFormat.Gif).Durations);
        Assert.DoesNotContain("NETSCAPE2.0", System.Text.Encoding.ASCII.GetString(bytes));
        Assert.Equal(bytes, RasterAnimationEncoder.Encode(new RasterAnimationSource(256, 1, frames.Length, (index, _) => frames[index]), RasterAnimationFormat.Gif, new RasterAnimationOptions { PlayCount = 1 }));
    }

    [Fact]
    public void OpaqueLocalPaletteClearsBeforeATransparentCanvas() {
        var first = ColourRow(0);
        var second = ColourRow(1);
        second.Image.Pixels[3] = 0;
        var bytes = RasterAnimationEncoder.Encode(new[] { first, second }, RasterAnimationFormat.Gif);
        var controls = ReadControls(bytes, RasterAnimationFormat.Gif);
        Assert.Equal(new[] { 9, 9 }, controls.PackedControls);
        Assert.Equal(255, bytes[11]);
        Assert.Equal(bytes, RasterAnimationEncoder.Encode(new RasterAnimationSource(256, 1, 2,
            (index, _) => index == 0 ? first : second), RasterAnimationFormat.Gif));
    }

    private static RasterAnimationFrame ColourRow(byte blue) {
        var pixels = new byte[256 * 4];
        for (var i = 0; i < 256; i++) { pixels[i * 4] = (byte)i; pixels[i * 4 + 2] = blue; pixels[i * 4 + 3] = 255; }
        return new RasterAnimationFrame(new RgbaImage(256, 1, pixels), TimeSpan.FromSeconds(1));
    }
}
