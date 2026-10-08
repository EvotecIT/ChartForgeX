using System;
using System.Collections.Generic;
using System.IO;

namespace ChartForgeX.Raster;

/// <summary>Encodes static or animated pixel inputs using the Stories-owned GIF and APNG exporters.</summary>
public static class RasterStoryExtensions {
    /// <summary>Encodes one RGBA image as a single-frame GIF.</summary>
    public static byte[] ToGif(this RgbaImage image) => GifWriter.WriteRgba(new[] { image }, 10, false);
    /// <summary>Encodes ordered, equally sized frames as GIF.</summary>
    /// <param name="frames">One or more complete RGBA canvases with matching dimensions.</param>
    /// <param name="delayCentiseconds">A nonnegative delay applied to every frame, up to 65,535 centiseconds. Zero is preserved.</param>
    /// <param name="loop">Repeats indefinitely when true; otherwise plays once.</param>
    public static byte[] ToGif(this IReadOnlyList<RgbaImage> frames, int delayCentiseconds = 10, bool loop = true) =>
        EncodeFrames(frames, delayCentiseconds, loop, RasterAnimationFormat.Gif);
    /// <summary>Encodes ordered, equally sized frames as animated PNG.</summary>
    /// <param name="frames">One or more complete RGBA canvases with matching dimensions.</param>
    /// <param name="delayCentiseconds">A nonnegative delay applied to every frame, up to 6,553,500 centiseconds. Zero is preserved.</param>
    /// <param name="loop">Repeats indefinitely when true; otherwise plays once.</param>
    public static byte[] ToApng(this IReadOnlyList<RgbaImage> frames, int delayCentiseconds = 10, bool loop = true) =>
        EncodeFrames(frames, delayCentiseconds, loop, RasterAnimationFormat.Apng);
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

    private static byte[] EncodeFrames(IReadOnlyList<RgbaImage> frames, int delayCentiseconds, bool loop, RasterAnimationFormat format) {
        if (frames == null) throw new ArgumentNullException(nameof(frames));
        if (delayCentiseconds < 0) {
            throw new ArgumentOutOfRangeException(nameof(delayCentiseconds), delayCentiseconds, "Frame delay must be nonnegative.");
        }
        var duration = TimeSpan.FromMilliseconds(delayCentiseconds * 10d);
        var timedFrames = new TimedImageFrames(frames, duration);
        return RasterAnimationEncoder.Encode(timedFrames, format, new RasterAnimationOptions { PlayCount = loop ? 0 : 1 });
    }

    // Borrow the image list so the encoder can reject oversized collections before allocating timing arrays.
    private sealed class TimedImageFrames : IReadOnlyList<RasterAnimationFrame> {
        private readonly IReadOnlyList<RgbaImage> _frames;
        private readonly TimeSpan _duration;
        internal TimedImageFrames(IReadOnlyList<RgbaImage> frames, TimeSpan duration) {
            _frames = frames;
            _duration = duration;
        }
        public int Count => _frames.Count;
        public RasterAnimationFrame this[int index] => new RasterAnimationFrame(_frames[index], _duration);
        public IEnumerator<RasterAnimationFrame> GetEnumerator() {
            for (var index = 0; index < Count; index++) yield return this[index];
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
