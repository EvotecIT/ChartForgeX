using ChartForgeX.Raster;

namespace ChartForgeX.Topology;

public static partial class TopologyChartExtensions {
    /// <summary>Renders a topology chart directly to an in-memory RGBA image without encoding it.</summary>
    /// <param name="chart">The topology chart to render.</param>
    /// <param name="options">Optional render options.</param>
    /// <returns>The rendered RGBA image.</returns>
    public static RgbaImage ToRgbaImage(this TopologyChart chart, TopologyRenderOptions? options = null) => TopologyRasterRenderer.RenderImage(chart, options);
}
