using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private void BuildSurface() {
        if (_options.CanvasSurfaceStyle != TopologyCanvasSurfaceStyle.Plain) {
            _builder.Rect(_plot, _colors.Surface, _colors.Border, _context.Theme.AxisStrokeWidth, _context.Theme.BarRadius, "topology-panel");
            if (_options.CanvasSurfaceStyle == TopologyCanvasSurfaceStyle.PanelGrid) {
                var gap = _context.Theme.Spacing * 2;
                for (var x = _plot.X + gap; x < _plot.Right; x += gap) _builder.Line(x, _plot.Y, x, _plot.Bottom, _colors.Border.WithOpacity(.25), .5, "topology-grid");
                for (var y = _plot.Y + gap; y < _plot.Bottom; y += gap) _builder.Line(_plot.X, y, _plot.Right, y, _colors.Border.WithOpacity(.25), .5, "topology-grid");
            }
        }
        if (_chart.LayoutMode != TopologyLayoutMode.Geographic) return;
        var mapChart = TopologyLayoutEngine.Clone(_chart); mapChart.Legend = null;
        var map = TopologyMapProjection.MapRect(mapChart);
        var soft = UseSoftMapBackground(_options);
        if (!soft) {
            foreach (var land in TopologyMapProjection.LandDots(_chart.MapViewport)) {
                if (!TopologyMapProjection.IsVisible(_chart.MapViewport, land.X, land.Y)) continue;
                var projected = TopologyMapProjection.Project(map, _chart.MapViewport, land.X, land.Y);
                var p = Point(new ChartPoint(projected.X, projected.Y));
                var radius = TopologyMapProjection.LandDotRadius(map, _chart.MapViewport) * _scale;
                _builder.Ellipse(p.X, p.Y, radius, radius, _colors.Border.WithOpacity(.65), role: "topology-map-land");
            }
        }
        foreach (var boundary in TopologyMapProjection.BoundaryLines(_chart.MapViewport)) {
            var points = boundary.Select(p => TopologyMapProjection.Project(map, _chart.MapViewport, p.X, p.Y)).Select(p => Point(new ChartPoint(p.X, p.Y))).ToArray();
            if (points.Length < 2) continue;
            var commands = points.Select((p, i) => i == 0 ? ChartPathCommand.MoveTo(p.X, p.Y) : ChartPathCommand.LineTo(p.X, p.Y)).ToArray();
            _builder.Path(new ChartPath(commands), soft && TopologyMapProjection.CanFillBoundary(boundary) ? _colors.Border.WithOpacity(.15) : null, _colors.Border.WithOpacity(.45), .7 * _scale, "topology-map-boundary", close: soft);
        }
    }

    private void BuildGroups() {
        if (_options.IncludeGroups) foreach (var group in _chart.Groups) {
            var active = _highlight.IsGroupHighlighted(group);
            var accent = Highlight(Color(group.Color, Status(group.Status)), active);
            var bounds = Bounds(group.X, group.Y, group.Width, group.Height);
            using (Host(group.Href, group.Tooltip, group.Id, "topology-group-host"))
            using (_builder.PushGroup(group.Id, "topology-group", new Dictionary<string, string> { ["data-status"] = group.Status.ToString() })) {
                var fill = _options.GroupSurfaceStyle == TopologyGroupSurfaceStyle.Neutral ? _colors.Surface : accent.WithOpacity(.06);
                _builder.Rect(bounds, Highlight(fill, active), accent.WithOpacity(.5), (_options.SelectedGroupIds.Contains(group.Id) ? 2.4 : _context.Theme.AxisStrokeWidth) * _scale, _context.Theme.BarRadius * _scale, "topology-group-surface");
                if (_options.IncludeGroupLabels) {
                    var icon = ResolveGroupIcon(group, _options);
                    if (_options.RequireResolvedIcons && group.IconId != null && icon == null) throw new InvalidOperationException("Unresolved topology group icon: " + group.IconId);
                    var symbol = group.Symbol ?? icon?.Symbol;
                    var reserve = string.IsNullOrWhiteSpace(symbol) && string.IsNullOrWhiteSpace(group.IconId) ? 0 : 28;
                    if (reserve > 0) BuildGlyph(new TopologyNode { IconId = group.IconId, Symbol = symbol, Kind = icon?.NodeKind ?? TopologyNodeKind.Hub }, group.X + 22, group.Y + 21, accent, .8);
                    Text(group.Label, Bounds(group.X + 12 + reserve, group.Y + 9, Math.Max(0, group.Width - 24 - reserve), 24), _context.Theme.Typography.DataLabelSize, Highlight(_colors.Foreground, active), 600, "topology-group-label");
                    if (!string.IsNullOrWhiteSpace(group.Subtitle)) Text(group.Subtitle!, Bounds(group.X + 12 + reserve, group.Y + 31, Math.Max(0, group.Width - 24 - reserve), 18), _context.Theme.Typography.DataLabelSize * .85, Highlight(_colors.MutedForeground, active), 400, "topology-group-subtitle");
                    if (_options.IncludeGroupStatusDots) {
                        using (PinnedState()) _builder.Ellipse(bounds.Right - 12 * _scale, bounds.Y + 16 * _scale, 4 * _scale, 4 * _scale, accent, role: "topology-group-status");
                    }
                }
            }
            _builder.AddRegion(new VisualSemanticRegion(group.Id, "topology-group", bounds, group.Label));
        }
        if (_chart.LayoutMode != TopologyLayoutMode.Geographic) return;
        if (_options.IncludeGeographicRegionHulls) foreach (var group in _chart.Groups) {
            if (!group.Longitude.HasValue || !group.Latitude.HasValue || !TopologyMapProjection.IsVisible(_chart.MapViewport, group.Longitude.Value, group.Latitude.Value)) continue;
            var mapChart = TopologyLayoutEngine.Clone(_chart); mapChart.Legend = null;
            var map = TopologyMapProjection.MapRect(mapChart);
            var anchor = TopologyMapProjection.Project(map, _chart.MapViewport, group.Longitude.Value, group.Latitude.Value);
            var p = Point(new ChartPoint(anchor.X, anchor.Y));
            var radius = _options.GeographicRegionHullMinRadius;
            foreach (var node in _chart.Nodes.Where(node => node.GroupId == group.Id && node.Longitude.HasValue && node.Latitude.HasValue && EffectiveNodeDisplayMode(node, _options) != TopologyNodeDisplayMode.Hidden)) {
                if (!TopologyMapProjection.IsVisible(_chart.MapViewport, node.Longitude!.Value, node.Latitude!.Value)) continue;
                var projected = TopologyMapProjection.Project(map, _chart.MapViewport, node.Longitude.Value, node.Latitude.Value);
                var dx = projected.X - anchor.X; var dy = projected.Y - anchor.Y;
                radius = Math.Max(radius, Math.Sqrt(dx * dx + dy * dy) + _options.GeographicRegionHullPadding);
            }
            radius = Math.Min(_options.GeographicRegionHullMaxRadius, Math.Max(_options.GeographicRegionHullMinRadius, radius)) * _scale;
            var accent = Color(group.Color, Status(group.Status));
            _builder.Ellipse(p.X, p.Y, radius, radius, accent.WithOpacity(.05), accent.WithOpacity(.3), _context.Theme.AxisStrokeWidth * _scale, "topology-geographic-hull");
        }
        var calloutChart = TopologyLayoutEngine.Clone(_chart); calloutChart.Legend = null;
        foreach (var callout in TopologyGeographicCallouts.Build(calloutChart, _options, Theme())) {
            var bounds = Bounds(callout.X, callout.Y, callout.Width, callout.Height);
            var accent = Color(callout.AccentColor, Status(callout.Group.Status));
            var leader = TopologyGeographicCalloutPrimitives.LeaderPoints(callout).Select(Point).ToArray();
            for (var i = 1; i < leader.Length; i++) _builder.Line(leader[i - 1].X, leader[i - 1].Y, leader[i].X, leader[i].Y, accent.WithOpacity(.6), _context.Theme.AxisStrokeWidth * _scale, "topology-callout-leader");
            _builder.Rect(bounds, _colors.Surface, accent, _context.Theme.AxisStrokeWidth * _scale, _context.Theme.BarRadius * _scale, "topology-callout");
            BuildCalloutPreview(callout, accent);
            Text(callout.Label, Bounds(callout.X + 12, callout.Y + 8, callout.Width - 80, 24), _context.Theme.Typography.DataLabelSize, _colors.Foreground, 600, "topology-callout-title");
            Text(callout.Subtitle, Bounds(callout.X + 12, callout.Y + 32, callout.Width - 80, 18), _context.Theme.Typography.DataLabelSize * .85, _colors.MutedForeground, 400, "topology-callout-subtitle");
            var counts = new[] { callout.HealthyCount, callout.WarningCount, callout.CriticalCount, callout.UnknownCount + callout.DisabledCount };
            var statuses = new[] { TopologyHealthStatus.Healthy, TopologyHealthStatus.Warning, TopologyHealthStatus.Critical, TopologyHealthStatus.Unknown };
            for (var i = 0; i < counts.Length; i++) {
                var b = Bounds(callout.X + 12 + i * 42, callout.Y + 58, 36, 22);
                using (PinnedState()) _builder.Rect(b, Status(statuses[i]).WithOpacity(.1), Status(statuses[i]), .7 * _scale, b.Height / 2, "topology-callout-status");
                Text(counts[i].ToString(System.Globalization.CultureInfo.InvariantCulture), b, _context.Theme.Typography.DataLabelSize * .8, _colors.Foreground, 400, "topology-callout-count", centered: true);
            }
            _builder.AddRegion(new VisualSemanticRegion(callout.Group.Id + "-callout", "topology-callout", bounds, callout.Label));
        }
    }

    private void BuildCalloutPreview(TopologyGeographicCallout callout, ChartColor accent) {
        var x = callout.X + callout.Width - TopologyGeographicCalloutPrimitives.MiniTopologyRightInset;
        var y = callout.Y + TopologyGeographicCalloutPrimitives.MiniTopologyYOffset;
        var center = Point(TopologyGeographicCalloutPrimitives.MiniTopologyCenter(x, y));
        var nodes = TopologyGeographicCalloutPrimitives.MiniTopologyPoints(x, y);
        var statuses = TopologyGeographicCalloutPrimitives.PreviewStatuses(callout);
        using (PinnedState()) {
            for (var i = 0; i < nodes.Length; i++) {
                var point = Point(nodes[i]); var color = Status(statuses[i]);
                _builder.Line(center.X, center.Y, point.X, point.Y, accent, _context.Theme.AxisStrokeWidth * _scale, "topology-callout-preview-link");
                _builder.Ellipse(point.X, point.Y, TopologyGeographicCalloutPrimitives.MiniTopologyNodeRadius * _scale,
                    TopologyGeographicCalloutPrimitives.MiniTopologyNodeRadius * _scale, color, role: "topology-callout-preview-node");
            }
            _builder.Ellipse(center.X, center.Y, TopologyGeographicCalloutPrimitives.MiniTopologyCenterRadius * _scale,
                TopologyGeographicCalloutPrimitives.MiniTopologyCenterRadius * _scale, accent, role: "topology-callout-preview-center");
        }
    }
}
