using System;
using System.IO;

namespace ChartForgeX.Raster;

internal static partial class PngWriter {
    /// <summary>Bounds only the actual PNG buffers used by an image-in-SVG request; ordinary PNG export is unchanged.</summary>
    internal static byte[] WriteRgbaBounded(RgbaImage image, long maximumBytes, Func<long, Exception> overflow) {
        if (overflow == null) throw new ArgumentNullException(nameof(overflow));
        // Signature + IHDR + IDAT framing + IEND. The zlib payload receives the remaining budget.
        if (maximumBytes > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(maximumBytes));
        if (maximumBytes < 57) throw overflow(57);
        return WriteRgbaCore(image.Width, image.Height, image.Pixels, null, maximumBytes, overflow);
    }

    /// <summary>Checks a compressed write before its MemoryStream can expand. It does not own the destination.</summary>
    private sealed class BoundedWriteStream : Stream {
        private readonly MemoryStream _destination;
        private readonly long _maximum;
        private readonly Func<long, Exception> _overflow;
        internal BoundedWriteStream(MemoryStream destination, long maximum, Func<long, Exception> overflow) {
            _destination = destination; _maximum = maximum; _overflow = overflow;
        }
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => _destination.Length;
        public override long Position { get => _destination.Position; set => throw new NotSupportedException(); }
        public override void Flush() => _destination.Flush();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || count < 0 || offset > buffer.Length - count) throw new ArgumentOutOfRangeException(nameof(count));
            Reserve(count); _destination.Write(buffer, offset, count);
        }
        public override void WriteByte(byte value) { Reserve(1); _destination.WriteByte(value); }
        private void Reserve(int count) {
            var observed = _destination.Length + count;
            if (observed > _maximum) throw _overflow(observed);
            // Prevent MemoryStream's geometric growth from reserving capacity beyond this compressed-buffer cap.
            if (observed > _destination.Capacity) _destination.Capacity = (int)Math.Min(_maximum,
                Math.Max(observed, Math.Max(256L, (long)_destination.Capacity * 2)));
        }
    }
}
