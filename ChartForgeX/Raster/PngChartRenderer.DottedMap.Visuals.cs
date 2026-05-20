using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static bool ShouldDrawDottedMapLandDot(ChartMapViewport viewport, ChartColor plotBackground, double longitude, double latitude) {
        var hasVectorGeography = DottedMapBoundaryLines(viewport).Length > 0 || DottedMapViewportOutlines(viewport).Length > 0;
        return !ChartDottedMapSurface.IsLightSurface(plotBackground) || !hasVectorGeography;
    }
}
