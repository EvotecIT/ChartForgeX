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
        var source = _routes[edge.Id];
        if (source.Count < 2) throw new InvalidOperationException("A topology route requires both endpoints: " + edge.Id);
        var points = source.Select(Point).ToArray();
        var colorRole = !string.IsNullOrWhiteSpace(edge.Color) ? SvgColorRole.Any : edge.IsMuted ? SvgColorRole.Surface : SvgColorRole.Status;
        var color = Highlight(Color(edge.Color, edge.IsMuted ? _colors.Border : Status(edge.Status)), _highlight.IsEdgeHighlighted(edge));
        if (_options.UseForceGraphPresentation || edge.Opacity.HasValue) color = color.WithOpacity(color.A / 255d * EdgeOpacity(edge, _options));
        var width = (edge.StrokeWidth ?? _context.Theme.SeriesStrokeWidth) * _scale;
        if (edge.Emphasis == TopologyEdgeEmphasis.Strong) width *= 1.6;
        if (edge.Emphasis == TopologyEdgeEmphasis.Subtle && !_options.UseForceGraphPresentation) { width *= .7; color = color.WithOpacity(color.A / 255d * .5); }
        if (_options.SelectedEdgeIds.Contains(edge.Id)) width *= 1.5;
        if (_options.UseForceGraphPresentation) width = EdgeStrokeWidth(edge, _options.SelectedEdgeIds.Contains(edge.Id), _options) * _scale;
        var paintPoints = _paintRoutes[edge.Id].Select(Point).ToArray();
        var hasTrunk = _trunks.TryGetValue(edge.Id, out var trunk);
        using (Host(edge.Href, edge.Tooltip, edge.Id, "topology-edge-host"))
        using (_builder.PushGroup(edge.Id, "topology-edge", EdgeMetadata(edge))) {
            if (hasTrunk) {
                // Keep every relationship reachable along its complete route while the shared tail
                // moves between visible members in the HTML host. Transparent ink adds no pixels.
                var hitPath = new ChartPath(points.Select((point, index) => index == 0 ? ChartPathCommand.MoveTo(point.X, point.Y) : ChartPathCommand.LineTo(point.X, point.Y)).ToArray());
                _builder.Path(hitPath, stroke: new ChartColor(0, 0, 0, 0), strokeWidth: Math.Max(width, 10 * _scale), role: "topology-shared-trunk-hit");
            }
            var dash = EffectiveEdgePngDashArray(edge)?.Select(length => length * _scale).ToArray();
            // Preserve caller-authored line effects through the shared layer owner. Theme defaults
            // remain flat; the legacy preset may still opt into its canonical line treatment.
            var style = _options.EdgeVisualStyle ?? ChartForgeX.Core.ChartLineVisualStyle.Plain();
            using (PinnedState()) {
                DrawLine(paintPoints);
                Marker(RenderedSourceMarker(edge, _options.IncludeDirectionMarkers), points[1], points[0], color, colorRole);
                if (!hasTrunk) Marker(RenderedTargetMarker(edge, _options.IncludeDirectionMarkers), points[points.Length - 2], points[points.Length - 1], color, colorRole);
                else if (trunk.Owner == edge.Id) {
                    using (_builder.PushGroup(edge.Id + "-trunk-tail", "topology-shared-trunk-tail", new Dictionary<string, string> { ["data-trunk-owner-id"] = trunk.Owner })) {
                        var tail = trunk.Tail.Select(Point).ToArray(); DrawLine(tail);
                        Marker(RenderedTargetMarker(edge, _options.IncludeDirectionMarkers), tail[tail.Length - 2], tail[tail.Length - 1], color, colorRole);
                    }
                }
            }
            if (_options.IncludeEndpointLabels) {
                Endpoint(edge.SourceLabel, source[0], source[1]);
                Endpoint(edge.TargetLabel, source[source.Count - 1], source[source.Count - 2]);
            }

            void DrawLine(ChartPoint[] route) {
                var path = new ChartPath(route.Select((point, index) => index == 0 ? ChartPathCommand.MoveTo(point.X, point.Y) : ChartPathCommand.LineTo(point.X, point.Y)).ToArray());
                foreach (var layer in ChartLineVisualLayers.Build(color, width / _scale, style)) {
                    if (layer.IsVisible) _builder.Path(path, stroke: layer.ColorWithOpacity(), strokeWidth: layer.StrokeWidth * _scale,
                        role: "topology-edge-line" + layer.RoleSuffix, dash: dash,
                        paint: new VisualScenePaintBinding(stroke: layer.IsHighlight ? SvgPaint.Literal(layer.ColorWithOpacity()) : SvgPaint.Of(layer.ColorWithOpacity(), colorRole)));
                }
            }
        }
        var margin = Math.Max(8 * _scale, width / 2);
        var bounds = new ChartRect(points.Min(p => p.X) - margin, points.Min(p => p.Y) - margin,
            points.Max(p => p.X) - points.Min(p => p.X) + margin * 2, points.Max(p => p.Y) - points.Min(p => p.Y) + margin * 2);
        _builder.AddRegion(new VisualSemanticRegion(edge.Id, "topology-edge", bounds, edge.Label));

        void Endpoint(string? text, ChartPoint endpoint, ChartPoint adjacent) {
            if (string.IsNullOrWhiteSpace(text)) return;
            var p = Point(EdgeEndpointLabelPoint(_chart, _options, endpoint, adjacent, text!));
            var size = _context.Theme.Typography.DataLabelSize * .8;
            var metrics = _builder.MeasureText(text!, size * _scale, 600);
            var b = new ChartRect(p.X - metrics.Width / 2 - 3 * _scale, p.Y - metrics.Height / 2 - 3 * _scale, metrics.Width + 6 * _scale, metrics.Height + 6 * _scale);
            if (_options.IncludeEdgeLabelBackplates) _builder.Rect(b, _colors.Surface, role: "topology-endpoint-label-surface", paint: Paint(_colors.Surface, SvgColorRole.Surface));
            Text(text!, b, size, _colors.Foreground, 600, "topology-endpoint-label", centered: true);
        }
    }

    private void BuildEdgeLabels() {
        if (!_options.IncludeEdgeLabels) return;
        foreach (var layout in _edgeLabels) {
            var bounds = Bounds(layout.CenterX - layout.Width / 2, layout.CenterY - layout.Height / 2, layout.Width, layout.Height);
            var edge = layout.Edge;
            var metadata = new Dictionary<string, string>(EdgeMetadata(edge));
            metadata["class"] = CssPrefix + "__edge-label";
            metadata["data-label-width"] = Number(bounds.Width); metadata["data-label-height"] = Number(bounds.Height);
            metadata["data-label-x"] = Number(bounds.X); metadata["data-label-y"] = Number(bounds.Y);
            var anchor = Point(new ChartPoint(layout.AnchorX, layout.AnchorY));
            metadata["data-label-anchor-x"] = Number(anchor.X); metadata["data-label-anchor-y"] = Number(anchor.Y);
            metadata["data-label-anchor-node-id"] = edge.LabelAnchorNodeId ?? string.Empty;
            metadata["data-label-anchor-override"] = edge.HasLabelAnchorOverride ? "true" : "false";
            metadata["data-label-leader"] = ShouldDrawEdgeLabelLeader(layout, _options) ? "true" : "false";
            using var labelGroup = _builder.PushGroup(edge.Id + "-label", "topology-edge-label", metadata);
            var active = _highlight.IsEdgeHighlighted(edge);
            if (_options.IncludeEdgeLabelLeaders && ShouldDrawEdgeLabelLeader(layout, _options)) {
                var from = Point(new ChartPoint(layout.AnchorX, layout.AnchorY)); var to = Point(EdgeLabelLeaderEnd(layout));
                _builder.Line(from.X, from.Y, to.X, to.Y, Highlight(_colors.Border, active), _context.Theme.AxisStrokeWidth * _scale, "topology-edge-label-leader", paint: Paint(stroke: Highlight(_colors.Border, active), strokeRole: SvgColorRole.Surface));
            }
            if (_options.IncludeEdgeLabelBackplates) _builder.Rect(bounds, Highlight(_colors.Surface, active), radius: _context.Theme.BarRadius * _scale, role: "topology-edge-label-surface", paint: Paint(Highlight(_colors.Surface, active), SvgColorRole.Surface));
            var labels = new[] { layout.Label, layout.SecondaryLabel, layout.TertiaryLabel }.Where(label => !string.IsNullOrWhiteSpace(label)).ToArray();
            var height = bounds.Height / Math.Max(1, labels.Length);
            ChartRect? measured = null;
            for (var i = 0; i < labels.Length; i++) {
                var textBounds = Text(labels[i], new ChartRect(bounds.X + 4 * _scale, bounds.Y + i * height, Math.Max(0, bounds.Width - 8 * _scale), height),
                    _context.Theme.Typography.DataLabelSize * (i == 0 ? 1 : .85), Highlight(i == 0 ? _colors.Foreground : _colors.MutedForeground, active), i == 0 ? 600 : 400, "topology-edge-label-text", centered: true);
                if (!textBounds.HasValue) continue;
                if (!measured.HasValue) measured = textBounds;
                else {
                    var left = Math.Min(measured.Value.X, textBounds.Value.X); var top = Math.Min(measured.Value.Y, textBounds.Value.Y);
                    measured = new ChartRect(left, top, Math.Max(measured.Value.Right, textBounds.Value.Right) - left, Math.Max(measured.Value.Bottom, textBounds.Value.Bottom) - top);
                }
            }
            if (measured.HasValue) _resolvedLabelBounds.Add(edge.Id, measured.Value);
            _builder.AddRegion(new VisualSemanticRegion(edge.Id + "-label", "topology-edge-label", bounds, string.Join("\n", labels)));
        }
    }

    private void Marker(TopologyMarkerKind kind, ChartPoint from, ChartPoint to, ChartColor color, SvgColorRole colorRole) {
        if (kind == TopologyMarkerKind.None) return;
        var angle = Math.Atan2(to.Y - from.Y, to.X - from.X);
        ChartPoint P(double x, double y) => new(to.X + (Math.Cos(angle) * x - Math.Sin(angle) * y) * _scale, to.Y + (Math.Sin(angle) * x + Math.Cos(angle) * y) * _scale);
        void Stroke(params double[] xy) {
            var commands = new List<ChartPathCommand>();
            for (var i = 0; i < xy.Length; i += 2) { var p = P(xy[i], xy[i + 1]); commands.Add(i == 0 ? ChartPathCommand.MoveTo(p.X, p.Y) : ChartPathCommand.LineTo(p.X, p.Y)); }
            _builder.Path(new ChartPath(commands), stroke: color, strokeWidth: _context.Theme.AxisStrokeWidth * _scale, role: "topology-marker", paint: Paint(stroke: color, strokeRole: colorRole));
        }
        void Polygon(bool fill, params double[] xy) {
            var commands = new List<ChartPathCommand>();
            for (var i = 0; i < xy.Length; i += 2) { var p = P(xy[i], xy[i + 1]); commands.Add(i == 0 ? ChartPathCommand.MoveTo(p.X, p.Y) : ChartPathCommand.LineTo(p.X, p.Y)); }
            _builder.Path(new ChartPath(commands), fill ? color : _colors.Surface, color, _context.Theme.AxisStrokeWidth * _scale, "topology-marker", close: true, paint: Paint(fill ? color : _colors.Surface, fill ? colorRole : SvgColorRole.Surface, color, colorRole));
        }
        void Circle(double x, bool fill) { var p = P(x, 0); _builder.Ellipse(p.X, p.Y, 3 * _scale, 3 * _scale, fill ? color : _colors.Surface, color, _context.Theme.AxisStrokeWidth * _scale, "topology-marker", paint: Paint(fill ? color : _colors.Surface, fill ? colorRole : SvgColorRole.Surface, color, colorRole)); }
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
