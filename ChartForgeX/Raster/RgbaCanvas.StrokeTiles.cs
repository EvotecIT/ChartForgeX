using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal sealed partial class RgbaCanvas {
    /// <summary>Bounds dashed outlines by disjoint pixel tiles, preserving one blend per painted pixel.</summary>
    internal void PaintStrokeTiles(bool dashed, Action<ChartRect, RasterStroker.DashBudget?> paint) {
        var left = _hasClip ? Math.Max(0, _clipLeft) : 0;
        var top = _hasClip ? Math.Max(0, _clipTop) : 0;
        var right = _hasClip ? Math.Min(_pixelWidth, _clipRight) : _pixelWidth;
        var bottom = _hasClip ? Math.Min(_pixelHeight, _clipBottom) : _pixelHeight;
        if (right <= left || bottom <= top) return;
        var tileWidth = dashed ? 2048 : right - left;
        var tileHeight = dashed ? 2048 : bottom - top;
        var budget = dashed ? new RasterStroker.DashBudget(_scale) : null;
        for (var y = top; y < bottom; y += tileHeight) for (var x = left; x < right; x += tileWidth) {
            var bounds = new ChartRect(x / (double)_scale, y / (double)_scale,
                Math.Min(tileWidth, right - x) / (double)_scale, Math.Min(tileHeight, bottom - y) / (double)_scale);
            using (PushClipBounds(bounds)) paint(bounds, budget);
        }
    }
}
