using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using ChartForgeX.Raster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RasterDecodeLimitTests {
    [Fact]
    public void PngInflationRejectsExcessDataWithoutAllocatingTheInflatedPayload() {
        var encoded = Png(1, 1, new byte[4 * 1024 * 1024]);
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        Assert.Throws<InvalidDataException>(() => RasterImageDecoder.Decode(encoded));
        allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
        Assert.True(allocated < 1024 * 1024, "A 1x1 PNG must not allocate its 4 MiB excess scanline payload.");
        Assert.False(RasterImageDecoder.TryDecode(encoded, out _));
    }

    [Theory]
    [InlineData(0xC0, 0xC0)]
    [InlineData(0xC0, 0xC2)]
    [InlineData(0xC2, 0xC0)]
    [InlineData(0xC2, 0xC2)]
    public void JpegRejectsAdditionalFramesBeforeAllocatingTheirComponentBuffers(int first, int subsequent) {
        using var input = new MemoryStream(); input.Write(new byte[] { 255, 216 });
        for (var frame = 0; frame < 100; frame++) {
            var marker = (byte)(frame == 0 ? first : subsequent);
            input.Write(new byte[] { 255, marker, 0, 11, 8, 1, 0, 1, 0, 1, 1, 17, 0, 255, 218, 0, 8, 1, 1, 0, 0, marker == 0xC0 ? (byte)63 : (byte)0, 0 });
        }
        input.Write(new byte[] { 255, 217 });
        var encoded = input.ToArray();
        var limits = new RasterDecodeOptions { MaximumPixels = 65536, MaximumEncodedBytes = 8192 };
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        var error = Assert.Throws<InvalidDataException>(() => RasterImageDecoder.Decode(encoded, limits));
        allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
        Assert.Contains("one frame header", error.Message);
        Assert.True(allocated < 1024 * 1024, "Repeated frame headers must not retain additional JPEG component arrays.");
        Assert.False(RasterImageDecoder.TryDecode(encoded, limits, out _));
    }

    [Fact]
    public void EncodedLimitsApplyToBytesFilesAndNonSeekableStreams() {
        var encoded = Png(1, 1, new byte[] { 0, 255, 0, 0, 255 });
        var options = new RasterDecodeOptions { MaximumEncodedBytes = encoded.Length, MaximumPixels = 1 };
        Assert.Equal(new byte[] { 255, 0, 0, 255 }, RasterImageDecoder.Decode(encoded, options).Pixels);
        using var input = new NonSeekableStream(encoded);
        Assert.Equal(1, RasterImageDecoder.Read(input, options).Width);
        Assert.True(input.CanRead);
        options.MaximumEncodedBytes--;
        Assert.False(RasterImageDecoder.TryDecode(encoded, options, out _));
        using var overlong = new NonSeekableStream(encoded);
        Assert.False(RasterImageDecoder.TryRead(overlong, options, out _));
        var path = Path.Combine(Path.GetTempPath(), "cfx-input-" + Guid.NewGuid().ToString("N") + ".png");
        try { File.WriteAllBytes(path, encoded); Assert.False(RasterImageDecoder.TryRead(path, options, out _)); }
        finally { File.Delete(path); }
    }

    [Theory]
    [MemberData(nameof(TwoPixelInputs))]
    public void EveryCodecRejectsDimensionsAboveTheHostsPixelLimit(byte[] encoded) {
        var options = new RasterDecodeOptions { MaximumPixels = 1 };
        var error = Assert.Throws<InvalidDataException>(() => RasterImageDecoder.Decode(encoded, options));
        Assert.Contains("pixel limit", error.Message);
        Assert.False(RasterImageDecoder.TryDecode(encoded, options, out _));
    }

    public static IEnumerable<object[]> TwoPixelInputs() {
        yield return new object[] { Png(2, 1, new byte[9]) };
        yield return new object[] { Encoding.ASCII.GetBytes("P3\n2 1\n255\n255 0 0 0 255 0") };
        var bmp = new byte[62]; bmp[0] = (byte)'B'; bmp[1] = (byte)'M';
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(10), 54); BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(14), 40);
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(18), 2); BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(22), 1);
        bmp[26] = 1; bmp[28] = 24;
        yield return new object[] { bmp };
        yield return new object[] { new byte[] { 71, 73, 70, 56, 57, 97, 2, 0, 1, 0, 0, 0, 0 } };
        yield return new object[] { new byte[] { 255, 216, 255, 192, 0, 11, 8, 0, 1, 0, 2, 1, 1, 17, 0, 255, 217 } };
        using var tiff = new MemoryStream(); using var writer = new BinaryWriter(tiff);
        writer.Write(new byte[] { 73, 73, 42, 0 }); writer.Write(8); writer.Write((ushort)4);
        foreach (var (tag, value) in new[] { (256, 2), (257, 1), (273, 62), (279, 6) }) { writer.Write((ushort)tag); writer.Write((ushort)4); writer.Write(1); writer.Write(value); }
        writer.Write(0); writer.Write(new byte[6]);
        yield return new object[] { tiff.ToArray() };
    }

    [Theory]
    [InlineData(12)]
    [InlineData(14)]
    public void MalformedPngHeaderLengthIsRejectedBeforeHeaderFieldsAreRead(int length) {
        using var encoded = new MemoryStream(); encoded.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        Chunk(encoded, "IHDR", new byte[length]);
        Assert.Throws<InvalidDataException>(() => RasterImageDecoder.Decode(encoded.ToArray()));
    }

    private static byte[] Png(int width, int height, byte[] raw) {
        using var payload = new MemoryStream();
        using (var zlib = new ZLibStream(payload, CompressionLevel.SmallestSize, true)) zlib.Write(raw);
        using var encoded = new MemoryStream(); encoded.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var header = new byte[13]; BinaryPrimitives.WriteInt32BigEndian(header, width); BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height); header[8] = 8; header[9] = 6;
        Chunk(encoded, "IHDR", header); Chunk(encoded, "IDAT", payload.ToArray()); Chunk(encoded, "IEND", Array.Empty<byte>());
        return encoded.ToArray();
    }

    private static void Chunk(Stream output, string type, byte[] payload) {
        Span<byte> number = stackalloc byte[4]; BinaryPrimitives.WriteInt32BigEndian(number, payload.Length); output.Write(number);
        var name = Encoding.ASCII.GetBytes(type); output.Write(name); output.Write(payload);
        uint crc = 0xffffffff;
        foreach (var value in name.Concat(payload)) { crc ^= value; for (var bit = 0; bit < 8; bit++) crc = (crc & 1) != 0 ? 0xedb88320u ^ (crc >> 1) : crc >> 1; }
        BinaryPrimitives.WriteUInt32BigEndian(number, crc ^ 0xffffffff); output.Write(number);
    }

    private sealed class NonSeekableStream : Stream {
        private readonly MemoryStream _inner;
        internal NonSeekableStream(byte[] data) => _inner = new MemoryStream(data);
        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing) { if (disposing) _inner.Dispose(); base.Dispose(disposing); }
    }
}
