using System;
using System.Collections.Generic;
using System.IO;

namespace ChartForgeX.Raster;

/// <summary>Encodes static or animated pixel inputs using the Stories-owned GIF and APNG exporters.</summary>
public static class RasterStoryExtensions {
    /// <summary>Encodes one RGBA image as a single-frame GIF.</summary>
    public static byte[] ToGif(this RgbaImage image) => GifWriter.WriteRgba(new[] { image }, 10, false);
    /// <summary>Encodes ordered, equally sized frames as GIF.</summary>
    public static byte[] ToGif(this IReadOnlyList<RgbaImage> frames, int delayCentiseconds = 10, bool loop = true) =>
        GifWriter.WriteRgba(frames, delayCentiseconds, loop);
    /// <summary>Encodes ordered, equally sized frames as animated PNG.</summary>
    public static byte[] ToApng(this IReadOnlyList<RgbaImage> frames, int delayCentiseconds = 10, bool loop = true) =>
        ApngWriter.WriteRgba(frames, delayCentiseconds, loop);
    /// <summary>Writes one RGBA image as a single-frame GIF.</summary>
    public static void WriteGif(this RgbaImage image, Stream stream) {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        GifWriter.WriteRgba(stream, new[] { image }, 10, false);
    }
    /// <summary>Saves one RGBA image as a single-frame GIF.</summary>
    public static void SaveGif(this RgbaImage image, string path) {
        if (path == null) throw new ArgumentNullException(nameof(path));
        var bytes = image.ToGif();
        File.WriteAllBytes(path, bytes);
    }
}
