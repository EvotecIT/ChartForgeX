using System;
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
        GifPalette? previousPalette = null;
        GifPalette? pendingPalette = null;
        GifIndexedFrame pending = default;
        var pendingDelay = 0;
        var clock = new GifFrameClock();
        for (var i = 0; i < source.FrameCount; i++) {
            var frame = source.GetFrame(i, cancellationToken);
            // Reserve the transparent background in every palette so disposal can clear opaque-to-transparent transitions.
            // Low-colour frames remain lossless even when the complete animation exceeds the available colour slots.
            var currentPalette = palette.IsExact ? palette : GifPaletteQuantizer.TryExactPalette(frame.Image, cancellationToken, palette.HasTransparency) ?? palette;
            var current = GifPaletteQuantizer.Quantize(frame.Image, currentPalette, cancellationToken);
            var delay = clock.Next(frame.Duration);
            var same = previous != null && SamePixels(current, previous, currentPalette, previousPalette!, cancellationToken);
            if (same && pendingDelay > 0 && delay > 0 && pendingDelay + delay <= MaximumGifFieldValue) {
                pendingDelay += delay;
                continue;
            }
            if (pendingPalette != null) WriteFrame(stream, pending, pendingDelay, pendingPalette, cancellationToken, pendingPalette != palette);
            pending = currentPalette.HasTransparency || previousPalette?.HasTransparency == true || previous == null
                ? GifFrameOptimizer.FullFrame(frame.Image, current)
                : GifFrameOptimizer.DeltaFrame(frame.Image, current, previous, currentPalette, previousPalette!);
            pendingDelay = delay;
            pendingPalette = currentPalette;
            previous = current;
            previousPalette = currentPalette;
        }
        if (pendingPalette != null) WriteFrame(stream, pending, pendingDelay, pendingPalette, cancellationToken, pendingPalette != palette);
        stream.WriteByte(0x3B);
    }

    private static bool SamePixels(byte[] current, byte[] previous, GifPalette palette, GifPalette previousPalette, CancellationToken token) {
        for (var i = 0; i < current.Length; i++) {
            if ((i & 4095) == 0) token.ThrowIfCancellationRequested();
            if (!GifFrameOptimizer.SameColor(current[i], previous[i], palette, previousPalette)) return false;
        }
        return true;
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
