using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private IReadOnlyDictionary<string, IReadOnlyList<ChartRect>> CaptureGroupHeaderParts() =>
        new ReadOnlyDictionary<string, IReadOnlyList<ChartRect>>(_groupHeaderParts.ToDictionary(entry => entry.Key,
            entry => (IReadOnlyList<ChartRect>)Array.AsReadOnly(entry.Value.ToArray()), StringComparer.Ordinal));

    internal TopologyChart LayoutSnapshot() {
        var snapshot = TopologyLayoutEngine.Clone(_chart);
        // Routing must replay the same policy reservations it saw before native painting captured
        // the actual glyph bounds. Keep both immutable observations rather than rerouting at capture.
        snapshot.PreparedGroupHeaderLayoutParts = new ReadOnlyDictionary<string, IReadOnlyList<ChartRect>>(
            _chart.Groups.ToDictionary(group => group.Id,
                group => (IReadOnlyList<ChartRect>)Array.AsReadOnly(TopologyGroupHeader.LayoutParts(_chart, group, _options).ToArray()), StringComparer.Ordinal));
        // The common frame owns headings and legend. Reintroducing them into this domain
        // would reserve their space again when diagnostics replay the canonical route plan.
        snapshot.Title = null; snapshot.Subtitle = null; snapshot.Legend = null;
        snapshot.Accessibility.Name = _source.Accessibility.Name; snapshot.Accessibility.Description = _source.Accessibility.Description;
        snapshot.Accessibility.Language = _source.Accessibility.Language; snapshot.Accessibility.IsDecorative = _source.Accessibility.IsDecorative;
        return snapshot;
    }

    internal VisualRenderContext Context {
        get {
            var captured=_context.Font.NativeTextCapture?.Freeze();
            var font=captured==null?_context.Font:_context.Font.WithCapturedText(captured);
            return new VisualRenderContext(_context.Layout,_context.Theme,_context.ThemeMode,_frameRequest.RequestValues(retainOwnedFonts:true),font)
                .WithNativeTextCapture(captured);
        }
    }
    internal string? SourceTitle => HeadingOrSource(_options.View?.Title, _source.Title);
    internal string? SourceSubtitle => HeadingOrSource(_options.View?.Subtitle, _source.Subtitle);
    internal TopologyLegend? FrameLegend => _legend == null ? null : TopologyLegend.Clone(_legend);
    internal TopologyRenderOptions OptionsSnapshot() => _options.CloneForRendering();
}
