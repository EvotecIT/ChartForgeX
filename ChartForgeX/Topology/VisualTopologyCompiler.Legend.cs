using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    // The common frame measures and clips this rectangle. No delegate survives preparation.
    private void BuildLegendMarker(TopologyLegendItem item, ChartRect bounds) {
        var metadata = new System.Collections.Generic.Dictionary<string, string> {
            ["data-legend-kind"] = item.Kind.ToString().ToLowerInvariant(), ["data-cfx-status"] = item.Status?.ToString() ?? string.Empty
        };
        if (item.IconId != null) {
            metadata["data-legend-icon-id"] = item.IconId;
            var resolved = TopologyRenderPrimitives.ResolveNodeIcon(new TopologyNode { IconId = item.IconId }, _options);
            if (resolved != null) metadata["data-legend-icon-shape"] = resolved.Shape.ToString();
        }
        using var markerGroup = _builder.PushGroup(null, "topology-legend-item", metadata);
        var fallback = item.Status.HasValue ? Status(item.Status.Value) : _colors.Accent;
        var color = Color(item.Color, fallback);
        var colorRole = !string.IsNullOrWhiteSpace(item.Color) ? SvgColorRole.Any : item.Status.HasValue ? SvgColorRole.Status : SvgColorRole.Series;
        var sourceColor = !string.IsNullOrWhiteSpace(item.Color) ? item.Color : item.Status.HasValue ? _svgTheme?.StatusColor(item.Status.Value) : _svgTheme?.Accent;
        var colorPaint = SourcePaint(sourceColor, color, colorRole, fallback: fallback);
        var centerX = bounds.X + bounds.Width / 2; var centerY = bounds.Y + bounds.Height / 2;
        if (item.Kind == TopologyLegendItemKind.Node) {
            var node = new TopologyNode { Kind = item.NodeKind ?? TopologyNodeKind.Generic, Symbol = item.Symbol, IconId = item.IconId, Color = item.Color, Status = item.Status ?? TopologyHealthStatus.Unknown };
            if (_options.RequireResolvedIcons && node.IconId != null && TopologyRenderPrimitives.ResolveNodeIcon(node, _options) == null)
                throw new System.InvalidOperationException("Unresolved topology legend icon: " + node.IconId);
            _builder.Rect(bounds, Color(item.BackgroundColor, _colors.Surface), color, _context.Theme.AxisStrokeWidth,
                _context.Theme.BarRadius, "topology-legend-node", paint: new VisualScenePaintBinding(SourcePaint(string.IsNullOrWhiteSpace(item.BackgroundColor) ? _svgTheme?.Card : item.BackgroundColor, Color(item.BackgroundColor, _colors.Surface), string.IsNullOrWhiteSpace(item.BackgroundColor) ? SvgColorRole.Surface : SvgColorRole.Any, fallback: _colors.Surface), colorPaint));
            BuildGlyph(node, centerX, centerY, color, System.Math.Min(bounds.Width, bounds.Height) / 26, colorRole, artworkOpacity: 1, sourceFallback: fallback);
        } else {
            using (PinnedState()) {
                if (item.Kind == TopologyLegendItemKind.Edge) {
                    var edge = new TopologyEdge { Kind = item.EdgeKind ?? TopologyEdgeKind.Generic, LineStyle = TopologyRenderPrimitives.LegendLineStyle(_source, item) };
                    _builder.Line(bounds.X, centerY, bounds.Right, centerY, color, _context.Theme.SeriesStrokeWidth,
                        "topology-legend-edge", dash: TopologyRenderPrimitives.EffectiveEdgePngDashArray(edge), paint: new VisualScenePaintBinding(stroke: colorPaint));
                } else _builder.Ellipse(centerX, centerY, bounds.Width / 2, bounds.Height / 2, color, role: "topology-legend-status", paint: new VisualScenePaintBinding(colorPaint));
            }
        }
    }
}
