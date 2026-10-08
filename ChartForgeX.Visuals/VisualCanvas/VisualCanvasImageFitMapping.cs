using System;
using ChartForgeX.Raster;

namespace ChartForgeX.Composition;

/// <summary>Maps canvas presentation policy onto the shared native raster placement geometry.</summary>
internal static class VisualCanvasImageFitMapping {
    internal static RasterImageFit Resolve(VisualCanvasImageFit fit) => fit switch {
        VisualCanvasImageFit.Stretch => RasterImageFit.Stretch,
        VisualCanvasImageFit.Contain => RasterImageFit.Contain,
        VisualCanvasImageFit.Cover => RasterImageFit.Cover,
        VisualCanvasImageFit.Center => RasterImageFit.Center,
        VisualCanvasImageFit.Tile => RasterImageFit.Tile,
        _ => throw new ArgumentOutOfRangeException(nameof(fit), fit, "Unknown image fit mode.")
    };
}
