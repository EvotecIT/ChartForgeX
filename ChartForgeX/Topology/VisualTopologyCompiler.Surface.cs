using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private void BuildSurface() {
        if (_options.CanvasSurfaceStyle != TopologyCanvasSurfaceStyle.Plain) {
            _builder.Rect(_plot, _colors.Surface, _colors.Border, _context.Theme.AxisStrokeWidth, _context.Theme.BarRadius, "topology-panel", paint: new VisualScenePaintBinding(CardPaint(_colors.Surface), BorderPaint(_colors.Border)));
            if (_options.CanvasSurfaceStyle == TopologyCanvasSurfaceStyle.PanelGrid) {
                var gap = _context.Theme.Spacing * 2;
                for (var x = _plot.X + gap; x < _plot.Right; x += gap) _builder.Line(x, _plot.Y, x, _plot.Bottom, ChartColorMath.WithOpacity(_colors.Border, .25), .5, "topology-grid", paint: new VisualScenePaintBinding(stroke: BorderPaint(ChartColorMath.WithOpacity(_colors.Border, .25), .25, SvgColorRole.Grid)));
                for (var y = _plot.Y + gap; y < _plot.Bottom; y += gap) _builder.Line(_plot.X, y, _plot.Right, y, ChartColorMath.WithOpacity(_colors.Border, .25), .5, "topology-grid", paint: new VisualScenePaintBinding(stroke: BorderPaint(ChartColorMath.WithOpacity(_colors.Border, .25), .25, SvgColorRole.Grid)));
            }
        }
        if (_chart.LayoutMode != TopologyLayoutMode.Geographic) return;
        var mapChart = TopologyLayoutEngine.Clone(_chart); mapChart.Legend = null;
        var map = TopologyMapProjection.MapRect(mapChart);
        var soft = UseSoftMapBackground(_options);
        BuildGeographicFrame(map, soft);
        if (!soft) {
            foreach (var land in TopologyMapProjection.LandDots(_chart.MapViewport)) {
                if (!TopologyMapProjection.IsVisible(_chart.MapViewport, land.X, land.Y)) continue;
                var projected = TopologyMapProjection.Project(map, _chart.MapViewport, land.X, land.Y);
                var p = Point(new ChartPoint(projected.X, projected.Y));
                var radius = TopologyMapProjection.LandDotRadius(map, _chart.MapViewport) * _scale;
                _builder.Ellipse(p.X, p.Y, radius, radius, ChartColorMath.WithOpacity(_colors.Border, .65), role: "topology-map-land", paint: new VisualScenePaintBinding(BorderPaint(ChartColorMath.WithOpacity(_colors.Border, .65), .65)));
            }
        }
        foreach (var boundary in TopologyMapProjection.BoundaryLines(_chart.MapViewport)) {
            var points = boundary.Select(p => TopologyMapProjection.Project(map, _chart.MapViewport, p.X, p.Y)).Select(p => Point(new ChartPoint(p.X, p.Y))).ToArray();
            if (points.Length < 2) continue;
            var commands = points.Select((p, i) => i == 0 ? ChartPathCommand.MoveTo(p.X, p.Y) : ChartPathCommand.LineTo(p.X, p.Y)).ToArray();
            var closed = soft && TopologyMapProjection.CanFillBoundary(boundary);
            _builder.Path(new ChartPath(commands), closed ? ChartColorMath.WithOpacity(_colors.Border, .15) : null, ChartColorMath.WithOpacity(_colors.Border, .45), .7 * _scale, "topology-map-boundary", close: closed, paint: new VisualScenePaintBinding(BorderPaint(ChartColorMath.WithOpacity(_colors.Border, .15), .15), BorderPaint(ChartColorMath.WithOpacity(_colors.Border, .45), .45)));
        }
    }

    private bool ShouldFitHeaderLine(double size, double height, int weight = 400) =>
        TextPolicySize(_context.Theme.Typography.DataLabelSize) > VisualTextFitting.MaximumOrdinaryFontSize
        || MeasurePolicyText("Ag", size * _scale, weight).LineHeight > height * _scale;

    private void BuildGroups() {
        var groupSurfaces = new Dictionary<string, GroupCaptionSurface>(StringComparer.Ordinal);
        if (_options.IncludeGroups) foreach (var group in TopologyGroupEndpoints.InPaintOrder(_chart)) {
            using var textPolicy = UseTextPolicy(group.TextStyle);
            if (group.TextStyle?.Color is ChartColor authoredTextColor) group.LabelColor = authoredTextColor.ToHexRgba();
            var headerParts = new List<ChartRect>();
            _groupHeaderParts.Add(group.Id, headerParts);
            var active = _highlight.IsGroupHighlighted(group);
            var authoredAccent = group.Color ?? ResolveGroupIcon(group, _options)?.Color;
            var baseAccent = Color(authoredAccent, Status(group.Status));
            var accent = Highlight(baseAccent, active);
            var accentRole = AccentRole(authoredAccent);
            var bounds = Bounds(group.X, group.Y, group.Width, group.Height);
            var parentSurface = group.ParentGroupId != null && groupSurfaces.TryGetValue(group.ParentGroupId, out var inheritedSurface)
                ? inheritedSurface : GroupCaptionBaseSurface();
            if (group.Presentation == TopologyGroupPresentation.Compartment) {
                groupSurfaces[group.Id] = parentSurface;
                using (_builder.PushGroup(group.Id, "topology-group", GroupMetadata(group)))
                    _builder.AddRegion(new VisualSemanticRegion(group.Id, "topology-group", bounds, group.Label));
                continue;
            }
            using (Host(group.Href, group.Tooltip, group.Id, "topology-group-host"))
            using (_builder.PushGroup(group.Id, "topology-group", GroupMetadata(group),opacity:group.Opacity??1)) {
                var neutral = _options.GroupSurfaceStyle == TopologyGroupSurfaceStyle.Neutral;
                var fill = Highlight(string.IsNullOrWhiteSpace(group.BackgroundColor)
                    ? neutral ? _colors.Surface : ChartColorMath.WithOpacity(baseAccent, .06)
                    : Color(group.BackgroundColor, _colors.Surface), active);
                var fillPaint = GroupFillPaint(group, fill, active, neutral);
                var captionFactor=(group.Opacity??1)*(group.SurfaceOpacity??1);
                var captionFill=captionFactor==1?fill:fill.WithOpacity(fill.A/255d*captionFactor);
                var captionSurface=parentSurface.Over(captionFill,captionFactor==1?fillPaint:fillPaint.WithOpacity(captionFill,captionFactor),!string.IsNullOrWhiteSpace(group.BackgroundColor));
                groupSurfaces[group.Id] = captionSurface;
                var stroke = string.IsNullOrWhiteSpace(group.BorderColor) ? ChartColorMath.WithOpacity(accent, .5) : Highlight(Color(group.BorderColor, _colors.Border), active);
                SurfaceRectangle(bounds,fill,stroke,SurfaceStrokeWidth(group.StrokeWidth,_options.SelectedGroupIds.Contains(group.Id)?2.4:0)*_scale,_context.Theme.BarRadius*_scale,"topology-group-surface",
                    SurfaceDash(group.DashPattern),new VisualScenePaintBinding(fillPaint,GroupBorderPaint(group,stroke,active)),group.SurfaceOpacity??1);
                BuildCompartmentSeparators(group);
                if (_options.IncludeGroupLabels) {
                    var icon = ResolveGroupIcon(group, _options);
                    if (_options.RequireResolvedIcons && !string.IsNullOrWhiteSpace(group.IconId) && icon == null) throw new InvalidOperationException("Unresolved topology group icon: " + group.IconId);
                    var symbol = group.Symbol ?? icon?.Symbol;
                    var symbolCenter = TopologyGroupHeader.SymbolCenter(group);
                    if (TopologyGroupHeader.SymbolReserve(group, _options) > 0) {
                        BuildGlyph(new TopologyNode { IconId = group.IconId, Symbol = symbol, Kind = icon?.NodeKind ?? TopologyNodeKind.Hub, Color = authoredAccent, Status = group.Status }, symbolCenter.X, symbolCenter.Y, accent, TopologyGroupHeader.SymbolScale, accentRole, artworkOpacity: HighlightFactor(active));
                        headerParts.Add(TopologyGroupHeader.SymbolBounds(group));
                    }
                    if (group.ShowLabel) {
                        var titleLineHeight = (group.LabelContent.IsStructured ? CaptionLineHeight(group.LabelContent, TextPolicySize(_context.Theme.Typography.DataLabelSize) * _scale) : MeasurePolicyText("Ag", TextPolicySize(_context.Theme.Typography.DataLabelSize) * _scale, 600).LineHeight) / _scale;
                        var subtitleLineHeight = MeasurePolicyText("Ag", TextPolicySize(_context.Theme.Typography.DataLabelSize) * .85 * _scale, 400).LineHeight / _scale;
                        var titleRows = TopologyGroupHeader.CaptionRows(group.Label, _options);
                        var subtitleRows = TopologyGroupHeader.CaptionRows(group.Subtitle, _options);
                        var titleBounds = TopologyGroupHeader.TitleBounds(group, _options, titleLineHeight);
                        var fitTitle = group.TextStyle?.HasOverrides != true && titleRows == 1 && ShouldFitHeaderLine(TextPolicySize(_context.Theme.Typography.DataLabelSize), titleBounds.Height, 600);
                        var fitSubtitle = group.TextStyle?.HasOverrides != true && subtitleRows == 1 && ShouldFitHeaderLine(TextPolicySize(_context.Theme.Typography.DataLabelSize) * .85, TopologyGroupHeader.SubtitleHeight);
                        var titleColor = Highlight(Color(group.LabelColor, GroupCaptionInk(captionSurface, _colors.Foreground)), active);
                        var titlePaint = string.IsNullOrWhiteSpace(group.LabelColor)
                            ? GroupCaptionPaint(captionSurface, titleColor, active, false) : GroupLabelPaint(group, titleColor, active);
                        CaptureCaption(Text(group.LabelContent, Bounds(titleBounds.X, titleBounds.Y, titleBounds.Width, titleBounds.Height), TextPolicySize(_context.Theme.Typography.DataLabelSize), titleColor, 600, "topology-group-label", opacity: HighlightFactor(active), paint: titlePaint, maxLines: titleRows, fitSingleLine: fitTitle));
                        if (!string.IsNullOrWhiteSpace(group.Subtitle)) {
                            var subtitleBounds = TopologyGroupHeader.SubtitleBounds(group, _options, fitTitle || fitSubtitle, titleLineHeight, subtitleLineHeight);
                            var subtitleColor = Highlight(Color(group.LabelColor, GroupCaptionInk(captionSurface, _colors.MutedForeground)), active);
                            var subtitlePaint = string.IsNullOrWhiteSpace(group.LabelColor)
                                ? GroupCaptionPaint(captionSurface, subtitleColor, active, true) : GroupLabelPaint(group, subtitleColor, active, true);
                            CaptureCaption(Text(group.Subtitle!, Bounds(subtitleBounds.X, subtitleBounds.Y, subtitleBounds.Width, subtitleBounds.Height), TextPolicySize(_context.Theme.Typography.DataLabelSize) * .85, subtitleColor, 400, "topology-group-subtitle", paint: subtitlePaint, maxLines: subtitleRows, fitSingleLine: fitSubtitle));
                        }
                    }
                    if (_options.IncludeGroupStatusDots) {
                        var status = Highlight(Status(group.Status), active);
                        var statusCenter = Point(TopologyGroupHeader.StatusCenter(group));
                        using (PinnedState()) _builder.Ellipse(statusCenter.X, statusCenter.Y, TopologyGroupHeader.StatusRadius * _scale, TopologyGroupHeader.StatusRadius * _scale, status, role: "topology-group-status", paint: new VisualScenePaintBinding(StatusPaint(group.Status, status, HighlightFactor(active))));
                        headerParts.Add(TopologyGroupHeader.StatusBounds(group));
                    }
                }
            }
            _builder.AddRegion(new VisualSemanticRegion(group.Id, "topology-group", bounds, group.Label));

            void CaptureCaption(ChartRect? painted) {
                if (painted.HasValue) {
                    var part = painted.Value;
                    headerParts.Add(new ChartRect((part.X - _offsetX) / _scale, (part.Y - _offsetY) / _scale,
                        part.Width / _scale, part.Height / _scale));
                }
            }
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
            var active = _highlight.IsGroupHighlighted(group);
            var accent = Highlight(Color(group.Color ?? ResolveGroupIcon(group, _options)?.Color, Status(group.Status)), active);
            var hullFill = ChartColorMath.WithOpacity(accent, .05); var hullStroke = ChartColorMath.WithOpacity(accent, .3);
            using (_builder.PushGroup(group.Id + "-hull", "topology-geographic-hull", new Dictionary<string, string> {
                ["data-group-id"] = group.Id, ["data-hull-padding"] = Number(_options.GeographicRegionHullPadding * _scale)
            })) _builder.Ellipse(p.X, p.Y, radius, radius, hullFill, hullStroke, _context.Theme.AxisStrokeWidth * _scale,
                paint: new VisualScenePaintBinding(GroupAccentPaint(group, hullFill, active, .05), GroupAccentPaint(group, hullStroke, active, .3)));
        }
        var calloutChart = TopologyLayoutEngine.Clone(_chart); calloutChart.Legend = null;
        foreach (var callout in TopologyGeographicCallouts.Build(calloutChart, _options, Theme())) {
            using var host = Host(callout.Group.Href, callout.Group.Tooltip, callout.Group.Id + "-callout", "topology-group-host");
            using var group = _builder.PushGroup(callout.Group.Id + "-callout", "topology-group", CalloutMetadata(callout));
            var bounds = Bounds(callout.X, callout.Y, callout.Width, callout.Height);
            var active = _highlight.IsGroupHighlighted(callout.Group);
            var accent = Highlight(Color(callout.Group.Color ?? ResolveGroupIcon(callout.Group, _options)?.Color ?? callout.AccentColor, Status(callout.Group.Status)), active);
            var accentRole = AccentRole(callout.Group.Color);
            var anchor = Point(new ChartPoint(callout.AnchorX, callout.AnchorY));
            _builder.Ellipse(anchor.X, anchor.Y, TopologyGeographicCalloutPrimitives.AnchorHaloRadius * _scale,
                TopologyGeographicCalloutPrimitives.AnchorHaloRadius * _scale, _colors.Background, role: "topology-geographic-callout-anchor-halo", paint: new VisualScenePaintBinding(BackgroundPaint(_colors.Background)));
            _builder.Ellipse(anchor.X, anchor.Y, TopologyGeographicCalloutPrimitives.AnchorRadius * _scale,
                TopologyGeographicCalloutPrimitives.AnchorRadius * _scale, accent, role: "topology-geographic-callout-anchor", paint: new VisualScenePaintBinding(GroupAccentPaint(callout.Group, accent, active)));
            var leader = TopologyGeographicCalloutPrimitives.LeaderPoints(callout).Select(Point).ToArray();
            var leaderPath = new ChartPath(leader.Select((point, index) => index == 0 ? ChartPathCommand.MoveTo(point.X, point.Y) : ChartPathCommand.LineTo(point.X, point.Y)).ToArray());
            var leaderStyle = ChartRouteVisualStyles.TopologyGeographicCalloutLeader();
            var leaderHalo = ChartColorMath.WithOpacity(_colors.Background, leaderStyle.HaloOpacity);
            var leaderInk = ChartColorMath.WithOpacity(accent, leaderStyle.StrokeOpacity);
            _builder.Path(leaderPath, stroke: leaderHalo, strokeWidth: leaderStyle.HaloStrokeWidth * _scale,
                role: "topology-geographic-callout-leader-halo", paint: new VisualScenePaintBinding(stroke: BackgroundPaint(leaderHalo, leaderStyle.HaloOpacity)));
            _builder.Path(leaderPath, stroke: leaderInk, strokeWidth: leaderStyle.StrokeWidth * _scale,
                role: "topology-geographic-callout-leader", dash: new[] { leaderStyle.Dash * _scale, leaderStyle.Gap * _scale },
                paint: new VisualScenePaintBinding(stroke: GroupAccentPaint(callout.Group, leaderInk, active, leaderStyle.StrokeOpacity)));
            var calloutFill = Color(callout.Group.BackgroundColor, _colors.Surface);
            var calloutBorder = string.IsNullOrWhiteSpace(callout.Group.BorderColor) ? accent : Highlight(Color(callout.Group.BorderColor, _colors.Border), active);
            _builder.Rect(bounds, calloutFill, calloutBorder, _context.Theme.AxisStrokeWidth * _scale, _context.Theme.BarRadius * _scale, "topology-callout", paint: new VisualScenePaintBinding(GroupFillPaint(callout.Group, calloutFill, true, true), GroupBorderPaint(callout.Group, calloutBorder, active, 1)));
            BuildCalloutPreview(callout, accent, accentRole);
            if (callout.Group.ShowLabel) {
                Text(callout.Label, Bounds(callout.X + 12, callout.Y + 8, callout.Width - 80, 24), TextPolicySize(_context.Theme.Typography.DataLabelSize), Color(callout.Group.LabelColor, _colors.Foreground), 600, "topology-callout-title", paint: string.IsNullOrWhiteSpace(callout.Group.LabelColor) ? null : GroupLabelPaint(callout.Group, Color(callout.Group.LabelColor, _colors.Foreground), true), fitSingleLine: ShouldFitHeaderLine(TextPolicySize(_context.Theme.Typography.DataLabelSize), 24, 600));
                Text(callout.Subtitle, Bounds(callout.X + 12, callout.Y + 32, callout.Width - 80, 18), TextPolicySize(_context.Theme.Typography.DataLabelSize) * .85, Color(callout.Group.LabelColor, _colors.MutedForeground), 400, "topology-callout-subtitle", paint: string.IsNullOrWhiteSpace(callout.Group.LabelColor) ? MutedPaint(_colors.MutedForeground) : GroupLabelPaint(callout.Group, Color(callout.Group.LabelColor, _colors.MutedForeground), true, true), fitSingleLine: ShouldFitHeaderLine(TextPolicySize(_context.Theme.Typography.DataLabelSize) * .85, 18));
            }
            var counts = new[] { callout.HealthyCount, callout.WarningCount, callout.CriticalCount, callout.UnknownCount + callout.DisabledCount };
            var statuses = new[] { TopologyHealthStatus.Healthy, TopologyHealthStatus.Warning, TopologyHealthStatus.Critical, TopologyHealthStatus.Unknown };
            for (var i = 0; i < counts.Length; i++) {
                var b = Bounds(callout.X + 12 + i * 42, callout.Y + 58, 36, 22);
                using (PinnedState()) _builder.Rect(b, ChartColorMath.WithOpacity(Status(statuses[i]), .1), Status(statuses[i]), _context.Theme.AxisStrokeWidth * _scale, b.Height / 2, "topology-callout-status", paint: new VisualScenePaintBinding(StatusPaint(statuses[i], ChartColorMath.WithOpacity(Status(statuses[i]), .1), .1), StatusPaint(statuses[i], Status(statuses[i]))));
                Text(counts[i].ToString(System.Globalization.CultureInfo.InvariantCulture), b, TextPolicySize(_context.Theme.Typography.DataLabelSize) * .8, _colors.Foreground, 400, "topology-callout-count", centered: true, fitSingleLine: ShouldFitHeaderLine(TextPolicySize(_context.Theme.Typography.DataLabelSize) * .8, 22));
            }
            _builder.AddRegion(new VisualSemanticRegion(callout.Group.Id + "-callout", "topology-callout", bounds, callout.Label));
        }
    }

    private void BuildCalloutPreview(TopologyGeographicCallout callout, ChartColor accent, SvgColorRole accentRole) {
        var x = callout.X + callout.Width - TopologyGeographicCalloutPrimitives.MiniTopologyRightInset;
        var y = callout.Y + TopologyGeographicCalloutPrimitives.MiniTopologyYOffset;
        var center = Point(TopologyGeographicCalloutPrimitives.MiniTopologyCenter(x, y));
        var nodes = TopologyGeographicCalloutPrimitives.MiniTopologyPoints(x, y);
        var statuses = TopologyGeographicCalloutPrimitives.PreviewStatuses(callout);
        using (PinnedState()) {
            for (var i = 0; i < nodes.Length; i++) {
                var point = Point(nodes[i]); var color = Status(statuses[i]);
                _builder.Line(center.X, center.Y, point.X, point.Y, accent, _context.Theme.AxisStrokeWidth * _scale, "topology-callout-preview-link", paint: new VisualScenePaintBinding(stroke: GroupAccentPaint(callout.Group, accent, _highlight.IsGroupHighlighted(callout.Group))));
                _builder.Ellipse(point.X, point.Y, TopologyGeographicCalloutPrimitives.MiniTopologyNodeRadius * _scale,
                    TopologyGeographicCalloutPrimitives.MiniTopologyNodeRadius * _scale, color, role: "topology-callout-preview-node", paint: new VisualScenePaintBinding(StatusPaint(statuses[i], color)));
            }
            _builder.Ellipse(center.X, center.Y, TopologyGeographicCalloutPrimitives.MiniTopologyCenterRadius * _scale,
                TopologyGeographicCalloutPrimitives.MiniTopologyCenterRadius * _scale, accent, role: "topology-callout-preview-center", paint: new VisualScenePaintBinding(GroupAccentPaint(callout.Group, accent, _highlight.IsGroupHighlighted(callout.Group))));
        }
    }
}
