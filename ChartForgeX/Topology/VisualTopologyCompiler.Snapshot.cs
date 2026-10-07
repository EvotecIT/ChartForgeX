using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    internal TopologyChart LayoutSnapshot() {
        var snapshot = TopologyLayoutEngine.Clone(_chart);
        snapshot.Title = _source.Title; snapshot.Subtitle = _source.Subtitle;
        snapshot.Legend = _legend == null ? null : TopologyLegend.Clone(_legend);
        snapshot.Accessibility.Name = _source.Accessibility.Name; snapshot.Accessibility.Description = _source.Accessibility.Description;
        snapshot.Accessibility.Language = _source.Accessibility.Language; snapshot.Accessibility.IsDecorative = _source.Accessibility.IsDecorative;
        return snapshot;
    }

    internal VisualRenderContext Context => _context;
    internal TopologyRenderOptions OptionsSnapshot() => _options.CloneForRendering();
}
