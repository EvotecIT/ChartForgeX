using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Raster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RasterAnimationCompressionLevelTests {
    [Theory]
    [InlineData(0, 0x01)]
    [InlineData(1, 0x5E)]
    [InlineData(3, 0x5E)]
    [InlineData(4, 0x9C)]
    [InlineData(6, 0x9C)]
    [InlineData(9, 0x9C)]
    public void ApngUsesTheSharedPngCompressionModesAndPreservesTiming(int level, int expectedZlibFlags) {
        var image = RepeatedImage();
        var output = RasterAnimationEncoder.Encode(Frames(image), RasterAnimationFormat.Apng,
            new RasterAnimationOptions { PngCompressionLevel = level, PlayCount = 3 });
        var chunks = ReadChunks(output).ToArray();
        var compressedFrames = chunks.Where(chunk => chunk.Name == "IDAT" || chunk.Name == "fdAT").ToArray();
        Assert.Equal(2, compressedFrames.Length);
        foreach (var frame in compressedFrames) {
            var zlibOffset = frame.Name == "fdAT" ? 4 : 0;
            Assert.Equal(0x78, frame.Data[zlibOffset]);
            Assert.Equal(expectedZlibFlags, frame.Data[zlibOffset + 1]);
        }
        var pngData = Assert.Single(ReadChunks(image.ToPng(new RasterImageOptions { PngCompressionLevel = level })),
            chunk => chunk.Name == "IDAT").Data;
        Assert.Equal(pngData, compressedFrames[0].Data);
        Assert.Equal(image.Pixels, RasterImageDecoder.Decode(output).Pixels);
        var timing = chunks.Where(chunk => chunk.Name == "fcTL")
            .Select(chunk => ReadBig16(chunk.Data, 20) * 1000d / ReadBig16(chunk.Data, 22));
        Assert.Equal(new[] { 0d, 120d }, timing);
        Assert.Equal(3, ReadBig32(Assert.Single(chunks, chunk => chunk.Name == "acTL").Data, 4));
    }

    [Fact]
    public void StoredApngOutputIsLargerThanCompressedOutputForRepeatedPixels() {
        var frames = Frames(RepeatedImage());
        var stored = RasterAnimationEncoder.Encode(frames, RasterAnimationFormat.Apng,
            new RasterAnimationOptions { PngCompressionLevel = 0 });
        var fastest = RasterAnimationEncoder.Encode(frames, RasterAnimationFormat.Apng,
            new RasterAnimationOptions { PngCompressionLevel = 1 });
        var optimal = RasterAnimationEncoder.Encode(frames, RasterAnimationFormat.Apng,
            new RasterAnimationOptions { PngCompressionLevel = 6 });
        Assert.True(stored.Length > fastest.Length * 2);
        Assert.True(stored.Length > optimal.Length * 2);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10)]
    public void InvalidApngCompressionFailsBeforeOutput(int level) {
        using var stream = new MemoryStream();
        Assert.Throws<ArgumentOutOfRangeException>(() => RasterAnimationEncoder.WriteTo(stream, Frames(RepeatedImage()),
            RasterAnimationFormat.Apng, new RasterAnimationOptions { PngCompressionLevel = level }));
        Assert.Equal(0, stream.Length);
    }

    [Fact]
    public void EncodingSnapshotsApngOptionsBeforeCallingTheDestinationStream() {
        var image = RepeatedImage();
        var options = new RasterAnimationOptions { PlayCount = 3, PngCompressionLevel = 0 };
        var expected = RasterAnimationEncoder.Encode(Frames(image), RasterAnimationFormat.Apng, options);
        using var stream = new MutatingStream(() => { options.PlayCount = 1; options.PngCompressionLevel = 9; });
        RasterAnimationEncoder.WriteTo(stream, Frames(image), RasterAnimationFormat.Apng, options);
        Assert.Equal(expected, stream.ToArray());
        Assert.Equal(9, options.PngCompressionLevel);
        Assert.Equal(1, options.PlayCount);
    }

    [Fact]
    public void GifEncodingIgnoresPngCompressionSetting() {
        var frames = Frames(RepeatedImage());
        var first = RasterAnimationEncoder.Encode(frames, RasterAnimationFormat.Gif,
            new RasterAnimationOptions { PngCompressionLevel = 0 });
        var second = RasterAnimationEncoder.Encode(frames, RasterAnimationFormat.Gif,
            new RasterAnimationOptions { PngCompressionLevel = 9 });
        Assert.Equal(first, second);
    }

    private static RgbaImage RepeatedImage() {
        var pixels = new byte[64 * 64 * 4];
        for (var offset = 0; offset < pixels.Length; offset += 4) {
            pixels[offset] = 20;
            pixels[offset + 1] = 40;
            pixels[offset + 2] = 80;
            pixels[offset + 3] = 128;
        }
        return new RgbaImage(64, 64, pixels);
    }

    private static RasterAnimationFrame[] Frames(RgbaImage image) => new[] {
        new RasterAnimationFrame(image, TimeSpan.Zero),
        new RasterAnimationFrame(image, TimeSpan.FromMilliseconds(120))
    };

    private static IEnumerable<(string Name, byte[] Data)> ReadChunks(byte[] bytes) {
        for (var offset = 8; offset < bytes.Length;) {
            var length = ReadBig32(bytes, offset);
            yield return (Encoding.ASCII.GetString(bytes, offset + 4, 4), bytes.Skip(offset + 8).Take(length).ToArray());
            offset += length + 12;
        }
    }

    private static int ReadBig32(byte[] bytes, int offset) =>
        (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
    private static int ReadBig16(byte[] bytes, int offset) => (bytes[offset] << 8) | bytes[offset + 1];

    private sealed class MutatingStream : MemoryStream {
        private readonly Action _mutate;
        internal MutatingStream(Action mutate) => _mutate = mutate;
        public override void Write(byte[] buffer, int offset, int count) {
            _mutate();
            base.Write(buffer, offset, count);
        }
    }
}
