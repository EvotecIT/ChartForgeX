using System;
using System.Collections.Generic;
using System.IO;

namespace ChartForgeX.Raster;

/// <summary>
/// Retains encoded animation output in fixed-size chunks, rejects growth past a
/// caller-supplied budget, and materializes one exact final byte array.
/// </summary>
internal sealed class BoundedChunkStream : Stream {
    internal const int ChunkSize = 64 * 1024;

    private readonly List<byte[]> _chunks = new();
    private readonly long _maximumLength;
    private long _length;

    internal BoundedChunkStream(long maximumLength) {
        if (maximumLength <= 0 || maximumLength > int.MaxValue) {
            throw new ArgumentOutOfRangeException(nameof(maximumLength));
        }
        _maximumLength = maximumLength;
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => _length;

    public override long Position {
        get => _length;
        set => throw new NotSupportedException();
    }

    public override void Flush() {
    }

    public override void Write(byte[] buffer, int offset, int count) {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
        if (offset < 0 || count < 0 || offset > buffer.Length - count) {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }
        EnsureCapacity(count);
        var source = offset;
        var remaining = count;
        while (remaining > 0) {
            var chunkIndex = checked((int)(_length / ChunkSize));
            var chunkOffset = checked((int)(_length % ChunkSize));
            if (chunkIndex == _chunks.Count) {
                _chunks.Add(new byte[ChunkSize]);
            }
            var writable = Math.Min(remaining, ChunkSize - chunkOffset);
            Buffer.BlockCopy(buffer, source, _chunks[chunkIndex], chunkOffset, writable);
            source += writable;
            remaining -= writable;
            _length += writable;
        }
    }

    public override void WriteByte(byte value) {
        EnsureCapacity(1);
        var chunkIndex = checked((int)(_length / ChunkSize));
        var chunkOffset = checked((int)(_length % ChunkSize));
        if (chunkIndex == _chunks.Count) {
            _chunks.Add(new byte[ChunkSize]);
        }
        _chunks[chunkIndex][chunkOffset] = value;
        _length++;
    }

    internal byte[] ToArray() {
        var output = new byte[checked((int)_length)];
        var destination = 0;
        foreach (var chunk in _chunks) {
            var count = Math.Min(ChunkSize, output.Length - destination);
            if (count <= 0) break;
            Buffer.BlockCopy(chunk, 0, output, destination, count);
            destination += count;
        }
        return output;
    }

    private void EnsureCapacity(int additionalBytes) {
        var required = checked(_length + additionalBytes);
        if (required > _maximumLength) {
            throw new InvalidOperationException(
                "Animated PNG output exceeds the bounded encoded-stream budget of " +
                _maximumLength + " bytes. Lower the size, scale, frame rate, duration, or visual complexity.");
        }
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
}
