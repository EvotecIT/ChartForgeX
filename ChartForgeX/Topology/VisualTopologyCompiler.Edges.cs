using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private void BuildEdge(TopologyEdge edge) {
        var source = _routes[edge];
        var units = TopologyPreparedEdgePlan.Units(edge);
        if (source.Count < 2) throw new InvalidOperationException("A topology route requires both endpoints: " + edge.Id);
        var points = source.Select(Point).ToArray();
        var ordinaryFullPath = _numericRoutes.TryGetValue(edge, out var numeric) && edge.Interpolation.HasValue ? numeric.FullPath : edge.PathGeometry?.Capture();
        var startAdjacent = ordinaryFullPath == null ? points[1] : Point(ordinaryFullPath.AdjacentToEndpoint(true));
        var endAdjacent = ordinaryFullPath == null ? points[points.Length - 2] : Point(ordinaryFullPath.AdjacentToEndpoint(false));
        var colorRole = edge.IsMuted ? SvgColorRole.Surface : !string.IsNullOrWhiteSpace(edge.Color) ? SvgColorRole.Any : SvgColorRole.Status;
        var authoredColor = EdgeColor(edge, _svgTheme ?? Theme(), _options);
        var usePalette = !edge.IsMuted && string.IsNullOrWhiteSpace(edge.Color) && edge.PaletteIndex.HasValue;
        var cssVariable = SvgPaint.TryCssVariable(authoredColor, edge.IsMuted ? _colors.Border : Status(edge.Status), out var resolvedColor, out var sourcePaint);
        if (!cssVariable) resolvedColor = Color(authoredColor, edge.IsMuted ? _colors.Border : Status(edge.Status));
        if (usePalette) resolvedColor = PaletteColor(edge.PaletteIndex!.Value);
        var paintOpacity = _highlight.IsEdgeHighlighted(edge) || !_highlight.IsActive ? 1 : _highlight.DimmedOpacity;
        var color = Highlight(resolvedColor, _highlight.IsEdgeHighlighted(edge));
        if (_options.UseForceGraphPresentation || IsMonitoringDashboardStyle(_options) || edge.Opacity.HasValue) {
            var opacity = EdgeOpacity(edge, _options);
            color = color.WithOpacity(color.A / 255d * opacity); paintOpacity *= opacity;
        }
        var width = (edge.StrokeWidth ?? _context.Theme.SeriesStrokeWidth * units) * (_options.UseForceGraphPresentation ? 1 : _scale);
        if (edge.Emphasis == TopologyEdgeEmphasis.Strong) width *= 1.6;
        if (edge.Emphasis == TopologyEdgeEmphasis.Subtle && !_options.UseForceGraphPresentation && !IsMonitoringDashboardStyle(_options)) { width *= .7; color = color.WithOpacity(color.A / 255d * .5); paintOpacity *= .5; }
        var edgePaint = usePalette ? PalettePaint(edge.PaletteIndex!.Value, color, paintOpacity)
            : cssVariable ? paintOpacity >= 1 ? sourcePaint : sourcePaint.WithOpacity(color, paintOpacity)
            : SvgPaint.Of(color, colorRole);
        if (_options.SelectedEdgeIds.Contains(edge.Id)) width *= 1.5;
        if (_options.UseForceGraphPresentation || IsMonitoringDashboardStyle(_options)) width = EdgeStrokeWidth(edge, _options.SelectedEdgeIds.Contains(edge.Id), _options) * (_options.UseForceGraphPresentation ? 1 : _scale);
        var paintPoints = _paintRoutes[edge].Select(Point).ToArray();
        var hasTrunk = _trunks.TryGetValue(edge, out var trunk);
        using (Host(edge.Href, edge.Tooltip, edge.Id, "topology-edge-host"))
        using (_builder.PushGroup(edge.Id, "topology-edge", EdgeMetadata(edge))) {
            if (hasTrunk || HasCrossingPaintPath(edge, false)) {
                // Keep every relationship reachable along its complete route while the shared tail
                // moves between visible members in the HTML host. Transparent ink adds no pixels.
                var hitPath = ordinaryFullPath == null ? new ChartPath(points.Select((point, index) => index == 0 ? ChartPathCommand.MoveTo(point.X, point.Y) : ChartPathCommand.LineTo(point.X, point.Y)).ToArray()) : Transform(ordinaryFullPath);
                _builder.Path(hitPath, stroke: new ChartColor(0, 0, 0, 0), strokeWidth: Math.Max(width, 10 * units * _scale), role: hasTrunk ? "topology-shared-trunk-hit" : "topology-crossing-hit");
            }
            var dash = EffectiveEdgePngDashArray(edge)?.Select(length => length * _scale * (edge.DashPattern.Count == 0 ? units : 1)).ToArray();
            var style = EdgeVisualStyle(edge, _options.SelectedEdgeIds.Contains(edge.Id), _options);
            using (PinnedState()) {
                DrawLine(paintPoints);
                Marker(RenderedSourceMarker(edge, _options.IncludeDirectionMarkers), startAdjacent, points[0], color, colorRole, edgePaint, edge.SourceMarkerStyle, units);
                if (!hasTrunk) Marker(RenderedTargetMarker(edge, _options.IncludeDirectionMarkers), endAdjacent, points[points.Length - 1], color, colorRole, edgePaint, edge.TargetMarkerStyle, units);
                else if (ReferenceEquals(trunk.Owner, edge)) {
                    using (_builder.PushGroup(edge.Id + "-trunk-tail", "topology-shared-trunk-tail", new Dictionary<string, string> { ["data-trunk-owner-id"] = trunk.Owner.Id })) {
                        var tail = trunk.Tail.Select(Point).ToArray(); DrawLine(tail, trunkTail: true);
                        Marker(RenderedTargetMarker(edge, _options.IncludeDirectionMarkers), numeric?.TailPath == null ? tail[tail.Length - 2] : Point(numeric.TailPath.AdjacentToEndpoint(false)), tail[tail.Length - 1], color, colorRole, edgePaint, edge.TargetMarkerStyle, units);
                    }
                }
            }
            if (_options.IncludeEndpointLabels) {
                Endpoint(edge.SourceLabel, source[0], ordinaryFullPath == null ? source[1] : ordinaryFullPath.AdjacentToEndpoint(true), "source");
                Endpoint(edge.TargetLabel, source[source.Count - 1], ordinaryFullPath == null ? source[source.Count - 2] : ordinaryFullPath.AdjacentToEndpoint(false), "target");
            }

            void DrawLine(ChartPoint[] route, bool trunkTail = false) {
                var logical = OrdinaryLogicalPath(edge, trunkTail);
                var ordinary = logical == null ? new ChartPath(route.Select((point, index) => index == 0 ? ChartPathCommand.MoveTo(point.X, point.Y) : ChartPathCommand.LineTo(point.X, point.Y)).ToArray()) : Transform(logical);
                var path = CrossingPaintPath(edge, trunkTail, ordinary);
                if (ShouldRenderMonitoringRouteHalo(_chart, edge, _nodesById, _options)) {
                    var geographic = ShouldRenderGeographicRouteHalo(_chart, edge, _nodesById, _options);
                    var halo = Highlight(ChartColorMath.WithOpacity(_colors.Background, RouteHaloOpacity(geographic)), _highlight.IsEdgeHighlighted(edge));
                    _builder.Path(path, stroke: halo, strokeWidth: width + RouteHaloStrokeExtra(geographic) * units * _scale,
                        role: "topology-edge-route-halo", dash: dash, paint: new VisualScenePaintBinding(stroke: BackgroundPaint(halo, RouteHaloOpacity(geographic) * HighlightFactor(_highlight.IsEdgeHighlighted(edge)))));
                }
                foreach (var layer in ChartLineVisualLayers.Build(color, width, style, units * _scale)) {
                    if (layer.IsVisible) _builder.Path(path, stroke: layer.ColorWithOpacity(), strokeWidth: layer.StrokeWidth,
                        role: "topology-edge-line" + layer.RoleSuffix, dash: dash,
                        paint: new VisualScenePaintBinding(stroke: layer.IsHighlight ? SvgPaint.Literal(layer.ColorWithOpacity())
                            : layer.Opacity >= 1 ? edgePaint : edgePaint.WithOpacity(layer.ColorWithOpacity(), layer.Opacity)));
                }
                if (dash is { Length: > 0 }) {
                    var layers = ChartLineVisualLayers.Build(color, width, style, units * _scale);
                    DrawEndpointConnector(route[0], edge.Interpolation.HasValue && logical != null ? Point(logical.AdjacentToEndpoint(true)) : route[1]);
                    DrawEndpointConnector(route[route.Length - 1], edge.Interpolation.HasValue && logical != null ? Point(logical.AdjacentToEndpoint(false)) : route[route.Length - 2]);

                    void DrawEndpointConnector(ChartPoint endpoint, ChartPoint adjacent) {
                        var dx = adjacent.X - endpoint.X;
                        var dy = adjacent.Y - endpoint.Y;
                        var distance = Math.Sqrt(dx * dx + dy * dy);
                        if (distance <= .001) return;
                        var length = Math.Min(EdgeEndpointGap * units * _scale, distance);
                        var inner = new ChartPoint(endpoint.X + dx / distance * length, endpoint.Y + dy / distance * length);
                        var connector = new ChartPath(new[] { ChartPathCommand.MoveTo(endpoint.X, endpoint.Y), ChartPathCommand.LineTo(inner.X, inner.Y) });
                        foreach (var layer in layers) {
                            if (!layer.IsVisible) continue;
                            _builder.Path(connector, stroke: layer.ColorWithOpacity(), strokeWidth: layer.StrokeWidth,
                                role: "topology-edge-endpoint-connector",
                                paint: new VisualScenePaintBinding(stroke: layer.IsHighlight ? SvgPaint.Literal(layer.ColorWithOpacity())
                                    : layer.Opacity >= 1 ? edgePaint : edgePaint.WithOpacity(layer.ColorWithOpacity(), layer.Opacity)));
                        }
                    }
                }
            }
        }
        var margin = Math.Max(8 * units * _scale, width / 2);
        var bounds = new ChartRect(points.Min(p => p.X) - margin, points.Min(p => p.Y) - margin,
            points.Max(p => p.X) - points.Min(p => p.X) + margin * 2, points.Max(p => p.Y) - points.Min(p => p.Y) + margin * 2);
        if (ordinaryFullPath != null) { var ink = Transform(ordinaryFullPath).Bounds(); bounds = new ChartRect(ink.X - margin, ink.Y - margin, ink.Width + margin * 2, ink.Height + margin * 2); }
        if (StyledMarkerBounds(_chart, edge, _options, _nodesById) is ChartRect markerBounds) {
            var b = Bounds(markerBounds.X, markerBounds.Y, markerBounds.Width, markerBounds.Height);
            var left = Math.Min(bounds.Left, b.Left); var top = Math.Min(bounds.Top, b.Top);
            bounds = new ChartRect(left, top, Math.Max(bounds.Right, b.Right) - left, Math.Max(bounds.Bottom, b.Bottom) - top);
        }
        var label = edge.Label;
        if (string.IsNullOrWhiteSpace(label)) label = edge.Direction switch {
            VisualLinkDirection.Backward => edge.TargetNodeId + " to " + edge.SourceNodeId,
            VisualLinkDirection.Bidirectional => edge.SourceNodeId + " to " + edge.TargetNodeId + " and " + edge.TargetNodeId + " to " + edge.SourceNodeId,
            VisualLinkDirection.None => edge.SourceNodeId + " and " + edge.TargetNodeId,
            _ => edge.SourceNodeId + " to " + edge.TargetNodeId
        };
        _builder.AddRegion(new VisualSemanticRegion(edge.Id, "topology-edge", bounds, label));

        void Endpoint(string? text, ChartPoint endpoint, ChartPoint adjacent, string end) {
            if (string.IsNullOrWhiteSpace(text)) return;
            var layout = ResolvedEndpointLabel(edge, endpoint, adjacent, text!, end == "source");
            var p = Point(new ChartPoint(layout.Bounds.X + layout.Bounds.Width / 2, layout.Bounds.Y + layout.Bounds.Height / 2));
            using var endpointGroup = _builder.PushGroup(null, "topology-edge-endpoint-label", new Dictionary<string, string> {
                ["data-edge-id"] = edge.Id, ["data-endpoint"] = end,
                ["data-label-x"] = Number(p.X), ["data-label-y"] = Number(p.Y)
            });
            var size = layout.FontSize;
            var b = Bounds(layout.Bounds.X, layout.Bounds.Y, layout.Bounds.Width, layout.Bounds.Height);
            var fill = EdgeLabelColor(Color(edge.LabelBackgroundColor, _colors.Surface), edge);
            if (_options.IncludeEdgeLabelBackplates) _builder.Rect(b, fill, role: "topology-endpoint-label-surface", paint: new VisualScenePaintBinding(EdgeLabelSurfacePaint(edge, fill, EdgeLabelOpacity(edge), true)));
            var ink = EdgeLabelColor(Color(edge.LabelColor, _colors.Foreground), edge);
            Text(text!, b, size, ink, 600, "topology-endpoint-label", centered: true, opacity: EdgeLabelOpacity(edge), paint: EdgeTextPaint(edge, ink), defaultUnits: units);
        }
    }

    private void ResolveExplicitLabelBaselines() {
        _edgeLabels = _edgeLabels.Select(layout => {
            var position = layout.Edge.LabelPosition;
            if (position == null || position.VerticalAnchor != TopologyLabelVerticalAnchor.TextBaseline || TopologyRenderRouteCache.Geometry(_chart, layout.Edge)?.MainLabel != null) return layout;
            var count = EdgeLabelRows(layout.Label, layout.SecondaryLabel, layout.TertiaryLabel, _options);
            var center = ResolveExplicitLabelCenter(_chart, _options, layout.Edge, position, layout.Label,
                layout.SecondaryLabel, layout.TertiaryLabel, layout.Width, layout.Height, count, _scale);
            return new TopologyEdgeLabelLayout(layout.Edge, layout.Label, layout.SecondaryLabel, layout.TertiaryLabel,
                center.X, center.Y, layout.Width, layout.Height, layout.AnchorX, layout.AnchorY);
        }).ToArray();
    }

    private void BuildEdgeLabels() {
        if (!_options.IncludeEdgeLabels) return;
        foreach (var layout in _edgeLabels.OrderBy(layout => _edgeRenderOrders[layout.Edge])) {
            using var textPolicy = UseTextPolicy(layout.Edge.TextStyle);
            if (layout.Edge.TextStyle?.Color is ChartColor authoredTextColor) layout.Edge.LabelColor = authoredTextColor.ToHexRgba();
            var bounds = Bounds(layout.CenterX - layout.Width / 2, layout.CenterY - layout.Height / 2, layout.Width, layout.Height);
            var edge = layout.Edge;
            var units = TopologyPreparedEdgePlan.Units(edge);
            // Relationship diagnostics belong to the route once. The label keeps its own
            // measured geometry and stable relationship key for host selection/filtering.
            var metadata = new Dictionary<string, string> {
                ["data-edge-id"] = edge.Id, ["data-cfx-status"] = edge.Status.ToString(),
                ["data-edge-render-order"] = _edgeRenderOrders[edge].ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["class"] = CssPrefix + "__edge-label" + _highlight.CssClass(CssPrefix, _highlight.IsEdgeHighlighted(edge))
            };
            metadata["data-label-width"] = Number(bounds.Width); metadata["data-label-height"] = Number(bounds.Height);
            metadata["data-label-x"] = Number(bounds.X + bounds.Width / 2); metadata["data-label-y"] = Number(bounds.Y + bounds.Height / 2);
            var anchor = Point(new ChartPoint(layout.AnchorX, layout.AnchorY));
            metadata["data-label-anchor-x"] = Number(anchor.X); metadata["data-label-anchor-y"] = Number(anchor.Y);
            metadata["data-label-anchor-node-id"] = edge.LabelAnchorNodeId ?? string.Empty;
            metadata["data-label-anchor-override"] = edge.HasLabelAnchorOverride ? "true" : "false";
            metadata["data-label-leader"] = ShouldDrawEdgeLabelLeader(layout, _options) ? "true" : "false";
            metadata["data-label-clearance"] = !_options.IncludeEdgeLabelBackplates && ShouldDrawEdgeLabelClearance(layout, _options) ? "true" : "false";
            metadata["data-edge-label-render-order"] = _edgeRenderOrders[edge].ToString(System.Globalization.CultureInfo.InvariantCulture);
            var lineCount = EdgeLabelRows(layout.Label, layout.SecondaryLabel, layout.TertiaryLabel, _options);
            metadata["data-label-line-count"] = lineCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
            using var labelGroup = _builder.PushGroup(edge.Id + "-label", "topology-edge-label", metadata);
            ChartColor LabelColor(ChartColor color) => EdgeLabelColor(color, edge);
            if (_options.IncludeEdgeLabelLeaders && ShouldDrawEdgeLabelLeader(layout, _options)) {
                var from = Point(new ChartPoint(layout.AnchorX, layout.AnchorY)); var to = Point(EdgeLabelLeaderEnd(layout));
                var color = LabelColor(_colors.Border);
                _builder.Line(from.X, from.Y, to.X, to.Y, color, _context.Theme.AxisStrokeWidth * units * _scale, "topology-edge-label-leader", paint: new VisualScenePaintBinding(stroke: BorderPaint(color, EdgeLabelOpacity(edge))));
            }
            if (_options.IncludeEdgeLabelBackplates) {
                var fill = LabelColor(ChartColorMath.WithOpacity(Color(edge.LabelBackgroundColor ?? EdgeLabelBackplateFill(Theme(), _options), _colors.Surface), EdgeLabelBackplateFillOpacity(_options)));
                var stroke = LabelColor(ChartColorMath.WithOpacity(_colors.Border, EdgeLabelBackplateStrokeOpacity(_options)));
                _builder.Rect(bounds, fill, stroke, EdgeLabelBackplateStrokeWidth * units * _scale,
                    EdgeLabelBackplateRadius(_options) * units * _scale, "topology-edge-label-surface", paint: new VisualScenePaintBinding(
                        EdgeLabelSurfacePaint(edge, fill, EdgeLabelBackplateFillOpacity(_options) * EdgeLabelOpacity(edge), IsMonitoringDashboardStyle(_options)),
                        BorderPaint(stroke, EdgeLabelBackplateStrokeOpacity(_options) * EdgeLabelOpacity(edge))));
            }
            else if (ShouldDrawEdgeLabelClearance(layout, _options)) {
                var containingGroup = EdgeLabelClearanceGroup(_chart, layout);
                var hasGroupSurface = containingGroup != null && _options.IncludeGroups;
                var groupAccent = containingGroup == null ? _colors.Accent : Color(containingGroup.Color ?? ResolveGroupIcon(containingGroup, _options)?.Color, Status(containingGroup.Status));
                var tintAmount = IsMonitoringDashboardStyle(_options) ? .055 : .10;
                var explicitGroupFill = hasGroupSurface && !string.IsNullOrWhiteSpace(containingGroup!.BackgroundColor);
                var baseFill = !hasGroupSurface ? _colors.Background : explicitGroupFill ? Color(containingGroup!.BackgroundColor, _colors.Surface)
                    : UseNeutralGroupSurface(_options) ? _colors.Surface : ChartColorMath.BlendPremultiplied(_colors.Background, groupAccent, tintAmount);
                var fill = ChartColorMath.WithOpacity(baseFill, EdgeLabelClearanceOpacity(containingGroup));
                var fillPaint = !hasGroupSurface ? BackgroundPaint(baseFill)
                    : explicitGroupFill ? GroupFillPaint(containingGroup!, baseFill, true, true)
                    : UseNeutralGroupSurface(_options) ? CardPaint(baseFill)
                    : SvgPaint.Mix(baseFill, BackgroundPaint(_colors.Background), GroupAccentPaint(containingGroup!, groupAccent, true), tintAmount);
                using var clearance = _builder.PushGroup(null, null, new Dictionary<string, string> {
                    ["data-clearance-surface"] = containingGroup == null ? "background" : "group",
                    ["data-clearance-group-id"] = containingGroup?.Id ?? string.Empty
                });
                _builder.Rect(Bounds(EdgeLabelClearanceX(layout, layout.CenterX), EdgeLabelClearanceY(layout, layout.CenterY),
                    EdgeLabelClearanceWidth(layout), EdgeLabelClearanceHeight(layout)), LabelColor(fill),
                    radius: EdgeLabelClearanceRadius * units * _scale, role: "topology-edge-label-clearance", paint: new VisualScenePaintBinding(fillPaint.WithOpacity(LabelColor(fill), EdgeLabelClearanceOpacity(containingGroup) * EdgeLabelOpacity(edge))));
            }
            var labels = new[] { layout.Label, layout.SecondaryLabel, layout.TertiaryLabel }.Where(label => !string.IsNullOrWhiteSpace(label)).ToArray();
            var height = bounds.Height / Math.Max(1, lineCount);
            var rowOffset = 0;
            ChartRect? measured = null;
            for (var i = 0; i < labels.Length; i++) {
                var rows = EdgeLabelRows(labels[i], _options);
                var textBounds = Text(i == 0 ? EdgeCaptionContent(edge, labels[i]) : ChartForgeX.Typography.TextContent.Plain(labels[i]), new ChartRect(bounds.X + 4 * units * _scale, bounds.Y + rowOffset * height, Math.Max(0, bounds.Width - 8 * units * _scale), rows * height),
                    TextPolicySize(_context.Theme.Typography.DataLabelSize) * (edge.TextStyle?.FontSize.HasValue == true ? 1 : units) * (i == 0 ? 1 : .85), LabelColor(Color(edge.LabelColor, i == 0 ? _colors.Foreground : _colors.MutedForeground)), i == 0 ? 600 : 400, "topology-edge-label-text", rows, centered: true, verticallyCentered: true,
                    paint: EdgeTextPaint(edge, LabelColor(Color(edge.LabelColor, i == 0 ? _colors.Foreground : _colors.MutedForeground)), i > 0), opacity: EdgeLabelOpacity(edge), defaultUnits: units);
                rowOffset += rows;
                if (!textBounds.HasValue) continue;
                if (!measured.HasValue) measured = textBounds;
                else {
                    var left = Math.Min(measured.Value.X, textBounds.Value.X); var top = Math.Min(measured.Value.Y, textBounds.Value.Y);
                    measured = new ChartRect(left, top, Math.Max(measured.Value.Right, textBounds.Value.Right) - left, Math.Max(measured.Value.Bottom, textBounds.Value.Bottom) - top);
                }
            }
            if (measured.HasValue) _resolvedLabelBounds.Add(edge, measured.Value);
            _builder.AddRegion(new VisualSemanticRegion(edge.Id + "-label", "topology-edge-label", bounds, string.Join("\n", labels)));
        }
    }

    private double EdgeLabelOpacity(TopologyEdge edge) => (edge.Opacity ?? 1) * (_highlight.IsActive && !_highlight.IsEdgeHighlighted(edge) ? _highlight.DimmedOpacity : 1);
    private ChartColor EdgeLabelColor(ChartColor color, TopologyEdge edge) => ChartColorMath.WithOpacity(color, EdgeLabelOpacity(edge));

    private void Marker(TopologyMarkerKind kind, ChartPoint from, ChartPoint to, ChartColor color, SvgColorRole colorRole, SvgPaint? sourcePaint = null, TopologyMarkerStyle? layout = null, double defaultUnits = 1) {
        if (kind == TopologyMarkerKind.None) return;
        var angle = Math.Atan2(to.Y - from.Y, to.X - from.X);
        ChartPoint P(double x, double y) => layout == null
            ? new ChartPoint(to.X + (Math.Cos(angle) * x - Math.Sin(angle) * y) * defaultUnits * _scale, to.Y + (Math.Sin(angle) * x + Math.Cos(angle) * y) * defaultUnits * _scale)
            : new ChartPoint(to.X + (Math.Cos(angle) * (x * layout.Scale + layout.Offset) - Math.Sin(angle) * y * layout.Scale) * _scale,
                to.Y + (Math.Sin(angle) * (x * layout.Scale + layout.Offset) + Math.Cos(angle) * y * layout.Scale) * _scale);
        var strokeWidth = (layout?.StrokeWidth ?? _context.Theme.AxisStrokeWidth * defaultUnits) * _scale;
        void Stroke(params double[] xy) {
            var commands = new List<ChartPathCommand>();
            for (var i = 0; i < xy.Length; i += 2) { var p = P(xy[i], xy[i + 1]); commands.Add(i == 0 ? ChartPathCommand.MoveTo(p.X, p.Y) : ChartPathCommand.LineTo(p.X, p.Y)); }
            _builder.Path(new ChartPath(commands), stroke: color, strokeWidth: strokeWidth, role: "topology-marker", paint: new VisualScenePaintBinding(stroke: sourcePaint ?? SvgPaint.Of(color, colorRole)));
        }
        void Polygon(bool fill, params double[] xy) {
            var commands = new List<ChartPathCommand>();
            for (var i = 0; i < xy.Length; i += 2) { var p = P(xy[i], xy[i + 1]); commands.Add(i == 0 ? ChartPathCommand.MoveTo(p.X, p.Y) : ChartPathCommand.LineTo(p.X, p.Y)); }
            _builder.Path(new ChartPath(commands), fill ? color : _colors.Surface, color, strokeWidth, "topology-marker", close: true,
                paint: new VisualScenePaintBinding(fill ? sourcePaint ?? SvgPaint.Of(color, colorRole) : CardPaint(_colors.Surface), sourcePaint ?? SvgPaint.Of(color, colorRole)));
        }
        void Circle(double x, bool fill) { var p = P(x, 0); _builder.Ellipse(p.X, p.Y, 3 * _scale * (layout?.Scale ?? defaultUnits), 3 * _scale * (layout?.Scale ?? defaultUnits), fill ? color : _colors.Surface, color, strokeWidth, "topology-marker",
            paint: new VisualScenePaintBinding(fill ? sourcePaint ?? SvgPaint.Of(color, colorRole) : CardPaint(_colors.Surface), sourcePaint ?? SvgPaint.Of(color, colorRole))); }
        switch (kind) {
            case TopologyMarkerKind.Arrow:
                switch (_options.ArrowMarkerStyle) {
                    case TopologyArrowMarkerStyle.Circle: Circle(-3, true); break;
                    case TopologyArrowMarkerStyle.Diamond: Polygon(true, 0, 0, -5, -4, -10, 0, -5, 4); break;
                    case TopologyArrowMarkerStyle.Chevron: Stroke(-7, -4, 0, 0, -7, 4); break;
                    default: Polygon(true, 0, 0, -8, -4, -8, 4); break;
                }
                break;
            case TopologyMarkerKind.Circle: Circle(-3, true); break;
            case TopologyMarkerKind.Cross: Stroke(-6, -3, 0, 3); Stroke(-6, 3, 0, -3); break;
            case TopologyMarkerKind.Diamond: Polygon(true, 0, 0, -5, -4, -10, 0, -5, 4); break;
            case TopologyMarkerKind.OpenDiamond: Polygon(false, 0, 0, -5, -4, -10, 0, -5, 4); break;
            case TopologyMarkerKind.OpenTriangle: Polygon(false, 0, 0, -9, -5, -9, 5); break;
            case TopologyMarkerKind.ExactlyOne: Stroke(-3, -5, -3, 5); Stroke(-7, -5, -7, 5); break;
            case TopologyMarkerKind.ZeroOrOne: Stroke(-3, -5, -3, 5); Circle(-10, false); break;
            case TopologyMarkerKind.OneOrMany: Stroke(0, -5, -8, 0, 0, 5); Stroke(-11, -5, -11, 5); break;
            case TopologyMarkerKind.ZeroOrMany: Stroke(0, -5, -8, 0, 0, 5); Circle(-12, false); break;
        }
    }
}
