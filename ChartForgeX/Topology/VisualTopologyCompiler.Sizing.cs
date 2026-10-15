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
        var previousWidth = _context.Layout.Size.Width;
        var previousHeight = _context.Layout.Size.Height;
        var previousPlotHeight = _plot.Height;
        // Growing the width can make a previously omitted heading fit, reserving more height.
        // Remeasure the same resolved content through the shared frame until that reservation
        // stabilizes; do not lay out or route the diagram a second time.
        for (var pass = 0; pass < 8; pass++) {
            var missingWidth = Math.Max(0, requiredWidth - _plot.Width);
            var missingHeight = Math.Max(0, requiredHeight - _plot.Height);
            if (missingWidth <= .001 && missingHeight <= .001) break;
            var width = _context.Layout.Size.Width + missingWidth;
            var lowerHeight = _context.Layout.Size.Height;
            var extraHeight = missingHeight;
            // Oversized headings can reserve a share of each additional canvas pixel.
            // Once width is stable, use the measured plot response to close that deficit.
            if (pass > 0 && missingWidth <= .001 && _context.Layout.Size.Width == previousWidth && lowerHeight > previousHeight) {
                var response = (_plot.Height - previousPlotHeight) / (lowerHeight - previousHeight);
                if (response > 0 && response < 1 && !double.IsInfinity(missingHeight / response)) extraHeight = missingHeight / response;
            }
            previousWidth = _context.Layout.Size.Width;
            previousHeight = lowerHeight;
            previousPlotHeight = _plot.Height;
            Resize(width, lowerHeight + extraHeight);
            grew = true;
            if (extraHeight <= missingHeight || _plot.Width + .001 < requiredWidth || _plot.Height + .001 < requiredHeight) continue;
            // A heading may wrap or stop shrinking across the extrapolated interval.
            // Keep a measured fitting upper bound and remove any excess natural space.
            var upperHeight = _context.Layout.Size.Height;
            for (var refinement = 0; refinement < 32 && upperHeight - lowerHeight > .001; refinement++) {
                var candidate = lowerHeight + (upperHeight - lowerHeight) / 2;
                Resize(width, candidate);
                if (_plot.Width + .001 >= requiredWidth && _plot.Height + .001 >= requiredHeight) upperHeight = candidate;
                else lowerHeight = candidate;
            }
            Resize(width, upperHeight);
            break;
        }
        if (!grew) return;
        _builder.AddDiagnostic(new VisualDiagnostic("topology.natural-size", "The convenience export canvas includes the complete resolved topology and shared frame at natural size."));

        void Resize(double width, double height) {
            _context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, height), _context.Layout.PaddingEdges),
                _context.Theme, _context.ThemeMode, _context.Frame, _context.Font);
            _builder = new VisualSceneBuilder(_context.Layout.Size, _context.Font);
            _plot = VisualFrameLayout.Build(_builder, _context, entries, FramePaints());
        }
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
