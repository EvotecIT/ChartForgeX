using ChartForgeX.Raster;

namespace ChartForgeX.Topology;

internal static class TopologyRasterRenderer {
    internal static RgbaImage RenderImage(TopologyChart chart, TopologyRenderOptions? options = null) => new TopologyPngRenderer().RenderImage(chart, options);
}
