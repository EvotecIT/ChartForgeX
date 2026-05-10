using ChartForgeX.Core;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Raster;

internal static class RasterRenderer {
    internal static RgbaImage RenderImage(Chart chart) => new PngChartRenderer().RenderImage(chart);

    internal static RgbaImage RenderImage(ChartGrid grid) => new PngChartGridRenderer().RenderImage(grid);

    internal static RgbaImage RenderImage(IVisualBlock block) => new PngVisualBlockRenderer().RenderImage(block);

    internal static RgbaImage RenderImage(VisualGrid grid) => new PngVisualGridRenderer().RenderImage(grid);
}
