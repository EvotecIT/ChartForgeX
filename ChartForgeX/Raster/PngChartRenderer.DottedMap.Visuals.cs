using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static bool ShouldDrawDottedMapLandDot(ChartMapViewport viewport, ChartColor plotBackground, double longitude, double latitude) {
        if (ChartDottedMapSurface.IsLightSurface(plotBackground)) return false;
        return IsWorldMapViewport(viewport) || IsPolandDottedMapViewport(viewport);
    }
}
