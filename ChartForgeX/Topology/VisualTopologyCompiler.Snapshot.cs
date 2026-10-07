using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    internal TopologyChart LayoutSnapshot() {
        var snapshot = TopologyLayoutEngine.Clone(_chart);
        // The common frame owns headings and legend. Reintroducing them into this domain
        // would reserve their space again when diagnostics replay the canonical route plan.
        snapshot.Title = null; snapshot.Subtitle = null; snapshot.Legend = null;
        snapshot.Accessibility.Name = _source.Accessibility.Name; snapshot.Accessibility.Description = _source.Accessibility.Description;
        snapshot.Accessibility.Language = _source.Accessibility.Language; snapshot.Accessibility.IsDecorative = _source.Accessibility.IsDecorative;
        return snapshot;
    }

    internal VisualRenderContext Context => _context;
    internal string? SourceTitle => _source.Title;
    internal string? SourceSubtitle => _source.Subtitle;
    internal TopologyLegend? FrameLegend => _legend == null ? null : TopologyLegend.Clone(_legend);
    internal TopologyRenderOptions OptionsSnapshot() => _options.CloneForRendering();
}
