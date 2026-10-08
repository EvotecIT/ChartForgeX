using System;

namespace ChartForgeX.Raster;

internal enum RasterImageFit { Stretch, Contain, Cover, Center, Tile }

/// <summary>Places raster pixels once for static composition and story surfaces, preserving integer fit rounding.</summary>
internal static class RasterImageFitting {
    internal static void Draw(RgbaCanvas canvas, int sourceWidth, int sourceHeight, byte[] rgba,
        int x, int y, int width, int height, RasterImageFit fit) {
        switch (fit) {
            case RasterImageFit.Contain:
                var containScale = Math.Min(width / (double)sourceWidth, height / (double)sourceHeight);
                var drawWidth = Math.Max(1, (int)Math.Round(sourceWidth * containScale));
                var drawHeight = Math.Max(1, (int)Math.Round(sourceHeight * containScale));
                canvas.DrawImageScaled(x + (width - drawWidth) / 2, y + (height - drawHeight) / 2,
                    drawWidth, drawHeight, sourceWidth, sourceHeight, rgba);
                return;
            case RasterImageFit.Cover:
                var coverScale = Math.Max(width / (double)sourceWidth, height / (double)sourceHeight);
                var cropWidth = Math.Min(sourceWidth, width / coverScale);
                var cropHeight = Math.Min(sourceHeight, height / coverScale);
                canvas.DrawImageScaled(x, y, width, height, sourceWidth, sourceHeight, rgba,
                    (sourceWidth - cropWidth) / 2, (sourceHeight - cropHeight) / 2, cropWidth, cropHeight);
                return;
            case RasterImageFit.Center:
                var centeredX = x + (width - sourceWidth) / 2;
                var centeredY = y + (height - sourceHeight) / 2;
                var left = Math.Max(x, centeredX); var top = Math.Max(y, centeredY);
                var right = Math.Min(x + width, centeredX + sourceWidth);
                var bottom = Math.Min(y + height, centeredY + sourceHeight);
                if (right <= left || bottom <= top) return;
                canvas.DrawImageScaled(left, top, right - left, bottom - top, sourceWidth, sourceHeight, rgba,
                    left - centeredX, top - centeredY, right - left, bottom - top);
                return;
            case RasterImageFit.Tile:
                for (var tileY = y; tileY < y + height; tileY += sourceHeight) {
                    var tileHeight = Math.Min(sourceHeight, y + height - tileY);
                    for (var tileX = x; tileX < x + width; tileX += sourceWidth) {
                        var tileWidth = Math.Min(sourceWidth, x + width - tileX);
                        canvas.DrawImageScaled(tileX, tileY, tileWidth, tileHeight, sourceWidth, sourceHeight,
                            rgba, 0, 0, tileWidth, tileHeight);
                    }
                }
                return;
            case RasterImageFit.Stretch:
                canvas.DrawImageScaled(x, y, width, height, sourceWidth, sourceHeight, rgba);
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(fit));
        }
    }
}
