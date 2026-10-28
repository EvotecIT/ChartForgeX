using System;
using System.Threading;

namespace ChartForgeX.Raster;

internal static partial class RasterEffects {
    /// <summary>Copies a fixed explicit frame from propagated kernel support; exact matching results retain their owned array.</summary>
    internal static RgbaImage Crop(RasterEffectResult filtered, RasterEffectRegion frame, CancellationToken cancellation) {
        cancellation.ThrowIfCancellationRequested();
        var region = filtered.Region;
        if (region.X == frame.X && region.Y == frame.Y && region.Width == frame.Width && region.Height == frame.Height)
            return filtered.Image;
        var final = new byte[checked(frame.Width * frame.Height * 4)];
        for (var row = 0; row < frame.Height; row++) {
            cancellation.ThrowIfCancellationRequested();
            var from = checked(((frame.Y + row - region.Y) * filtered.Image.Width + frame.X - region.X) * 4);
            Buffer.BlockCopy(filtered.Image.Pixels, from, final, row * frame.Width * 4, frame.Width * 4);
        }
        return new RgbaImage(frame.Width, frame.Height, final);
    }
}
