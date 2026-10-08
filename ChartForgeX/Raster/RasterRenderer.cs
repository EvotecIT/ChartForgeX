using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Raster;

internal static class RasterRenderer {
    internal static RgbaImage RenderImage(Chart chart) {
        var request = VisualExportRequest.ForChart(chart);
        return chart.Prepare(request.Context).ToRgba(request.RasterOptions);
    }

    internal static RgbaImage RenderImage(ChartGrid grid) {
        var request = VisualExportRequest.ForGrid(grid);
        return grid.Prepare(request.Context).ToRgba(request.RasterOptions);
    }

    internal static RgbaImage RenderImage(IVisualBlock block) => block.RenderRgba();

}
