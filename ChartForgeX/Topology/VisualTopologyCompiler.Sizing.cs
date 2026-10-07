using System;
using System.Collections.Generic;
using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private void ResolveNaturalSize(IReadOnlyList<VisualLegendEntry> entries) {
        if (!_naturalSize || _options.FitContentToViewport || _resolvedLayout) return;
        var padding = _chart.Viewport.Padding;
        var extraWidth = _context.Layout.Size.Width - _plot.Width - padding * 2;
        var extraHeight = _context.Layout.Size.Height - _plot.Height - padding * 2;
        var width = Math.Max(_context.Layout.Size.Width, _chart.Viewport.Width + extraWidth);
        var height = Math.Max(_context.Layout.Size.Height, _chart.Viewport.Height + extraHeight);
        if (width <= _context.Layout.Size.Width + .001 && height <= _context.Layout.Size.Height + .001) return;
        _context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, height), _context.Layout.PaddingEdges),
            _context.Theme, _context.ThemeMode, _context.Frame, _context.Font);
        _builder = new VisualSceneBuilder(_context.Layout.Size, _context.Font);
        _plot = VisualFrameLayout.Build(_builder, _context, entries);
        _builder.AddDiagnostic(new VisualDiagnostic("topology.natural-size", "The convenience export canvas includes the complete resolved topology and shared frame at natural size."));
    }

    private void ResolveFit() {
        var padding = _chart.Viewport.Padding;
        var width = Math.Max(1, _chart.Viewport.Width - padding * 2);
        var height = Math.Max(1, _chart.Viewport.Height - padding * 2);
        _offsetX = _plot.X - padding; _offsetY = _plot.Y - padding;
        if (!_resolvedLayout && width <= _plot.Width + .001 && height <= _plot.Height + .001) return;
        if (!_resolvedLayout && !_options.FitContentToViewport)
            throw new NotSupportedException($"Prepared topology exceeds its fixed viewport: requires {width:0.##} x {height:0.##}, available {_plot.Width:0.##} x {_plot.Height:0.##}. Enlarge the common size or enable FitContentToViewport.");
        _scale = Math.Min(_plot.Width / width, _plot.Height / height);
        _offsetX = _plot.X + (_plot.Width - width * _scale) / 2 - padding * _scale;
        _offsetY = _plot.Y + (_plot.Height - height * _scale) / 2 - padding * _scale;
        _builder.AddDiagnostic(new VisualDiagnostic("topology.content-fitted", "The complete topology, including text and native semantic geometry, was uniformly fitted into the common content viewport."));
    }
}
