using System;

namespace ChartForgeX.Raster;

internal sealed partial class RgbaCanvas {
    public void DrawImageScaledCircle(int x, int y, int destinationWidth, int destinationHeight, int sourceWidth, int sourceHeight, byte[] rgba) {
        if (rgba == null) throw new ArgumentNullException(nameof(rgba));
        if (destinationWidth <= 0 || destinationHeight <= 0 || sourceWidth <= 0 || sourceHeight <= 0) return;
        if (rgba.Length < sourceWidth * sourceHeight * 4) throw new ArgumentException("RGBA buffer is smaller than the requested source dimensions.", nameof(rgba));

        var clipped = new byte[sourceWidth * sourceHeight * 4];
        Buffer.BlockCopy(rgba, 0, clipped, 0, clipped.Length);
        var radius = Math.Min(destinationWidth, destinationHeight) / 2d;
        var centerX = destinationWidth / 2d;
        var centerY = destinationHeight / 2d;
        for (var sourceY = 0; sourceY < sourceHeight; sourceY++) for (var sourceX = 0; sourceX < sourceWidth; sourceX++) {
            var destinationX = (sourceX + 0.5) * destinationWidth / sourceWidth;
            var destinationY = (sourceY + 0.5) * destinationHeight / sourceHeight;
            var deltaX = destinationX - centerX;
            var deltaY = destinationY - centerY;
            if (deltaX * deltaX + deltaY * deltaY > radius * radius) clipped[(sourceY * sourceWidth + sourceX) * 4 + 3] = 0;
        }

        DrawImageScaled(x, y, destinationWidth, destinationHeight, sourceWidth, sourceHeight, clipped);
    }
}
