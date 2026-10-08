using System;

namespace ChartForgeX.Topology;

/// <summary>Exports topology SVG from the shared native scene compiler.</summary>
public sealed class TopologySvgRenderer {
    /// <summary>Resolves one detached scene and serializes its SVG through the common backend.</summary>
    public string Render(TopologyChart chart, TopologyRenderOptions? options = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        return chart.Prepare(options).ToSvg();
    }
}
