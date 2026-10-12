using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal sealed partial class RgbaCanvas {
    /// <summary>Paints disjoint device-pixel tiles with one blend per pixel and a shared work limit.</summary>
    internal void PaintStrokeTiles(bool tiled, double padding, Action<ChartRect, RasterStroker.DashBudget?> paint) {
        var left = _hasClip ? Math.Max(0, _clipLeft) : 0;
        var top = _hasClip ? Math.Max(0, _clipTop) : 0;
        var right = _hasClip ? Math.Min(_pixelWidth, _clipRight) : _pixelWidth;
        var bottom = _hasClip ? Math.Min(_pixelHeight, _clipBottom) : _pixelHeight;
        if (right <= left || bottom <= top) return;
        var tileWidth = tiled ? RasterStroker.DashTileSize : right - left;
        var tileHeight = tiled ? RasterStroker.DashTileSize : bottom - top;
        var budget = tiled ? new RasterStroker.DashBudget(right - left, bottom - top, padding) : null;
        for (var y = top; y < bottom; y += tileHeight) for (var x = left; x < right; x += tileWidth) {
            var bounds = new ChartRect(x / (double)_scale, y / (double)_scale,
                Math.Min(tileWidth, right - x) / (double)_scale, Math.Min(tileHeight, bottom - y) / (double)_scale);
            budget?.BeginTile();
            using (PushClipBounds(bounds)) paint(bounds, budget);
        }
    }
}
