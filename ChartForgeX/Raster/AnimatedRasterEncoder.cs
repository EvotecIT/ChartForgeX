using System;
using System.IO;

namespace ChartForgeX.Raster;

internal enum AnimatedRasterFormat {
    Gif,
    Apng
}

internal static class AnimatedRasterFormatExtensions {
    public static bool TryFromFileExtension(string extension, out AnimatedRasterFormat format) {
        switch ((extension ?? string.Empty).Trim().ToLowerInvariant()) {
            case ".gif":
                format = AnimatedRasterFormat.Gif;
                return true;
            case ".apng":
                format = AnimatedRasterFormat.Apng;
                return true;
            default:
                format = default;
                return false;
        }
    }

    public static string GetDisplayName(this AnimatedRasterFormat format) {
        switch (format) {
            case AnimatedRasterFormat.Gif:
                return "GIF";
            case AnimatedRasterFormat.Apng:
                return "APNG";
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported animated raster format.");
        }
    }
}

internal static class AnimatedRasterEncoder {
    public static byte[] Encode(AnimatedRasterFormat format, AnimatedRasterFrames frames) {
        using var stream = new MemoryStream();
        Write(stream, format, frames);
        return stream.ToArray();
    }

    public static void Write(Stream stream, AnimatedRasterFormat format, AnimatedRasterFrames frames) {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (frames == null) throw new ArgumentNullException(nameof(frames));
        switch (format) {
            case AnimatedRasterFormat.Gif:
                GifWriter.WriteRgba(stream, frames);
                break;
            case AnimatedRasterFormat.Apng:
                ApngWriter.WriteRgba(stream, frames);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported animated raster format.");
        }
    }

    /// <summary>Renders and encodes APNG frames one at a time into a bounded output buffer.</summary>
    internal static byte[] EncodeStreamedApng(
        int width,
        int height,
        int frameCount,
        int delayCentiseconds,
        bool loop,
        long maximumEncodedBytes,
        Func<int, RgbaImage> renderFrame) {
        using var stream = new BoundedChunkStream(maximumEncodedBytes);
        ApngWriter.WriteRgba(
            stream,
            width,
            height,
            frameCount,
            delayCentiseconds,
            loop,
            renderFrame);
        return stream.ToArray();
    }
}
