using System;
using ChartForgeX.Core;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static bool ShouldDrawDottedMapLandDot(ChartMapViewport viewport, double longitude, double latitude) {
        var boundaries = DottedMapBoundaryLines(viewport);
        var hasClosedBoundary = false;
        foreach (var boundary in boundaries) {
            if (!CanFillDottedMapBoundary(boundary)) continue;
            hasClosedBoundary = true;
        }

        return !hasClosedBoundary;
    }
}
