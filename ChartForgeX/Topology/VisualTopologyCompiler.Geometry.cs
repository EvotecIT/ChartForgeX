using System;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    /// <summary>Retains only existing detached preparation data and scalar fit state until geometry is requested.</summary>
    internal Lazy<ResolvedTopologyGeometry> GeometrySnapshot(TopologyChart chart, TopologyRenderOptions options) {
        // Capture locals so the factory retains neither the compiler nor its transient font/layout resources.
        var context = _context; var clip = _plot; var scale = _scale;
        var offsetX = _offsetX; var offsetY = _offsetY; var background = _colors.Background;
        return new Lazy<ResolvedTopologyGeometry>(() => ResolvedTopologyGeometry.Create(
            chart, options, context, clip, scale, offsetX, offsetY, background));
    }
}
