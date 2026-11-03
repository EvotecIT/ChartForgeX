using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private bool TrimNaturalFrame => _source.LayoutMode is TopologyLayoutMode.Layered or TopologyLayoutMode.MindMap or TopologyLayoutMode.Swimlane
        && _source.Nodes.All(node => !node.HasPositionOverride && Math.Abs(node.X) < .0001 && Math.Abs(node.Y) < .0001)
        && _source.Groups.All(group => !group.HasPositionOverride && group.X == 0 && group.Y == 0 && group.Width <= 0 && group.Height <= 0);
    // Natural automatic diagrams retain their measured typography and route geometry.
    // The requested viewport is a layout constraint, not invisible content to preserve.
    private void FitNaturalContentFrame(IReadOnlyList<VisualLegendEntry> entries) {
        var content = IncludeAnnotationBounds(MeasureRenderedContent(0, 0, _chart.Viewport.Width, _chart.Viewport.Height));
        var typography = _context.Theme.Typography;
        var headingWidth = Math.Max(string.IsNullOrWhiteSpace(_source.Title) ? 0 : _builder.MeasureText(_source.Title!, typography.TitleSize, 600).Width,
            string.IsNullOrWhiteSpace(_source.Subtitle) ? 0 : _builder.MeasureText(_source.Subtitle!, typography.SubtitleSize, 400).Width);
        var horizontal = _context.Layout.Size.Width - _plot.Width;
        var vertical = _context.Layout.Size.Height - _plot.Height;
        Resize(Math.Max(content.Width, headingWidth) + horizontal, content.Height + vertical);
        // A changed width can unwrap the shared frame heading. Resolve its final reservation
        // against the same content without running layout/routing or scaling text again.
        for (var pass = 0; pass < 32; pass++) {
            var missing = content.Height - _plot.Height;
            if (missing <= .001) break;
            Resize(_context.Layout.Size.Width, _context.Layout.Size.Height + Math.Max(1, missing));
        }

        void Resize(double width, double height) {
            _context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(Math.Max(1, Math.Ceiling(width)), Math.Max(1, Math.Ceiling(height))), _context.Layout.PaddingEdges),
                _context.Theme, _context.ThemeMode, _context.Frame, _context.Font).WithNativeTextCapture(_context.NativeTextCapture);
            _builder = new VisualSceneBuilder(_context.Layout.Size, _context.Font, _context.NativeTextCapture);
            _plot = VisualFrameLayout.Build(_builder, _context, entries, FramePaints());
        }
    }
}
