using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Rendering;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private ChartRect? _preparedSceneContent;

    // Prepared envelopes carry output-space bounds. Explicit output fitting and a new
    // automatic layout deliberately use their existing coordinate-resolution policies.
    private bool PreservePreparedSceneCoordinates => _source.UsesPreparedSceneCoordinates
        && _source.LayoutMode == TopologyLayoutMode.Manual && !_resolvedLayout && !_options.FitContentToViewport;

    private void ReserveNaturalFrame(IReadOnlyList<VisualLegendEntry> entries) {
        if (!_naturalSize || _options.FitContentToViewport || _resolvedLayout) return;
        // Measure through the common frame with enough room for its bounded headings. A tiny
        // natural canvas must reach canonical layout before its content can expand that canvas.
        var typography = _context.Theme.Typography;
        var probeHeight = _context.Layout.Size.Height + 4 * (typography.TitleSize + typography.SubtitleSize + _context.Theme.Spacing);
        var probeContext = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(_context.Layout.Size.Width, probeHeight), _context.Layout.PaddingEdges),
            _context.Theme, _context.ThemeMode, _context.Frame, _context.Font).WithNativeTextCapture(_context.NativeTextCapture);
        var probe = VisualFrameLayout.Build(new VisualSceneBuilder(probeContext.Layout.Size, probeContext.Font, probeContext.NativeTextCapture), probeContext, entries, FramePaints());
        var height = Math.Max(_context.Layout.Size.Height, probeHeight - probe.Height + 1);
        _context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(_context.Layout.Size.Width, height), _context.Layout.PaddingEdges),
            _context.Theme, _context.ThemeMode, _context.Frame, _context.Font).WithNativeTextCapture(_context.NativeTextCapture);
        _builder = new VisualSceneBuilder(_context.Layout.Size, _context.Font, _context.NativeTextCapture);
        _plot = VisualFrameLayout.Build(_builder, _context, entries, FramePaints());
    }

    private void ResolveNaturalSize(IReadOnlyList<VisualLegendEntry> entries) {
        if (!_naturalSize || _options.FitContentToViewport || _resolvedLayout) return;
        if (PreservePreparedSceneCoordinates) {
            ResolvePreparedSceneSize(entries);
            return;
        }
        var padding = _chart.Viewport.Padding;
        var viewportWidth = Math.Max(1, _chart.Viewport.Width - padding * 2);
        var content = IncludeAnnotationBounds(MeasureRenderedContent(padding, padding,
            viewportWidth, Math.Max(1, _chart.Viewport.Height - padding * 2), includeViewport: true));
        // Route ink may use horizontal outer padding without changing the resolved graph
        // domain. Independent annotation panels are content and retain the frame padding.
        var requiredWidth = (_annotations.Count > 0 || _pathDecorations.Count > 0) ? content.Width : viewportWidth
            + Math.Max(0, padding - content.X - _plot.X)
            + Math.Max(0, content.Right - padding - viewportWidth - (_context.Layout.Size.Width - _plot.Right));
        var requiredHeight = content.Height;
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
            _context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(Math.Ceiling(width), Math.Ceiling(height)), _context.Layout.PaddingEdges),
                _context.Theme, _context.ThemeMode, _context.Frame, _context.Font).WithNativeTextCapture(_context.NativeTextCapture);
            _builder = new VisualSceneBuilder(_context.Layout.Size, _context.Font, _context.NativeTextCapture);
            _plot = VisualFrameLayout.Build(_builder, _context, entries, FramePaints());
        }
    }

    private void ResolveFit() {
        if (PreservePreparedSceneCoordinates) {
            var sceneContent = PreparedSceneContent();
            var size = _context.Layout.Size;
            // A fixed caller context must accommodate scene coordinates as supplied. Natural
            // convenience export can extend the right/bottom canvas, never translate the origin.
            if (sceneContent.X < -.001 || sceneContent.Y < -.001 || sceneContent.Right > size.Width + .001 || sceneContent.Bottom > size.Height + .001)
                throw new NotSupportedException("Prepared topology scene bounds exceed the fixed canvas. Enlarge the common size or enable FitContentToViewport.");
            _contentClip = TopologyPreparedFramePadding.ResolveContentClip(_source, _options, _context, _plot) ?? _plot;
            _offsetX = 0; _offsetY = 0;
            return;
        }
        var padding = _chart.Viewport.Padding;
        var width = Math.Max(1, _chart.Viewport.Width - padding * 2);
        var height = Math.Max(1, _chart.Viewport.Height - padding * 2);
        _offsetX = _plot.X - padding; _offsetY = _plot.Y - padding;
        var canFitManualCards = _chart.Nodes.Where(node => EffectiveNodeDisplayMode(node, _options) != TopologyNodeDisplayMode.Hidden)
            .All(node => node.Kind == TopologyNodeKind.Generic && !node.Shape.HasValue
            && node.DisplayMode is not (TopologyNodeDisplayMode.Tile or TopologyNodeDisplayMode.CompactCard or TopologyNodeDisplayMode.Dot)
            && !HasExplicitIconShape(node.IconShape) && string.IsNullOrWhiteSpace(node.IconId) && string.IsNullOrWhiteSpace(node.Symbol));
        var fitResolvedLayout = _options.FitContentToViewport
            && (_chart.UsesPreparedSceneCoordinates || _chart.LayoutMode != TopologyLayoutMode.Manual || canFitManualCards || _pathDecorations.Count > 0);
        var naturalContent = _naturalSize && !_resolvedLayout && !_options.FitContentToViewport;
        var content = IncludeAnnotationBounds(fitResolvedLayout || naturalContent
            ? MeasureRenderedContent(padding, padding, width, height, includeViewport: naturalContent) : new ChartRect(padding, padding, width, height));
        _contentClip = _plot;
        if (naturalContent) {
            _offsetX = (_annotations.Count > 0 || _pathDecorations.Count > 0) ? _plot.X - content.X
                : _plot.X - padding + Math.Max(0, padding - content.X - _plot.X);
            _offsetY = _plot.Y - content.Y;
            var left = Math.Min(_plot.X, _offsetX + content.X);
            var right = Math.Max(_plot.Right, _offsetX + content.Right);
            _contentClip = new ChartRect(left, _plot.Y, right - left, _plot.Height);
            if (left >= 0 && right <= _context.Layout.Size.Width + .001 && content.Height <= _plot.Height + .001) return;
        }
        _offsetX = _plot.X - content.X; _offsetY = _plot.Y - content.Y;
        if (!fitResolvedLayout
            && content.Width <= _plot.Width + .001 && content.Height <= _plot.Height + .001) return;
        if (!_resolvedLayout && !_options.FitContentToViewport)
            throw new NotSupportedException($"Prepared topology exceeds its fixed viewport: requires {content.Width:0.##} x {content.Height:0.##}, available {_plot.Width:0.##} x {_plot.Height:0.##}. Enlarge the common size or enable FitContentToViewport.");
        _scale = Math.Min(_plot.Width / content.Width, _plot.Height / content.Height);
        _offsetX = _plot.X + (_plot.Width - content.Width * _scale) / 2 - content.X * _scale;
        _offsetY = _plot.Y + (_plot.Height - content.Height * _scale) / 2 - content.Y * _scale;
        _builder.AddDiagnostic(new VisualDiagnostic("topology.content-fitted", "The complete topology, including text and native semantic geometry, was uniformly fitted into the common content viewport."));

    }

    private ChartRect PreparedSceneContent() {
        if (!_preparedSceneContent.HasValue) _preparedSceneContent = IncludeAnnotationBounds(MeasureRenderedContent(
            0, 0, _chart.Viewport.Width, _chart.Viewport.Height));
        return _preparedSceneContent.Value;
    }

    private void ResolvePreparedSceneSize(IReadOnlyList<VisualLegendEntry> entries) {
        var content = PreparedSceneContent();
        var width = Math.Max(_context.Layout.Size.Width, content.Right);
        var height = Math.Max(_context.Layout.Size.Height, content.Bottom);
        if (width <= _context.Layout.Size.Width + .001 && height <= _context.Layout.Size.Height + .001) return;
        _context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(Math.Ceiling(width), Math.Ceiling(height)), _context.Layout.PaddingEdges),
            _context.Theme, _context.ThemeMode, _context.Frame, _context.Font).WithNativeTextCapture(_context.NativeTextCapture);
        _builder = new VisualSceneBuilder(_context.Layout.Size, _context.Font, _context.NativeTextCapture);
        _plot = VisualFrameLayout.Build(_builder, _context, entries, FramePaints());
        _builder.AddDiagnostic(new VisualDiagnostic("topology.natural-size", "The convenience export canvas includes the complete restored scene at its retained coordinate origin."));
    }

    private ChartRect MeasureRenderedContent(double fallbackX, double fallbackY, double fallbackWidth, double fallbackHeight, bool includeViewport = false) {
        var left = includeViewport ? fallbackX : double.PositiveInfinity;
        var top = includeViewport ? fallbackY : double.PositiveInfinity;
        var right = includeViewport ? fallbackX + fallbackWidth : double.NegativeInfinity;
        var bottom = includeViewport ? fallbackY + fallbackHeight : double.NegativeInfinity;
        void Include(double x1, double y1, double x2, double y2) {
            left = Math.Min(left, x1); top = Math.Min(top, y1);
            right = Math.Max(right, x2); bottom = Math.Max(bottom, y2);
        }
        // Native normalization already reserves node/group footprints in its viewport.
        // Natural exports keep that authored domain and add materialized route extents;
        // explicit fitting measures every primitive independently of the original canvas.
        if (!includeViewport) {
            if (_options.IncludeGroups)
                foreach (var group in _chart.Groups) {
                    var stroke = _options.SelectedGroupIds.Contains(group.Id) ? 2.4 : _context.Theme.AxisStrokeWidth;
                    IncludeStroked(group.X, group.Y, group.X + group.Width, group.Y + group.Height, stroke);
                }
            foreach (var node in _chart.Nodes) {
                var mode = EffectiveNodeDisplayMode(node, _options);
                if (mode == TopologyNodeDisplayMode.Hidden) continue;
                var stroke = NodeSurfaceStrokeWidth(node);
                IncludeStroked(node.X, node.Y, node.X + node.Width, node.Y + node.Height, stroke);
                if (node.Shape.HasValue) {
                    foreach (var part in TopologyNodeShapeGeometry.SceneParts(node)) {
                        var shape = part.Path.Bounds();
                        IncludeStroked(shape.Left, shape.Top, shape.Right, shape.Bottom, NodeSurfaceStrokeWidth(node, part.ContourPaint));
                    }
                }
                if (!UseEnclosedShapeContent(node, mode, _options) && !string.IsNullOrWhiteSpace(node.Badge)) {
                    var badge = NodeBadgeBounds(node, mode, _options);
                    Include(badge.X, badge.Y, badge.Right, badge.Bottom);
                }
                if (!_options.IncludeNodeLabels || !node.ShowLabel) continue;
                if (TopologyExternalCaption.HasPolicy(node)) {
                    var external = TopologyExternalCaption.Prepare(node, _options);
                    if (external != null) Include(external.Bounds.Left, external.Bounds.Top, external.Bounds.Right, external.Bounds.Bottom);
                } else if (mode == TopologyNodeDisplayMode.Tile) {
                    var caption = TopologyNodeFootprint.RenderedCaption(node, _options, _chart.TextMeasurement);
                    var center = CenterX(node);
                    var gap = 4 * TopologyPreparedNodeBasis.Units(node);
                    Include(center - caption.Width / 2, node.Y + node.Height + gap,
                        center + caption.Width / 2, node.Y + node.Height + gap + caption.Height);
                } else if (mode == TopologyNodeDisplayMode.Icon && _options.IncludeIconLabels) {
                    var labelWidth = IconLabelPlateWidth(node, _chart.TextMeasurement, _options.ResolvedIconLabelFontSize);
                    var labelY = IconLabelPlateY(node);
                    var labelHeight = IconLabelPlateHeight(node, _chart.TextMeasurement, _options.ResolvedIconLabelFontSize);
                    var center = CenterX(node);
                    IncludeStroked(center - labelWidth / 2, labelY, center + labelWidth / 2, labelY + labelHeight, _context.Theme.AxisStrokeWidth * TopologyPreparedNodeBasis.Units(node));
                } else if (TopologyActorCaption.Applies(node, _options)) {
                    var caption = TopologyActorCaption.Prepare(node, _options, _chart.TextMeasurement).Bounds;
                    Include(caption.Left, caption.Top, caption.Right, caption.Bottom);
                } else if (TopologyNodeShapeGeometry.IsActor(node)) {
                    if (node.LabelContent.IsStructured) {
                        var height = Math.Max(1, _options.MaxNodeLabelLines)
                            * TopologyTextLayout.StructuredCaptionRowHeight(node, _chart.TextMeasurement) + 1e-8;
                        var centerY = DiagramNodeLabelCenterY(node);
                        Include(node.X + node.Width * .175, centerY - height / 2,
                            node.X + node.Width * .825, centerY + height / 2);
                        continue;
                    }
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
        }
        foreach (var route in _routes) {
            if (route.Value.Count == 0) continue;
            var halfStroke = FittedRouteStrokeWidth(route.Key) / 2;
            if (_numericRoutes.TryGetValue(route.Key, out var numericRoute) && numericRoute.Interpolation.HasValue) {
                var ink = numericRoute.FullPath.Bounds(); Include(ink.Left - halfStroke, ink.Top - halfStroke, ink.Right + halfStroke, ink.Bottom + halfStroke);
            } else foreach (var point in route.Value) Include(point.X - halfStroke, point.Y - halfStroke, point.X + halfStroke, point.Y + halfStroke);
            var markerExtent = (15 + _context.Theme.AxisStrokeWidth / 2) * TopologyPreparedEdgePlan.Units(route.Key);
            if (RenderedSourceMarker(route.Key, _options.IncludeDirectionMarkers) != TopologyMarkerKind.None)
                IncludeMarker(route.Value[0], route.Key.SourceMarkerStyle == null ? markerExtent : MarkerExtent(route.Key.SourceMarkerStyle, _context.Theme.AxisStrokeWidth * TopologyPreparedEdgePlan.Units(route.Key)));
            if (_trunks.TryGetValue(route.Key, out var trunk)) {
                if (ReferenceEquals(trunk.Owner, route.Key) && trunk.Tail.Count > 0
                    && RenderedTargetMarker(route.Key, _options.IncludeDirectionMarkers) != TopologyMarkerKind.None)
                    IncludeMarker(trunk.Tail[trunk.Tail.Count - 1], route.Key.TargetMarkerStyle == null ? markerExtent : MarkerExtent(route.Key.TargetMarkerStyle, _context.Theme.AxisStrokeWidth * TopologyPreparedEdgePlan.Units(route.Key)));
            } else if (RenderedTargetMarker(route.Key, _options.IncludeDirectionMarkers) != TopologyMarkerKind.None) {
                IncludeMarker(route.Value[route.Value.Count - 1], route.Key.TargetMarkerStyle == null ? markerExtent : MarkerExtent(route.Key.TargetMarkerStyle, _context.Theme.AxisStrokeWidth * TopologyPreparedEdgePlan.Units(route.Key)));
            }
        }
        foreach (var label in _edgeLabels) {
            if (label.Edge.LabelPosition == null) {
                Include(label.CenterX - label.Width / 2, label.CenterY - label.Height / 2,
                    label.CenterX + label.Width / 2, label.CenterY + label.Height / 2);
                continue;
            }
            var b = TopologyTextLayout.PositionEnvelope(label.Edge.LabelPosition, label.CenterX, label.CenterY, label.Width, label.Height);
            Include(b.Left, b.Top, b.Right, b.Bottom);
        }
        if (_options.IncludeEndpointLabels) {
            foreach (var edge in _chart.Edges) {
                if (!_routes.TryGetValue(edge, out var route) || route.Count < 2) continue;
                var numeric = _numericRoutes.TryGetValue(edge, out var geometry) && geometry.Interpolation.HasValue ? geometry.FullPath : null;
                IncludeEndpoint(edge, edge.SourceLabel, route[0], numeric == null ? route[1] : numeric.AdjacentToEndpoint(true), true);
                IncludeEndpoint(edge, edge.TargetLabel, route[route.Count - 1], numeric == null ? route[route.Count - 2] : numeric.AdjacentToEndpoint(false), false);
            }
        }
        var measured = double.IsPositiveInfinity(left) ? new ChartRect(fallbackX, fallbackY, fallbackWidth, fallbackHeight)
            : new ChartRect(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top));
        return IncludeExternalGlyphBounds(IncludeSurfaceStrokeBounds(IncludeCrossingPaintBounds(measured)));

        void IncludeEndpoint(TopologyEdge edge, string? text, ChartPoint endpoint, ChartPoint adjacent, bool source) {
            if (string.IsNullOrWhiteSpace(text)) return;
            var label = ResolvedEndpointLabel(edge, endpoint, adjacent, text!, source);
            Include(label.Bounds.Left, label.Bounds.Top, label.Bounds.Right, label.Bounds.Bottom);
        }

        void IncludeStroked(double x1, double y1, double x2, double y2, double strokeWidth) {
            var halfStroke = Math.Max(0, strokeWidth) / 2;
            Include(x1 - halfStroke, y1 - halfStroke, x2 + halfStroke, y2 + halfStroke);
        }

        void IncludeMarker(ChartPoint point, double extent) =>
            Include(point.X - extent, point.Y - extent, point.X + extent, point.Y + extent);
    }

    private double FittedRouteStrokeWidth(TopologyEdge edge) {
        var selected = _options.SelectedEdgeIds.Contains(edge.Id);
        var units = TopologyPreparedEdgePlan.Units(edge);
        var width = edge.StrokeWidth ?? _context.Theme.SeriesStrokeWidth * units;
        if (edge.Emphasis == TopologyEdgeEmphasis.Strong) width *= 1.6;
        if (edge.Emphasis == TopologyEdgeEmphasis.Subtle && !_options.UseForceGraphPresentation && !IsMonitoringDashboardStyle(_options)) width *= .7;
        if (_options.UseForceGraphPresentation || IsMonitoringDashboardStyle(_options)) width = EdgeStrokeWidth(edge, selected, _options);
        else if (selected) width *= 1.5;
        var layers = ChartLineVisualLayers.Build(_colors.Foreground, width, EdgeVisualStyle(edge, selected, _options), units);
        var maximum = layers.Where(layer => layer.IsVisible).Select(layer => layer.StrokeWidth).DefaultIfEmpty(width).Max();
        if (ShouldRenderMonitoringRouteHalo(_chart, edge, _nodesById, _options)) {
            var geographic = ShouldRenderGeographicRouteHalo(_chart, edge, _nodesById, _options);
            maximum = Math.Max(maximum, width + RouteHaloStrokeExtra(geographic) * units);
        }
        return maximum;
    }
}
