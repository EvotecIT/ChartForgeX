using System.Text;
using ChartForgeX.Raster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RasterAnimationZeroDurationTests {
    [Theory]
    [InlineData(RasterAnimationFormat.Gif)]
    [InlineData(RasterAnimationFormat.Apng)]
    public void ZeroDelayInputCanBeReencodedWithoutAnInventedHold(RasterAnimationFormat format) {
        var images = new[] {
            new RgbaImage(1, 1, new byte[] { 0, 255, 0, 255 }),
            new RgbaImage(1, 1, new byte[] { 255, 0, 0, 255 }),
            new RgbaImage(1, 1, new byte[] { 0, 0, 255, 255 })
        };
        var source = RasterAnimationEncoder.Encode(images.Select(image =>
            new RasterAnimationFrame(image, TimeSpan.FromMilliseconds(120))).ToArray(), format,
            new RasterAnimationOptions { PlayCount = 3 });
        SetZeroDelay(source, format, 0);
        SetZeroDelay(source, format, 2);
        var sourceControls = ReadControls(source, format);
        Assert.Equal(new[] { 0d, 120d, 0d }, sourceControls.Delays.Select(delay => delay.Numerator * 1000d / delay.Denominator));
        Assert.Equal(images[0].Pixels, RasterImageDecoder.Decode(source).Pixels);

        var frames = images.Select((image, index) => new RasterAnimationFrame(image,
            TimeSpan.FromSeconds(sourceControls.Delays[index].Numerator / (double)sourceControls.Delays[index].Denominator))).ToArray();
        using var stream = new MemoryStream();
        RasterAnimationEncoder.WriteTo(stream, frames, format, new RasterAnimationOptions { PlayCount = 3 });
        var output = stream.ToArray();
        var outputControls = ReadControls(output, format);
        Assert.Equal(new[] { 0d, 120d, 0d }, outputControls.Delays.Select(delay => delay.Numerator * 1000d / delay.Denominator));
        Assert.Equal(sourceControls.ContainerPlayCount, outputControls.ContainerPlayCount);
        Assert.Equal(images[0].Pixels, RasterImageDecoder.Decode(output).Pixels);
        Assert.Equal(TimeSpan.Zero, frames[0].Duration);
        Assert.Equal(TimeSpan.Zero, frames[2].Duration);
    }

    [Theory]
    [InlineData(RasterAnimationFormat.Gif)]
    [InlineData(RasterAnimationFormat.Apng)]
    public void ZeroAndPositiveDurationBoundsHaveDistinctContainerRepresentations(RasterAnimationFormat format) {
        var image = new RgbaImage(1, 1, new byte[] { 255, 255, 255, 255 });
        var maximum = format == RasterAnimationFormat.Gif ? TimeSpan.FromMilliseconds(655350) : TimeSpan.FromSeconds(65535);
        var frames = new[] {
            new RasterAnimationFrame(image, TimeSpan.Zero),
            new RasterAnimationFrame(image, TimeSpan.FromTicks(1)),
            new RasterAnimationFrame(image, maximum)
        };
        var controls = ReadControls(RasterAnimationEncoder.Encode(frames, format), format);
        var expected = format == RasterAnimationFormat.Gif
            ? new[] { (0, 100), (1, 100), (65535, 100) }
            : new[] { (0, 1), (1, 65535), (65535, 1) };
        Assert.Equal(expected, controls.Delays);
        using var stream = new MemoryStream();
        Assert.Throws<ArgumentOutOfRangeException>(() => RasterAnimationEncoder.WriteTo(stream,
            new[] { new RasterAnimationFrame(image, maximum + TimeSpan.FromTicks(1)) }, format));
        Assert.Equal(0, stream.Length);
    }

    [Fact]
    public void NegativeFrameDurationIsRejected() {
        var image = new RgbaImage(1, 1, new byte[4]);
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => new RasterAnimationFrame(image, TimeSpan.FromTicks(-1)));
        Assert.Equal("duration", error.ParamName);
    }

    [Theory]
    [InlineData(RasterAnimationFormat.Gif, false)]
    [InlineData(RasterAnimationFormat.Gif, true)]
    [InlineData(RasterAnimationFormat.Apng, false)]
    [InlineData(RasterAnimationFormat.Apng, true)]
    public void ImageArrayConveniencesPreserveZeroDelayAndLoopChoice(RasterAnimationFormat format, bool loop) {
        var images = new[] { new RgbaImage(1, 1, new byte[] { 0, 255, 0, 255 }) };
        var output = EncodeImages(images, format, 0, loop);
        var controls = ReadControls(output, format);
        Assert.Equal(0, Assert.Single(controls.Delays).Numerator);
        Assert.Equal(loop ? 0 : 1, controls.ContainerPlayCount);
        Assert.Equal(images[0].Pixels, RasterImageDecoder.Decode(output).Pixels);
    }

    [Theory]
    [InlineData(RasterAnimationFormat.Gif, 65535)]
    [InlineData(RasterAnimationFormat.Apng, 6553500)]
    public void ImageArrayConveniencesUseContainerDurationBounds(RasterAnimationFormat format, int maximumCentiseconds) {
        var images = new[] { new RgbaImage(1, 1, new byte[4]) };
        var controls = ReadControls(EncodeImages(images, format, maximumCentiseconds, false), format);
        Assert.Equal((65535, format == RasterAnimationFormat.Gif ? 100 : 1), Assert.Single(controls.Delays));
        Assert.Throws<ArgumentOutOfRangeException>(() => EncodeImages(images, format, maximumCentiseconds + 1, false));
        var negative = Assert.Throws<ArgumentOutOfRangeException>(() => EncodeImages(images, format, -1, false));
        Assert.Equal("delayCentiseconds", negative.ParamName);
    }

    [Fact]
    public void ImageArrayConvenienceRejectsOversizedCollectionsBeforeProjectingEveryFrame() {
        var images = new RepeatedImages(new RgbaImage(1, 1, new byte[4]), 4_000_000);
        var error = Assert.Throws<InvalidOperationException>(() => images.ToGif(0));
        Assert.Contains("256 MiB", error.Message);
        Assert.Equal(0, images.LastRequestedIndex);
    }

    private static byte[] EncodeImages(IReadOnlyList<RgbaImage> images, RasterAnimationFormat format, int delayCentiseconds, bool loop) =>
        format == RasterAnimationFormat.Gif ? images.ToGif(delayCentiseconds, loop) : images.ToApng(delayCentiseconds, loop);

    private static Controls ReadControls(byte[] bytes, RasterAnimationFormat format) {
        var controls = new Controls();
        foreach (var offset in DelayOffsets(bytes, format)) {
            controls.Delays.Add(format == RasterAnimationFormat.Gif
                ? (ReadLittle16(bytes, offset), 100)
                : (ReadBig16(bytes, offset), ReadBig16(bytes, offset + 2)));
        }
        if (format == RasterAnimationFormat.Gif) {
            var loopOffset = FindAscii(bytes, "NETSCAPE2.0");
            controls.ContainerPlayCount = loopOffset < 0 ? 1 : ReadLittle16(bytes, loopOffset + 13);
        } else {
            for (var offset = 8; offset < bytes.Length; offset += 12 + ReadBig32(bytes, offset)) {
                if (Encoding.ASCII.GetString(bytes, offset + 4, 4) == "acTL") controls.ContainerPlayCount = ReadBig32(bytes, offset + 12);
            }
        }
        return controls;
    }

    private static void SetZeroDelay(byte[] bytes, RasterAnimationFormat format, int frameIndex) {
        var delayOffset = DelayOffsets(bytes, format).ElementAt(frameIndex);
        bytes[delayOffset] = 0;
        bytes[delayOffset + 1] = 0;
        if (format != RasterAnimationFormat.Apng) return;
        // fcTL includes its four-byte type and 26-byte payload in the PNG checksum.
        var crc = uint.MaxValue;
        for (var offset = delayOffset - 24; offset < delayOffset + 6; offset++) {
            crc ^= bytes[offset];
            for (var bit = 0; bit < 8; bit++) crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
        }
        crc ^= uint.MaxValue;
        for (var index = 0; index < 4; index++) bytes[delayOffset + 6 + index] = (byte)(crc >> (24 - index * 8));
    }

    private static IEnumerable<int> DelayOffsets(byte[] bytes, RasterAnimationFormat format) {
        if (format == RasterAnimationFormat.Apng) {
            for (var offset = 8; offset < bytes.Length; offset += 12 + ReadBig32(bytes, offset)) {
                if (Encoding.ASCII.GetString(bytes, offset + 4, 4) == "fcTL") yield return offset + 28;
            }
            yield break;
        }
        var position = 13 + ((bytes[10] & 128) != 0 ? 3 * (1 << ((bytes[10] & 7) + 1)) : 0);
        while (bytes[position] != 0x3B) {
            var marker = bytes[position++];
            if (marker == 0x21) {
                var extension = bytes[position++];
                if (extension == 0xF9) yield return position + 2;
            } else {
                Assert.Equal(0x2C, marker);
                var packed = bytes[position + 8];
                position += 9 + ((packed & 128) != 0 ? 3 * (1 << ((packed & 7) + 1)) : 0);
                position++;
            }
            while (bytes[position] != 0) position += 1 + bytes[position];
            position++;
        }
    }

    private static int FindAscii(byte[] bytes, string value) {
        var expected = Encoding.ASCII.GetBytes(value);
        for (var offset = 0; offset <= bytes.Length - expected.Length; offset++) {
            if (bytes.Skip(offset).Take(expected.Length).SequenceEqual(expected)) return offset;
        }
        return -1;
    }

    private static int ReadBig32(byte[] bytes, int offset) =>
        (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
    private static int ReadBig16(byte[] bytes, int offset) => (bytes[offset] << 8) | bytes[offset + 1];
    private static int ReadLittle16(byte[] bytes, int offset) => bytes[offset] | (bytes[offset + 1] << 8);

    private sealed class Controls {
        internal List<(int Numerator, int Denominator)> Delays { get; } = new();
        internal int ContainerPlayCount { get; set; }
    }

    private sealed class RepeatedImages : IReadOnlyList<RgbaImage> {
        private readonly RgbaImage _image;
        internal RepeatedImages(RgbaImage image, int count) { _image = image; Count = count; }
        public int Count { get; }
        internal int LastRequestedIndex { get; private set; } = -1;
        public RgbaImage this[int index] {
            get {
                LastRequestedIndex = index;
                if (index > 0) throw new InvalidOperationException("Memory validation must precede traversing every frame.");
                return _image;
            }
        }
        public IEnumerator<RgbaImage> GetEnumerator() {
            for (var index = 0; index < Count; index++) yield return this[index];
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
