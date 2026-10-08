using System;
using System.IO;

namespace ChartForgeX.Raster;

/// <summary>Bounds encoded input and decoded pixel count before raster buffers are allocated.</summary>
public sealed class RasterDecodeOptions {
    /// <summary>Gets or sets the encoded input limit in bytes. The default is 64 MiB.</summary>
    public int MaximumEncodedBytes { get; set; } = 64 * 1024 * 1024;

    /// <summary>Gets or sets the decoded pixel limit. The default is 67,108,864 pixels (256 MiB of RGBA output).</summary>
    /// <remarks>Codec working buffers may require additional memory. Hosts should lower this limit for untrusted uploads.</remarks>
    public int MaximumPixels { get; set; } = 64 * 1024 * 1024;
}

internal readonly struct RasterDecodeLimits {
    internal static RasterDecodeLimits Default => new(64 * 1024 * 1024, 64 * 1024 * 1024);
    private RasterDecodeLimits(int encodedBytes, int pixels) { MaximumEncodedBytes = encodedBytes; MaximumPixels = pixels; }
    internal int MaximumEncodedBytes { get; }
    internal int MaximumPixels { get; }

    internal static RasterDecodeLimits From(RasterDecodeOptions? options) {
        if (options == null) return Default;
        int encodedBytes = options.MaximumEncodedBytes, pixels = options.MaximumPixels;
        if (encodedBytes <= 0) throw new ArgumentOutOfRangeException(nameof(options), "The encoded input limit must be positive.");
        if (pixels <= 0 || pixels > int.MaxValue / 4) throw new ArgumentOutOfRangeException(nameof(options), "The pixel limit must be positive and fit an RGBA array.");
        return new RasterDecodeLimits(encodedBytes, pixels);
    }

    internal void ValidateInput(byte[] data) {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (data.Length > MaximumEncodedBytes) throw new InvalidDataException("Raster encoded input exceeds the configured byte limit.");
    }

    internal void ValidateDimensions(int width, int height) {
        if (width <= 0 || height <= 0 || (long)width * height > MaximumPixels)
            throw new InvalidDataException("Raster dimensions are invalid or exceed the configured pixel limit.");
    }

    internal byte[] Read(Stream stream) {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (stream.CanSeek && stream.Length - stream.Position > MaximumEncodedBytes) {
            throw new InvalidDataException("Raster encoded input exceeds the configured byte limit.");
        }
        using var output = new MemoryStream();
        var buffer = new byte[8192];
        while (output.Length < MaximumEncodedBytes) {
            int count = stream.Read(buffer, 0, (int)Math.Min(buffer.Length, MaximumEncodedBytes - output.Length));
            if (count == 0) return output.ToArray();
            output.Write(buffer, 0, count);
        }
        if (stream.ReadByte() != -1) throw new InvalidDataException("Raster encoded input exceeds the configured byte limit.");
        return output.ToArray();
    }
}
