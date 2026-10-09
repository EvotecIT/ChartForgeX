using System.Text;
using ChartForgeX.Raster;
using ChartForgeX.Stories;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RasterAnimationEncoderTests {
    [Fact]
    public void GifRationalCadenceKeepsOneClockForRetainedAndStreamedFramesIncludingZeroDelays() {
        var image = new RgbaImage(1, 1, new byte[] { 255, 0, 0, 255 });
        var frames = new List<RasterAnimationFrame>();
        for (var i = 0; i < 60; i++) {
            if (i == 20) frames.Add(new RasterAnimationFrame(image, TimeSpan.Zero));
            var ticks = (i + 1L) * TimeSpan.TicksPerSecond / 6 - i * (long)TimeSpan.TicksPerSecond / 6;
            frames.Add(new RasterAnimationFrame(image, TimeSpan.FromTicks(ticks)));
        }
        frames.Add(new RasterAnimationFrame(image, TimeSpan.Zero));
        var bytes = RasterAnimationEncoder.Encode(frames, RasterAnimationFormat.Gif);
        var controls = ReadControls(bytes, RasterAnimationFormat.Gif);
        Assert.Equal(10_000, controls.Durations.Sum());
        Assert.Equal(0, controls.Durations[20]);
        Assert.Equal(0, controls.Durations[^1]);
        var source = new RasterAnimationSource(1, 1, frames.Count, (index, _) => frames[index]);
        Assert.Equal(bytes, RasterAnimationEncoder.Encode(source, RasterAnimationFormat.Gif));
    }

    [Fact]
    public void PreparedGifClockMatchesRawAndContainerSpecificFrameEncoderRoutes() {
        var story = VisualStory.Create("One GIF clock").WithSize(480, 320);
        story.Scene("result", "Result", 9).Panel("result", new VisualStoryTextSurface("Ready"));
        story.Outcome("ready", "Ready", "result");
        var prepared = story.Prepare(new VisualStoryPlaybackOptions(TimeSpan.FromSeconds(1), TimeSpan.Zero, 1));
        var sampling = new VisualStoryFrameOptions(6);
        var playback = new RasterAnimationOptions { PlayCount = 1 };
        var expected = prepared.ToGif(sampling);
        Assert.Equal(10_000, ReadControls(expected, RasterAnimationFormat.Gif).Durations.Sum());
        Assert.Equal(expected, RasterAnimationEncoder.Encode(prepared.FrameSource(sampling), RasterAnimationFormat.Gif, playback));
        Assert.Equal(expected, RasterAnimationEncoder.Encode(prepared.Frames(sampling).ToArray(), RasterAnimationFormat.Gif, playback));
        Assert.Equal(expected, RasterAnimationEncoder.Encode(prepared.FrameSource(RasterAnimationFormat.Gif, sampling), RasterAnimationFormat.Gif, playback));
        Assert.Equal(expected, RasterAnimationEncoder.Encode(prepared.Frames(RasterAnimationFormat.Gif, sampling).ToArray(), RasterAnimationFormat.Gif, playback));
    }

    [Theory]
    [InlineData(RasterAnimationFormat.Gif)]
    [InlineData(RasterAnimationFormat.Apng)]
    public void FramesRetainIndividualDurationsAndFinitePlayback(RasterAnimationFormat format) {
        var frames = new[] {
            Frame(2, 2, 135), Frame(2, 2, 250), Frame(2, 2, 470)
        };
        var bytes = RasterAnimationEncoder.Encode(frames, format, new RasterAnimationOptions { PlayCount = 3 });
        var controls = ReadControls(bytes, format);
        Assert.Equal(3, controls.Durations.Count);
        Assert.Equal(format == RasterAnimationFormat.Gif ? 140 : 135, controls.Durations[0], 4);
        Assert.Equal(250, controls.Durations[1], 4);
        Assert.Equal(470, controls.Durations[2], 4);
        Assert.Equal(format == RasterAnimationFormat.Gif ? 2 : 3, controls.ContainerPlayCount);
    }

    [Theory]
    [InlineData(RasterAnimationFormat.Gif, 0)]
    [InlineData(RasterAnimationFormat.Gif, 1)]
    [InlineData(RasterAnimationFormat.Apng, 0)]
    [InlineData(RasterAnimationFormat.Apng, 1)]
    public void PlaybackDistinguishesInfiniteAndSinglePlay(RasterAnimationFormat format, int playCount) {
        var bytes = RasterAnimationEncoder.Encode(new[] { Frame(1, 1, 100) }, format, new RasterAnimationOptions { PlayCount = playCount });
        var controls = ReadControls(bytes, format);
        Assert.Equal(format == RasterAnimationFormat.Gif && playCount == 1 ? null : playCount, controls.ContainerPlayCount);
    }

    [Theory]
    [InlineData(RasterAnimationFormat.Gif)]
    [InlineData(RasterAnimationFormat.Apng)]
    public void StreamOutputMatchesArrayOutputAndLeavesTheStreamOpen(RasterAnimationFormat format) {
        var frames = new[] { Frame(2, 2, 125), Frame(2, 2, 300) };
        using var stream = new MemoryStream();
        RasterAnimationEncoder.WriteTo(stream, frames, format);
        Assert.Equal(RasterAnimationEncoder.Encode(frames, format), stream.ToArray());
        Assert.True(stream.CanWrite);
    }

    [Theory]
    [InlineData(RasterAnimationFormat.Gif)]
    [InlineData(RasterAnimationFormat.Apng)]
    public void InvalidFramesFailBeforeWritingOutput(RasterAnimationFormat format) {
        using var stream = new MemoryStream();
        Assert.Throws<ArgumentException>(() => RasterAnimationEncoder.WriteTo(stream, new[] { Frame(2, 2, 100), Frame(1, 2, 100) }, format));
        Assert.Throws<ArgumentException>(() => RasterAnimationEncoder.WriteTo(stream, new[] { default(RasterAnimationFrame) }, format));
        Assert.Throws<ArgumentOutOfRangeException>(() => RasterAnimationEncoder.WriteTo(stream, new[] { Frame(2, 2, 100) }, (RasterAnimationFormat)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => RasterAnimationEncoder.WriteTo(stream, new[] { Frame(2, 2, 100) }, format, new RasterAnimationOptions { PlayCount = -1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => RasterAnimationEncoder.WriteTo(stream,
            new[] { new RasterAnimationFrame(new RgbaImage(1, 1, new byte[4]), TimeSpan.FromSeconds(65536)) }, format));
        Assert.Equal(0, stream.Length);
    }

    [Theory]
    [InlineData(RasterAnimationFormat.Gif)]
    [InlineData(RasterAnimationFormat.Apng)]
    public void CancellationIsObservedDuringEncoding(RasterAnimationFormat format) {
        using var cancellation = new CancellationTokenSource();
        using var stream = new CancellingStream(cancellation);
        Assert.Throws<OperationCanceledException>(() => RasterAnimationEncoder.WriteTo(stream,
            new[] { Frame(2, 2, 100) }, format, cancellationToken: cancellation.Token));
        Assert.True(stream.Length > 0);
        Assert.True(stream.CanWrite);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TinyGifFramesRespectRetainedCollectionOverheadBeforeAllocationOrWriting(bool writeToStream) {
        using var cancellation = new CancellationTokenSource();
        var frames = new RepeatedFrames(Frame(1, 1, 100), 4_000_000, cancellation);
        using var stream = new MemoryStream();
        var error = Assert.Throws<InvalidOperationException>(() => {
            if (writeToStream) RasterAnimationEncoder.WriteTo(stream, frames, RasterAnimationFormat.Gif, cancellationToken: cancellation.Token);
            else RasterAnimationEncoder.Encode(frames, RasterAnimationFormat.Gif, cancellationToken: cancellation.Token);
        });
        Assert.Contains("256 MiB", error.Message);
        Assert.Equal(0, frames.LastRequestedIndex);
        Assert.Equal(0, stream.Length);
        Assert.False(cancellation.IsCancellationRequested);
    }

    [Fact]
    public void GifRepresentableLimitsAndApngShortTimingAreExplicit() {
        var image = new RgbaImage(1, 1, new byte[4]);
        var gif = RasterAnimationEncoder.Encode(new[] { new RasterAnimationFrame(image, TimeSpan.FromMilliseconds(655350)) },
            RasterAnimationFormat.Gif, new RasterAnimationOptions { PlayCount = 65536 });
        var controls = ReadControls(gif, RasterAnimationFormat.Gif);
        Assert.Equal(655350, controls.Durations[0]);
        Assert.Equal(65535, controls.ContainerPlayCount);
        Assert.Throws<ArgumentOutOfRangeException>(() => RasterAnimationEncoder.Encode(new[] { Frame(1, 1, 100) },
            RasterAnimationFormat.Gif, new RasterAnimationOptions { PlayCount = 65537 }));
        Assert.Throws<ArgumentException>(() => RasterAnimationEncoder.Encode(new[] {
            new RasterAnimationFrame(new RgbaImage(65536, 1, new byte[65536 * 4]), TimeSpan.FromMilliseconds(100))
        }, RasterAnimationFormat.Gif));
        var apng = RasterAnimationEncoder.Encode(new[] { new RasterAnimationFrame(image, TimeSpan.FromMilliseconds(1)) }, RasterAnimationFormat.Apng);
        Assert.Equal(1, ReadControls(apng, RasterAnimationFormat.Apng).Durations[0], 6);
    }

    private static RasterAnimationFrame Frame(int width, int height, int durationMilliseconds) =>
        new(new RgbaImage(width, height, Enumerable.Repeat((byte)255, width * height * 4).ToArray()), TimeSpan.FromMilliseconds(durationMilliseconds));

    private static Controls ReadControls(byte[] bytes, RasterAnimationFormat format) {
        var result = new Controls();
        if (format == RasterAnimationFormat.Apng) {
            for (var offset = 8; offset < bytes.Length;) {
                var length = ReadBig32(bytes, offset);
                var name = Encoding.ASCII.GetString(bytes, offset + 4, 4);
                if (name == "acTL") result.ContainerPlayCount = ReadBig32(bytes, offset + 12);
                if (name == "fcTL") result.Durations.Add(ReadBig16(bytes, offset + 28) * 1000d / ReadBig16(bytes, offset + 30));
                offset += 12 + length;
            }
            return result;
        }
        var position = 13 + ((bytes[10] & 128) != 0 ? 3 * (1 << ((bytes[10] & 7) + 1)) : 0);
        while (bytes[position] != 0x3B) {
            var marker = bytes[position++];
            if (marker == 0x21) {
                var extension = bytes[position++];
                if (extension == 0xF9) result.Durations.Add(ReadLittle16(bytes, position + 2) * 10d);
                if (extension == 0xFF && Encoding.ASCII.GetString(bytes, position + 1, bytes[position]) == "NETSCAPE2.0") {
                    result.ContainerPlayCount = ReadLittle16(bytes, position + bytes[position] + 3);
                }
            } else {
                Assert.Equal(0x2C, marker);
                var packed = bytes[position + 8];
                position += 9 + ((packed & 128) != 0 ? 3 * (1 << ((packed & 7) + 1)) : 0);
                position++;
            }
            while (bytes[position] != 0) position += 1 + bytes[position];
            position++;
        }
        return result;
    }

    private static int ReadBig32(byte[] bytes, int offset) =>
        (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
    private static int ReadBig16(byte[] bytes, int offset) => (bytes[offset] << 8) | bytes[offset + 1];
    private static int ReadLittle16(byte[] bytes, int offset) => bytes[offset] | (bytes[offset + 1] << 8);

    private sealed class Controls {
        internal List<double> Durations { get; } = new();
        internal int? ContainerPlayCount { get; set; }
    }

    private sealed class CancellingStream : MemoryStream {
        private readonly CancellationTokenSource _cancellation;
        internal CancellingStream(CancellationTokenSource cancellation) => _cancellation = cancellation;
        public override void Write(byte[] buffer, int offset, int count) {
            base.Write(buffer, offset, count);
            _cancellation.Cancel();
        }
    }

    private sealed class RepeatedFrames : IReadOnlyList<RasterAnimationFrame> {
        private readonly RasterAnimationFrame _frame;
        private readonly CancellationTokenSource _cancellation;
        internal RepeatedFrames(RasterAnimationFrame frame, int count, CancellationTokenSource cancellation) {
            _frame = frame; Count = count; _cancellation = cancellation;
        }
        public int Count { get; }
        internal int LastRequestedIndex { get; private set; } = -1;
        public RasterAnimationFrame this[int index] {
            get {
                LastRequestedIndex = index;
                // Stop a regressed guard before the test can allocate and encode millions of indexed frames.
                if (index > 0) _cancellation.Cancel();
                return _frame;
            }
        }
        public IEnumerator<RasterAnimationFrame> GetEnumerator() {
            for (var index = 0; index < Count; index++) yield return this[index];
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
