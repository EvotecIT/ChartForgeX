using System;
using ChartForgeX.Raster;

namespace ChartForgeX.Topology;

/// <summary>Exports topology PNG from the shared native scene compiler.</summary>
public sealed class TopologyPngRenderer {
    /// <summary>Resolves one detached scene and paints its PNG through the common raster backend.</summary>
    public byte[] Render(TopologyChart chart, TopologyRenderOptions? options = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        return chart.Prepare(options).ToPng();
    }

    internal RgbaImage RenderImage(TopologyChart chart, TopologyRenderOptions? options = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        return chart.Prepare(options).ToRgba();
    }
}
