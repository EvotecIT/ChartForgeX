using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.SvgRaster;

internal static partial class SvgRasterRenderer {
    private static void PaintLayers(RgbaCanvas canvas, IReadOnlyList<List<ChartPoint>> fills,
        IReadOnlyList<List<ChartPoint>> strokes, SvgRasterStyle style, SvgRasterMatrix matrix,
        SvgRasterDefinitions definitions, SvgRasterViewport viewport, Action? renderMarkers = null) {
        for (int layer = 0; layer < 3; layer++) {
            if (layer == style.FillOrder) Fill(canvas, fills, style, matrix, definitions, viewport);
            else if (layer == style.StrokeOrder) foreach (var contour in strokes) Stroke(canvas, contour, style, matrix.ScaleFactor, definitions);
            else renderMarkers?.Invoke();
        }
    }
}
