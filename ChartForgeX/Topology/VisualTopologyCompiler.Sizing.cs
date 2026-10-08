using System;
using System.Collections.Generic;
using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private void ReserveNaturalFrame(IReadOnlyList<VisualLegendEntry> entries) {
        if (!_naturalSize || _options.FitContentToViewport || _resolvedLayout) return;
        // Measure through the common frame with enough room for its bounded headings. A tiny
        // natural canvas must reach canonical layout before its content can expand that canvas.
        var typography = _context.Theme.Typography;
        var probeHeight = _context.Layout.Size.Height + 4 * (typography.TitleSize + typography.SubtitleSize + _context.Theme.Spacing);
        var probeContext = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(_context.Layout.Size.Width, probeHeight), _context.Layout.PaddingEdges),
            _context.Theme, _context.ThemeMode, _context.Frame, _context.Font);
        var probe = VisualFrameLayout.Build(new VisualSceneBuilder(probeContext.Layout.Size, probeContext.Font), probeContext, entries, FramePaints());
        var height = Math.Max(_context.Layout.Size.Height, probeHeight - probe.Height + 1);
        _context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(_context.Layout.Size.Width, height), _context.Layout.PaddingEdges),
            _context.Theme, _context.ThemeMode, _context.Frame, _context.Font);
        _builder = new VisualSceneBuilder(_context.Layout.Size, _context.Font);
        _plot = VisualFrameLayout.Build(_builder, _context, entries, FramePaints());
    }

    private void ResolveNaturalSize(IReadOnlyList<VisualLegendEntry> entries) {
        if (!_naturalSize || _options.FitContentToViewport || _resolvedLayout) return;
        var padding = _chart.Viewport.Padding;
        var requiredWidth = Math.Max(1, _chart.Viewport.Width - padding * 2);
        var requiredHeight = Math.Max(1, _chart.Viewport.Height - padding * 2);
        var grew = false;
        // Growing the width can make a previously omitted heading fit, reserving more height.
        // Remeasure the same resolved content through the shared frame until that reservation
        // stabilizes; do not lay out or route the diagram a second time.
        for (var pass = 0; pass < 8; pass++) {
            var missingWidth = Math.Max(0, requiredWidth - _plot.Width);
            var missingHeight = Math.Max(0, requiredHeight - _plot.Height);
            if (missingWidth <= .001 && missingHeight <= .001) break;
            var width = _context.Layout.Size.Width + missingWidth;
            var height = _context.Layout.Size.Height + missingHeight;
            _context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, height), _context.Layout.PaddingEdges),
                _context.Theme, _context.ThemeMode, _context.Frame, _context.Font);
            _builder = new VisualSceneBuilder(_context.Layout.Size, _context.Font);
            _plot = VisualFrameLayout.Build(_builder, _context, entries, FramePaints());
            grew = true;
        }
        if (!grew) return;
        _builder.AddDiagnostic(new VisualDiagnostic("topology.natural-size", "The convenience export canvas includes the complete resolved topology and shared frame at natural size."));
    }

    private void ResolveFit() {
        var padding = _chart.Viewport.Padding;
        var width = Math.Max(1, _chart.Viewport.Width - padding * 2);
        var height = Math.Max(1, _chart.Viewport.Height - padding * 2);
        _offsetX = _plot.X - padding; _offsetY = _plot.Y - padding;
        // Output preservation may leave unused space. Fitting never enlarges or recenters an
        // already fitting diagram: its source coordinates remain the semantic coordinates.
        if (width <= _plot.Width + .001 && height <= _plot.Height + .001) return;
        if (!_resolvedLayout && !_options.FitContentToViewport)
            throw new NotSupportedException($"Prepared topology exceeds its fixed viewport: requires {width:0.##} x {height:0.##}, available {_plot.Width:0.##} x {_plot.Height:0.##}. Enlarge the common size or enable FitContentToViewport.");
        _scale = Math.Min(_plot.Width / width, _plot.Height / height);
        _offsetX = _plot.X + (_plot.Width - width * _scale) / 2 - padding * _scale;
        _offsetY = _plot.Y + (_plot.Height - height * _scale) / 2 - padding * _scale;
        _builder.AddDiagnostic(new VisualDiagnostic("topology.content-fitted", "The complete topology, including text and native semantic geometry, was uniformly fitted into the common content viewport."));
    }
}
