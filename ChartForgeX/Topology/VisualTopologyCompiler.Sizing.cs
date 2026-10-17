using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Rendering;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

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
        var canFitManualCards = _chart.Nodes.Where(node => EffectiveNodeDisplayMode(node, _options) != TopologyNodeDisplayMode.Hidden)
            .All(node => node.Kind == TopologyNodeKind.Generic && !node.Shape.HasValue
            && node.DisplayMode is not (TopologyNodeDisplayMode.Tile or TopologyNodeDisplayMode.CompactCard or TopologyNodeDisplayMode.Dot)
            && string.IsNullOrWhiteSpace(node.IconId) && string.IsNullOrWhiteSpace(node.Symbol));
        var fitResolvedLayout = _options.FitContentToViewport
            && (_chart.LayoutMode != TopologyLayoutMode.Manual || canFitManualCards);
        var content = fitResolvedLayout ? MeasureFittedContent(padding, padding, width, height) : new ChartRect(padding, padding, width, height);
        if (!fitResolvedLayout
            && content.Width <= _plot.Width + .001 && content.Height <= _plot.Height + .001) return;
        if (!_resolvedLayout && !_options.FitContentToViewport)
            throw new NotSupportedException($"Prepared topology exceeds its fixed viewport: requires {content.Width:0.##} x {content.Height:0.##}, available {_plot.Width:0.##} x {_plot.Height:0.##}. Enlarge the common size or enable FitContentToViewport.");
        _scale = Math.Min(_plot.Width / content.Width, _plot.Height / content.Height);
        _offsetX = _plot.X + (_plot.Width - content.Width * _scale) / 2 - content.X * _scale;
        _offsetY = _plot.Y + (_plot.Height - content.Height * _scale) / 2 - content.Y * _scale;
        _builder.AddDiagnostic(new VisualDiagnostic("topology.content-fitted", "The complete topology, including text and native semantic geometry, was uniformly fitted into the common content viewport."));

        ChartRect MeasureFittedContent(double fallbackX, double fallbackY, double fallbackWidth, double fallbackHeight) {
            var left = double.PositiveInfinity; var top = double.PositiveInfinity;
            var right = double.NegativeInfinity; var bottom = double.NegativeInfinity;
            void Include(double x1, double y1, double x2, double y2) {
                left = Math.Min(left, x1); top = Math.Min(top, y1);
                right = Math.Max(right, x2); bottom = Math.Max(bottom, y2);
            }
            if (_options.IncludeGroups)
                foreach (var group in _chart.Groups) {
                    var stroke = _options.SelectedGroupIds.Contains(group.Id) ? 2.4 : _context.Theme.AxisStrokeWidth;
                    IncludeStroked(group.X, group.Y, group.X + group.Width, group.Y + group.Height, stroke);
                }
            foreach (var node in _chart.Nodes) {
                var mode = EffectiveNodeDisplayMode(node, _options);
                if (mode == TopologyNodeDisplayMode.Hidden) continue;
                var stroke = _options.SelectedNodeIds.Contains(node.Id) ? 2.8 : _context.Theme.AxisStrokeWidth;
                IncludeStroked(node.X, node.Y, node.X + node.Width, node.Y + node.Height, stroke);
                if (node.Shape.HasValue) {
                    foreach (var part in TopologyNodeShapeGeometry.SceneParts(node)) {
                        var shape = part.Path.Bounds();
                        IncludeStroked(shape.Left, shape.Top, shape.Right, shape.Bottom, stroke);
                    }
                }
                if (!string.IsNullOrWhiteSpace(node.Badge)) {
                    var badge = NodeBadgeBounds(node, mode, _options);
                    Include(badge.X, badge.Y, badge.Right, badge.Bottom);
                }
                if (!_options.IncludeNodeLabels) continue;
                if (mode == TopologyNodeDisplayMode.Tile) {
                    var caption = TopologyNodeFootprint.RenderedCaption(node, _options, _chart.TextMeasurement);
                    var center = CenterX(node);
                    Include(center - caption.Width / 2, node.Y + node.Height + 4,
                        center + caption.Width / 2, node.Y + node.Height + 4 + caption.Height);
                } else if (mode == TopologyNodeDisplayMode.Icon && _options.IncludeIconLabels) {
                    var labelWidth = IconLabelPlateWidth(node, _chart.TextMeasurement, _options.ResolvedIconLabelFontSize);
                    var labelY = IconLabelPlateY(node);
                    var labelHeight = IconLabelPlateHeight(node, _chart.TextMeasurement, _options.ResolvedIconLabelFontSize);
                    var center = CenterX(node);
                    IncludeStroked(center - labelWidth / 2, labelY, center + labelWidth / 2, labelY + labelHeight, _context.Theme.AxisStrokeWidth);
                } else if (node.Shape == TopologyNodeShape.Actor) {
                    var lines = DiagramNodeLabelLines(node, _options);
                    if (lines.Count > 0) {
                        var size = FitCaptionFontSize(lines, node.Width * .65, _context.Theme.Typography.DataLabelSize, 600);
                        var lineHeight = Math.Max(14, _builder.MeasureText("Ag", size * _scale, 600).LineHeight / _scale);
                        var centerY = DiagramNodeLabelCenterY(node);
                        Include(node.X + node.Width * .175, centerY - lineHeight * lines.Count / 2,
                            node.X + node.Width * .825, centerY + lineHeight * lines.Count / 2);
                    }
                } else if (mode == TopologyNodeDisplayMode.Artwork && !CanRenderNodeArtwork(node)) {
                    var textBounds = new ChartRect(node.X - 17, node.Y + node.Height + 5, node.Width + 34,
                        (_options.MaxNodeLabelLines + 1) * 18);
                    Include(textBounds.X, textBounds.Y, textBounds.Right, textBounds.Bottom);
                }
            }
            foreach (var route in _routes) {
                if (route.Value.Count == 0) continue;
                var halfStroke = FittedRouteStrokeWidth(route.Key) / 2;
                foreach (var point in route.Value) Include(point.X - halfStroke, point.Y - halfStroke, point.X + halfStroke, point.Y + halfStroke);
                var markerExtent = 12 + _context.Theme.AxisStrokeWidth / 2;
                if (RenderedSourceMarker(route.Key, _options.IncludeDirectionMarkers) != TopologyMarkerKind.None)
                    IncludeMarker(route.Value[0], markerExtent);
                if (_trunks.TryGetValue(route.Key, out var trunk)) {
                    if (ReferenceEquals(trunk.Owner, route.Key) && trunk.Tail.Count > 0
                        && RenderedTargetMarker(route.Key, _options.IncludeDirectionMarkers) != TopologyMarkerKind.None)
                        IncludeMarker(trunk.Tail[trunk.Tail.Count - 1], markerExtent);
                } else if (RenderedTargetMarker(route.Key, _options.IncludeDirectionMarkers) != TopologyMarkerKind.None) {
                    IncludeMarker(route.Value[route.Value.Count - 1], markerExtent);
                }
            }
            foreach (var label in _edgeLabels) Include(label.CenterX - label.Width / 2, label.CenterY - label.Height / 2,
                label.CenterX + label.Width / 2, label.CenterY + label.Height / 2);
            if (_options.IncludeEndpointLabels) {
                foreach (var edge in _chart.Edges) {
                    if (!_routes.TryGetValue(edge, out var route) || route.Count < 2) continue;
                    IncludeEndpoint(edge.SourceLabel, route[0], route[1]);
                    IncludeEndpoint(edge.TargetLabel, route[route.Count - 1], route[route.Count - 2]);
                }
            }
            return double.IsPositiveInfinity(left) ? new ChartRect(fallbackX, fallbackY, fallbackWidth, fallbackHeight)
                : new ChartRect(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top));

            void IncludeEndpoint(string? text, ChartPoint endpoint, ChartPoint adjacent) {
                if (string.IsNullOrWhiteSpace(text)) return;
                var label = EdgeEndpointLabelLayout(_chart, _options, endpoint, adjacent, text!);
                Include(label.Bounds.Left, label.Bounds.Top, label.Bounds.Right, label.Bounds.Bottom);
            }

            void IncludeStroked(double x1, double y1, double x2, double y2, double strokeWidth) {
                var halfStroke = Math.Max(0, strokeWidth) / 2;
                Include(x1 - halfStroke, y1 - halfStroke, x2 + halfStroke, y2 + halfStroke);
            }

            void IncludeMarker(ChartPoint point, double extent) =>
                Include(point.X - extent, point.Y - extent, point.X + extent, point.Y + extent);

            double FittedRouteStrokeWidth(TopologyEdge edge) {
                var selected = _options.SelectedEdgeIds.Contains(edge.Id);
                var width = edge.StrokeWidth ?? _context.Theme.SeriesStrokeWidth;
                if (edge.Emphasis == TopologyEdgeEmphasis.Strong) width *= 1.6;
                if (edge.Emphasis == TopologyEdgeEmphasis.Subtle && !_options.UseForceGraphPresentation && !IsMonitoringDashboardStyle(_options)) width *= .7;
                if (_options.UseForceGraphPresentation || IsMonitoringDashboardStyle(_options)) width = EdgeStrokeWidth(edge, selected, _options);
                else if (selected) width *= 1.5;
                var layers = ChartLineVisualLayers.Build(_colors.Foreground, width, EdgeVisualStyle(edge, selected, _options));
                var maximum = layers.Where(layer => layer.IsVisible).Select(layer => layer.StrokeWidth).DefaultIfEmpty(width).Max();
                if (ShouldRenderMonitoringRouteHalo(_chart, edge, _nodesById, _options)) {
                    var geographic = ShouldRenderGeographicRouteHalo(_chart, edge, _nodesById, _options);
                    maximum = Math.Max(maximum, width + RouteHaloStrokeExtra(geographic));
                }
                return maximum;
            }
        }
    }
}
