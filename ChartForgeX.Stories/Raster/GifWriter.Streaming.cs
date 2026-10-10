using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace ChartForgeX.Raster;

internal static partial class GifWriter {
    internal static void WriteSource(Stream stream, RasterAnimationSource source, int playCount, CancellationToken cancellationToken) {
        // All samples contribute to the same palette. No RGBA frame collection survives the pass.
        var palette = GifPaletteQuantizer.BuildPalette(Images(source, cancellationToken), cancellationToken);
        WriteAscii(stream, "GIF89a");
        WriteUInt16(stream, source.Width, "logical screen width");
        WriteUInt16(stream, source.Height, "logical screen height");
        stream.WriteByte(0xF7);
        stream.WriteByte(palette.HasTransparency ? (byte)palette.TransparentIndex : (byte)0);
        stream.WriteByte(0); WritePalette(stream, palette);
        if (playCount != 1) WriteLoopExtension(stream, playCount == 0 ? 0 : playCount - 1);
        byte[]? previous = null;
        var clock = new GifFrameClock();
        for (var i = 0; i < source.FrameCount; i++) {
            var frame = source.GetFrame(i, cancellationToken);
            var current = GifPaletteQuantizer.Quantize(frame.Image, palette, cancellationToken);
            var indexed = palette.HasTransparency || previous == null
                ? GifFrameOptimizer.FullFrame(frame.Image, current)
                : GifFrameOptimizer.DeltaFrame(frame.Image, current, previous);
            WriteFrame(stream, indexed, clock.Next(frame.Duration), palette, cancellationToken);
            previous = current;
        }
        stream.WriteByte(0x3B);
    }

    private static IEnumerable<RgbaImage> Images(RasterAnimationSource source, CancellationToken cancellationToken) {
        var clock = new GifFrameClock();
        for (var i = 0; i < source.FrameCount; i++) {
            var frame = source.GetFrame(i, cancellationToken);
            clock.Next(frame.Duration);
            yield return frame.Image;
        }
    }
}
