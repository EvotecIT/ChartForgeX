using ChartForgeX.Composition;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class CompositionImageDecodeLimitTests {
    [Fact]
    public void FileEntryPointsRespectCallerLimitsBeforeChangingTheirDestination() {
        var path = Path.Combine(Path.GetTempPath(), "cfx-bounded-image-" + Guid.NewGuid().ToString("N") + ".bin");
        try {
            using (var file = File.Create(path)) file.SetLength(1025);
            var options = new RasterDecodeOptions { MaximumEncodedBytes = 1024 };
            var placement = new VisualCanvasPlacement(VisualCanvasAnchor.Center);
            var composition = ImageComposition.Create(32, 32, ChartColor.FromHex("#ff0000"));
            var before = composition.ToImage().Pixels;
            var canvas = VisualCanvas.Create(32, 32);
            Assert.Throws<InvalidDataException>(() => ImageComposition.FromFile(path, options));
            Assert.False(ImageComposition.TryFromFile(path, out _, options));
            Assert.Throws<InvalidDataException>(() => composition.DrawImageFile(path, 0, 0, 20, 20, options: options));
            Assert.Throws<InvalidDataException>(() => composition.DrawImageFile(path, placement, 20, 20, options: options));
            Assert.Throws<InvalidDataException>(() => canvas.AddImageFile(0, 0, 20, 20, path, options: options));
            Assert.Throws<InvalidDataException>(() => canvas.AddImageFile(placement, 20, 20, path, options: options));
            Assert.Throws<InvalidDataException>(() => canvas.AddHeroBadgeImageFile(0, 0, 20, 20, path, options: options));
            Assert.Throws<InvalidDataException>(() => canvas.AddHeroBadgeImageFile(placement, 20, 20, path, options: options));
            Assert.Equal(before, composition.ToImage().Pixels);
            Assert.Empty(canvas.Layers);
        } finally {
            File.Delete(path);
        }
    }

    [Fact]
    public void ByteAndFileEntryPointsDecodeValidImagesAndRespectPixelLimits() {
        var encoded = ImageComposition.Create(2, 2, ChartColor.FromHex("#ff0000")).ToPng();
        var options = new RasterDecodeOptions { MaximumEncodedBytes = encoded.Length, MaximumPixels = 4 };
        var path = Path.Combine(Path.GetTempPath(), "cfx-bounded-image-" + Guid.NewGuid().ToString("N") + ".png");
        try {
            File.WriteAllBytes(path, encoded);
            Assert.Equal(ImageComposition.FromBytes(encoded, options).ToImage().Pixels, ImageComposition.FromFile(path, options).ToImage().Pixels);
            Assert.True(ImageComposition.TryFromBytes(encoded, out _, options));
            var composition = ImageComposition.CreateTransparent(4, 4).DrawImageBytes(encoded, 0, 0, 4, 4, options: options);
            Assert.Equal(255, composition.ToImage().Pixels[0]);
            options.MaximumPixels = 3;
            Assert.Throws<InvalidDataException>(() => ImageComposition.FromBytes(encoded, options));
            Assert.Throws<InvalidDataException>(() => ImageComposition.FromFile(path, options));
            Assert.Throws<InvalidDataException>(() => VisualCanvas.Create(4, 4).AddImageBytes(0, 0, 4, 4, encoded, options: options));
        } finally {
            File.Delete(path);
        }
    }

    [Fact]
    public void SeekableReadBoundsTheRemainingInputAndLeavesTheStreamOpen() {
        var encoded = ImageComposition.Create(2, 2, ChartColor.FromHex("#ff0000")).ToPng();
        using var source = new MemoryStream();
        source.Write(new byte[128]);
        source.Write(encoded);
        source.Position = 128;
        var decoded = RasterImageDecoder.Read(source, new RasterDecodeOptions { MaximumEncodedBytes = encoded.Length });
        Assert.Equal(2, decoded.Width);
        Assert.True(source.CanRead);
        Assert.Equal(source.Length, source.Position);

        source.Position = 127;
        Assert.Throws<InvalidDataException>(() => RasterImageDecoder.Read(source, new RasterDecodeOptions { MaximumEncodedBytes = encoded.Length }));
        Assert.Equal(127, source.Position);
    }

    [Fact]
    public void NonSeekableReadStopsAtTheBudgetAndOneOverflowByte() {
        using var source = new NonSeekableInput(new byte[1026]);
        Assert.Throws<InvalidDataException>(() => RasterImageDecoder.Read(source, new RasterDecodeOptions { MaximumEncodedBytes = 1024 }));
        Assert.Equal(1025, source.BytesRead);
        Assert.True(source.CanRead);
    }

    private sealed class NonSeekableInput(byte[] bytes) : Stream {
        private readonly MemoryStream _source = new(bytes);
        internal int BytesRead { get; private set; }
        public override bool CanRead => _source.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) {
            var read = _source.Read(buffer, offset, count);
            BytesRead += read;
            return read;
        }
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing) {
            if (disposing) _source.Dispose();
            base.Dispose(disposing);
        }
    }
}
