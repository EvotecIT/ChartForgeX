using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private void BuildEdge(TopologyEdge edge) {
        var source = _routes[edge.Id];
        if (source.Count < 2) throw new InvalidOperationException("A topology route requires both endpoints: " + edge.Id);
        var points = source.Select(Point).ToArray();
        var color = Highlight(Color(edge.Color, edge.IsMuted ? _colors.Border : Status(edge.Status)), _highlight.IsEdgeHighlighted(edge));
        if (edge.Opacity.HasValue) color = color.WithOpacity(color.A / 255d * edge.Opacity.Value);
        var width = (edge.StrokeWidth ?? _context.Theme.SeriesStrokeWidth) * _scale;
        if (edge.Emphasis == TopologyEdgeEmphasis.Strong) width *= 1.6;
        if (edge.Emphasis == TopologyEdgeEmphasis.Subtle) { width *= .7; color = color.WithOpacity(color.A / 255d * .5); }
        if (_options.SelectedEdgeIds.Contains(edge.Id)) width *= 1.5;
        var commands = points.Select((point, index) => index == 0 ? ChartPathCommand.MoveTo(point.X, point.Y) : ChartPathCommand.LineTo(point.X, point.Y)).ToArray();
        using (Host(edge.Href, edge.Tooltip, edge.Id, "topology-edge-host"))
        using (_builder.PushGroup(edge.Id, "topology-edge", new Dictionary<string, string> { ["data-source"] = edge.SourceNodeId, ["data-target"] = edge.TargetNodeId, ["data-direction"] = edge.Direction.ToString(), ["data-kind"] = edge.Kind.ToString() })) {
            var path = new ChartPath(commands);
            var dash = EffectiveEdgePngDashArray(edge)?.Select(length => length * _scale).ToArray();
            // Preserve caller-authored line effects through the shared layer owner. Theme defaults
            // remain flat; the legacy preset may still opt into its canonical line treatment.
            var style = _options.EdgeVisualStyle ?? ChartForgeX.Core.ChartLineVisualStyle.Plain();
            using (PinnedState()) {
                foreach (var layer in ChartLineVisualLayers.Build(color, width / _scale, style)) {
                    if (layer.IsVisible) _builder.Path(path, stroke: layer.ColorWithOpacity(), strokeWidth: layer.StrokeWidth * _scale,
                        role: "topology-edge-line" + layer.RoleSuffix, dash: dash);
                }
                Marker(RenderedSourceMarker(edge, _options.IncludeDirectionMarkers), points[1], points[0], color);
                Marker(RenderedTargetMarker(edge, _options.IncludeDirectionMarkers), points[points.Length - 2], points[points.Length - 1], color);
            }
            if (_options.IncludeEndpointLabels) {
                Endpoint(edge.SourceLabel, source[0], source[1]);
                Endpoint(edge.TargetLabel, source[source.Count - 1], source[source.Count - 2]);
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
            if (_options.IncludeEdgeLabelBackplates) _builder.Rect(b, _colors.Surface, role: "topology-endpoint-label-surface");
            Text(text!, b, size, _colors.Foreground, 600, "topology-endpoint-label", centered: true);
        }
    }

    private void BuildEdgeLabels() {
        if (!_options.IncludeEdgeLabels) return;
        foreach (var layout in _edgeLabels) {
            var bounds = Bounds(layout.CenterX - layout.Width / 2, layout.CenterY - layout.Height / 2, layout.Width, layout.Height);
            var edge = layout.Edge;
            var active = _highlight.IsEdgeHighlighted(edge);
            if (_options.IncludeEdgeLabelLeaders && ShouldDrawEdgeLabelLeader(layout, _options)) {
                var from = Point(new ChartPoint(layout.AnchorX, layout.AnchorY)); var to = Point(EdgeLabelLeaderEnd(layout));
                _builder.Line(from.X, from.Y, to.X, to.Y, Highlight(_colors.Border, active), _context.Theme.AxisStrokeWidth * _scale, "topology-edge-label-leader");
            }
            if (_options.IncludeEdgeLabelBackplates) _builder.Rect(bounds, Highlight(_colors.Surface, active), radius: _context.Theme.BarRadius * _scale, role: "topology-edge-label-surface");
            var labels = new[] { layout.Label, layout.SecondaryLabel, layout.TertiaryLabel }.Where(label => !string.IsNullOrWhiteSpace(label)).ToArray();
            var height = bounds.Height / Math.Max(1, labels.Length);
            ChartRect? measured = null;
            for (var i = 0; i < labels.Length; i++) {
                var textBounds = Text(labels[i], new ChartRect(bounds.X + 4 * _scale, bounds.Y + i * height, Math.Max(0, bounds.Width - 8 * _scale), height),
                    _context.Theme.Typography.DataLabelSize * (i == 0 ? 1 : .85), Highlight(i == 0 ? _colors.Foreground : _colors.MutedForeground, active), i == 0 ? 600 : 400, "topology-edge-label", centered: true);
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

    private void Marker(TopologyMarkerKind kind, ChartPoint from, ChartPoint to, ChartColor color) {
        if (kind == TopologyMarkerKind.None) return;
        var angle = Math.Atan2(to.Y - from.Y, to.X - from.X);
        ChartPoint P(double x, double y) => new(to.X + (Math.Cos(angle) * x - Math.Sin(angle) * y) * _scale, to.Y + (Math.Sin(angle) * x + Math.Cos(angle) * y) * _scale);
        void Stroke(params double[] xy) {
            var commands = new List<ChartPathCommand>();
            for (var i = 0; i < xy.Length; i += 2) { var p = P(xy[i], xy[i + 1]); commands.Add(i == 0 ? ChartPathCommand.MoveTo(p.X, p.Y) : ChartPathCommand.LineTo(p.X, p.Y)); }
            _builder.Path(new ChartPath(commands), stroke: color, strokeWidth: _context.Theme.AxisStrokeWidth * _scale, role: "topology-marker");
        }
        void Polygon(bool fill, params double[] xy) {
            var commands = new List<ChartPathCommand>();
            for (var i = 0; i < xy.Length; i += 2) { var p = P(xy[i], xy[i + 1]); commands.Add(i == 0 ? ChartPathCommand.MoveTo(p.X, p.Y) : ChartPathCommand.LineTo(p.X, p.Y)); }
            _builder.Path(new ChartPath(commands), fill ? color : _colors.Surface, color, _context.Theme.AxisStrokeWidth * _scale, "topology-marker", close: true);
        }
        void Circle(double x, bool fill) { var p = P(x, 0); _builder.Ellipse(p.X, p.Y, 3 * _scale, 3 * _scale, fill ? color : _colors.Surface, color, _context.Theme.AxisStrokeWidth * _scale, "topology-marker"); }
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
