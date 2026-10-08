using System;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    /// <summary>Captures the final frame transform so detached diagnostics use the same coordinates as the scene.</summary>
    internal Func<TopologyLayoutDiagnosticReport> DiagnosticsSnapshot(TopologyChart chart, TopologyRenderOptions options) {
        var size = _context.Layout.Size; var scale = _scale; var offsetX = _offsetX; var offsetY = _offsetY;
        return () => TopologyLayoutDiagnostics.AnalyzeOutput(chart, options, size, scale, offsetX, offsetY);
    }

    /// <summary>Retains only existing detached preparation data and scalar fit state until geometry is requested.</summary>
    internal Lazy<ResolvedTopologyGeometry> GeometrySnapshot(TopologyChart chart, TopologyRenderOptions options) {
        // Capture locals so the factory retains neither the compiler nor its transient font/layout resources.
        var context = _context; var clip = _plot; var scale = _scale;
        var offsetX = _offsetX; var offsetY = _offsetY; var background = _colors.Background;
        return new Lazy<ResolvedTopologyGeometry>(() => ResolvedTopologyGeometry.Create(
            chart, options, context, clip, scale, offsetX, offsetY, background));
    }
}
