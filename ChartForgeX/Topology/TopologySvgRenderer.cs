using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

/// <summary>
/// Renders topology charts to static SVG markup.
/// </summary>
public sealed partial class TopologySvgRenderer {
    /// <summary>
    /// Renders a topology chart to complete SVG markup.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="options">Optional render options.</param>
    /// <returns>Complete SVG markup.</returns>
    public string Render(TopologyChart chart, TopologyRenderOptions? options = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        options ??= new TopologyRenderOptions();
        if (options.Preset != TopologyViewPreset.Default) options.ApplyPreset(options.Preset);
        var prepared = TopologyLayoutEngine.Prepare(chart, options.View, options);
        var validation = new TopologyChartValidator().Validate(prepared);
        if (!validation.IsValid) throw new TopologyValidationException(validation);

        var theme = prepared.Theme ?? TopologyTheme.Light();
        var prefix = string.IsNullOrWhiteSpace(options.CssClassPrefix) ? "cfx-topology" : options.CssClassPrefix!;
        var id = SanitizeId(string.IsNullOrWhiteSpace(prepared.Id) ? "topology" : prepared.Id!);
        var w = prepared.Viewport.Width;
        var h = prepared.Viewport.Height;
        var highlight = TopologyHighlightState.From(prepared, options);
        var document = SvgDocument.Create(w, h, "0 0 " + F(w) + " " + F(h));
        document.Root
            .Attribute("role", "img")
            .Attribute("aria-labelledby", id + "-title " + id + "-desc")
            .Attribute("shape-rendering", "geometricPrecision")
            .Attribute("text-rendering", "geometricPrecision");
        if (options.UseResponsiveSvg) document.Root.Attribute("style", "max-width:100%;height:auto;display:block");

        document.Root.Element("title", title => title
            .Attribute("id", id + "-title")
            .Text(string.IsNullOrWhiteSpace(prepared.Title) ? "ChartForgeX topology" : prepared.Title!));
        document.Root.Element("desc", desc => desc
            .Attribute("id", id + "-desc")
            .Text(BuildDescription(prepared)));
        document.Root.AddElement(BuildDefs(id, prefix, theme, options));
        document.Root.Element("g", root => {
            root
                .Attribute("id", id)
                .Class(prefix)
                .Attribute("data-cfx-role", "topology")
                .Attribute("data-chart-id", prepared.Id ?? id)
                .Attribute("data-layout-mode", prepared.LayoutMode.ToString())
                .Attribute("data-layout-direction", prepared.LayoutDirection.ToString())
                .Attribute("data-node-display-mode", options.NodeDisplayMode.ToString())
                .Attribute("data-cfx-projection", prepared.LayoutMode == TopologyLayoutMode.Geographic ? TopologyMapProjection.ProjectionName : null)
                .Attribute("data-cfx-viewport", prepared.LayoutMode == TopologyLayoutMode.Geographic ? prepared.MapViewport.Name : null)
                .Attribute("data-cfx-viewport-min-longitude", prepared.LayoutMode == TopologyLayoutMode.Geographic ? F(prepared.MapViewport.MinimumLongitude) : null)
                .Attribute("data-cfx-viewport-max-longitude", prepared.LayoutMode == TopologyLayoutMode.Geographic ? F(prepared.MapViewport.MaximumLongitude) : null)
                .Attribute("data-cfx-viewport-min-latitude", prepared.LayoutMode == TopologyLayoutMode.Geographic ? F(prepared.MapViewport.MinimumLatitude) : null)
                .Attribute("data-cfx-viewport-max-latitude", prepared.LayoutMode == TopologyLayoutMode.Geographic ? F(prepared.MapViewport.MaximumLatitude) : null)
                .Raw(BuildBodyMarkup(prepared, prefix, theme, options, id, highlight));
        });

        return document.ToMarkup();
    }

    private static string BuildBodyMarkup(TopologyChart chart, string prefix, TopologyTheme theme, TopologyRenderOptions options, string id, TopologyHighlightState highlight) {
        var sb = new StringBuilder();
        sb.Append(ElementMarkup(new SvgElement("rect")
            .Class(prefix + "__background")
            .Attribute("width", "100%")
            .Attribute("height", "100%")
            .Attribute("fill", theme.Background)));
        if (options.IncludeTitle) DrawHeader(sb, chart, prefix, theme);
        if (chart.LayoutMode == TopologyLayoutMode.Geographic) DrawGeographicFrame(sb, chart, prefix, theme);
        if (options.IncludeGroups) DrawGroups(sb, chart, prefix, theme, options, highlight);
        DrawEdges(sb, chart, prefix, theme, options, id, highlight);
        DrawEdgeLabels(sb, chart, prefix, theme, options, highlight);
        DrawNodes(sb, chart, prefix, theme, options, highlight);
        if (options.IncludeStatusBadges) DrawNodeStatuses(sb, chart, prefix, theme, options, highlight);
        if (chart.LayoutMode == TopologyLayoutMode.Geographic) DrawGeographicCallouts(sb, chart, prefix, theme, options, highlight);
        if (options.IncludeLegend && chart.Legend != null) DrawLegend(sb, chart, prefix, theme);
        return sb.ToString();
    }

    private static void DrawGeographicFrame(StringBuilder sb, TopologyChart chart, string prefix, TopologyTheme theme) {
        var map = TopologyMapProjection.MapRect(chart);
        var layer = new SvgElement("g")
            .Class(prefix + "__geo-frame")
            .Attribute("data-cfx-role", "topology-geographic-frame")
            .Attribute("data-cfx-projection", TopologyMapProjection.ProjectionName)
            .Attribute("data-cfx-viewport", chart.MapViewport.Name)
            .Attribute("data-cfx-viewport-min-longitude", F(chart.MapViewport.MinimumLongitude))
            .Attribute("data-cfx-viewport-max-longitude", F(chart.MapViewport.MaximumLongitude))
            .Attribute("data-cfx-viewport-min-latitude", F(chart.MapViewport.MinimumLatitude))
            .Attribute("data-cfx-viewport-max-latitude", F(chart.MapViewport.MaximumLatitude));
        layer.Element("rect", rect => rect
            .Attribute("x", map.Left)
            .Attribute("y", map.Top)
            .Attribute("width", map.Width)
            .Attribute("height", map.Height)
            .Attribute("rx", 16)
            .Attribute("fill", StatusFill(theme.Accent, theme.Background))
            .Attribute("stroke", theme.Border)
            .Attribute("stroke-width", 1));
        DrawGeographicLandLayer(layer, chart, map, theme);
        for (var i = 1; i < 4; i++) {
            var longitude = chart.MapViewport.MinimumLongitude + (chart.MapViewport.MaximumLongitude - chart.MapViewport.MinimumLongitude) * i / 4.0;
            var x = TopologyMapProjection.Project(map, chart.MapViewport, longitude, chart.MapViewport.MinimumLatitude).X;
            layer.Element("line", line => line
                .Attribute("data-cfx-role", "topology-geographic-graticule")
                .Attribute("data-cfx-axis", "longitude")
                .Attribute("data-cfx-longitude", F(longitude))
                .Attribute("x1", x)
                .Attribute("y1", map.Top)
                .Attribute("x2", x)
                .Attribute("y2", map.Bottom)
                .Attribute("stroke", theme.Border)
                .Attribute("stroke-opacity", 0.42)
                .Attribute("stroke-width", 0.8));
        }

        for (var i = 1; i < 3; i++) {
            var latitude = chart.MapViewport.MinimumLatitude + (chart.MapViewport.MaximumLatitude - chart.MapViewport.MinimumLatitude) * i / 3.0;
            var y = TopologyMapProjection.Project(map, chart.MapViewport, chart.MapViewport.MinimumLongitude, latitude).Y;
            layer.Element("line", line => line
                .Attribute("data-cfx-role", "topology-geographic-graticule")
                .Attribute("data-cfx-axis", "latitude")
                .Attribute("data-cfx-latitude", F(latitude))
                .Attribute("x1", map.Left)
                .Attribute("y1", y)
                .Attribute("x2", map.Right)
                .Attribute("y2", y)
                .Attribute("stroke", theme.Border)
                .Attribute("stroke-opacity", 0.34)
                .Attribute("stroke-width", 0.8));
        }

        sb.Append(ElementMarkup(layer));
    }

    private static void DrawGeographicLandLayer(SvgElement layer, TopologyChart chart, ChartRect map, TopologyTheme theme) {
        var landFill = theme.MutedForeground;
        foreach (var boundary in TopologyMapProjection.BoundaryLines(chart.MapViewport)) {
            var path = GeographicBoundaryPath(boundary, map, chart.MapViewport);
            if (TopologyMapProjection.CanFillBoundary(boundary)) {
                layer.Element("path", element => element
                    .Attribute("data-cfx-role", "topology-geographic-land-area")
                    .Attribute("d", path)
                    .Attribute("fill", landFill)
                    .Attribute("fill-opacity", 0.075)
                    .Attribute("stroke", "none"));
            }

            layer.Element("path", element => element
                .Attribute("data-cfx-role", "topology-geographic-boundary")
                .Attribute("d", path)
                .Attribute("fill", "none")
                .Attribute("stroke", landFill)
                .Attribute("stroke-opacity", 0.22)
                .Attribute("stroke-width", 0.75)
                .Attribute("stroke-linejoin", "round")
                .Attribute("stroke-linecap", "round"));
        }

        var radius = TopologyMapProjection.LandDotRadius(map, chart.MapViewport);
        foreach (var land in TopologyMapProjection.LandDots(chart.MapViewport)) {
            foreach (var offset in TopologyMapProjection.LandOffsets(chart.MapViewport)) {
                var longitude = land.X + offset.X;
                var latitude = land.Y + offset.Y;
                if (!TopologyMapProjection.IsVisible(chart.MapViewport, longitude, latitude)) continue;
                var point = TopologyMapProjection.Project(map, chart.MapViewport, longitude, latitude);
                layer.Element("circle", circle => circle
                    .Attribute("data-cfx-role", "topology-geographic-land-dot")
                    .Attribute("cx", point.X)
                    .Attribute("cy", point.Y)
                    .Attribute("r", radius)
                    .Attribute("fill", landFill)
                    .Attribute("opacity", 0.105));
            }
        }
    }

    private static string GeographicBoundaryPath(ChartPoint[] boundary, ChartRect map, ChartMapViewport viewport) {
        var path = new SvgPathDataBuilder();
        for (var i = 0; i < boundary.Length; i++) {
            var point = TopologyMapProjection.Project(map, viewport, boundary[i].X, boundary[i].Y);
            if (i == 0) path.MoveTo(point.X, point.Y);
            else path.LineTo(point.X, point.Y);
        }

        return path.ToString();
    }

    private static SvgElement BuildDefs(string id, string prefix, TopologyTheme theme, TopologyRenderOptions options) {
        var defs = new SvgElement("defs");
        if (options.IncludeCss) {
            defs.Element("style", style => style.Text(BuildCss(id, prefix, theme)));
        }

        AddDropShadowFilter(defs, id + "-shadow", "#0F172A", 0.10);
        AddDropShadowFilter(defs, id + "-selected-shadow", "#2563EB", 0.18);
        foreach (var status in Enum.GetValues(typeof(TopologyHealthStatus)).Cast<TopologyHealthStatus>()) {
            var color = theme.StatusColor(status);
            defs.Element("marker", marker => {
                marker
                    .Attribute("id", id + "-arrow-" + StatusMarkerToken(status))
                    .Attribute("viewBox", "0 0 10 10")
                    .Attribute("refX", 8)
                    .Attribute("refY", 5)
                    .Attribute("markerWidth", 7)
                    .Attribute("markerHeight", 7)
                    .Attribute("orient", "auto-start-reverse");
                marker.Element("path", path => path
                    .Attribute("d", "M 0 0 L 10 5 L 0 10 z")
                    .Attribute("fill", color));
            });
        }

        return defs;
    }

    private static string BuildCss(string id, string prefix, TopologyTheme theme) {
        var sb = new StringBuilder();
        sb.Append("#" + id + " text{font-family:" + CssFontFamily(theme.FontFamily) + ";font-synthesis:none;letter-spacing:0}");
        sb.Append("#" + id + " ." + prefix + "__link{cursor:pointer}");
        sb.Append("#" + id + " ." + prefix + "__edge{fill:none;stroke-linecap:round;stroke-linejoin:round;vector-effect:non-scaling-stroke}");
        sb.Append("#" + id + " ." + prefix + "__node-card,#" + id + " ." + prefix + "__group-card{vector-effect:non-scaling-stroke}");
        sb.Append("#" + id + " ." + prefix + "--highlighted{filter:url(#" + id + "-shadow)}");
        sb.Append("#" + id + " ." + prefix + "--selected{filter:url(#" + id + "-selected-shadow)}");
        return sb.ToString();
    }

    private static void AddDropShadowFilter(SvgElement defs, string id, string floodColor, double floodOpacity) {
        defs.Element("filter", filter => {
            filter
                .Attribute("id", id)
                .Attribute("x", "-20%")
                .Attribute("y", "-20%")
                .Attribute("width", "140%")
                .Attribute("height", "150%");
            filter.Element("feDropShadow", shadow => shadow
                .Attribute("dx", 0)
                .Attribute("dy", 10)
                .Attribute("stdDeviation", 12)
                .Attribute("flood-color", floodColor)
                .Attribute("flood-opacity", floodOpacity));
        });
    }

    private static void DrawHeader(StringBuilder sb, TopologyChart chart, string prefix, TopologyTheme theme) {
        if (string.IsNullOrWhiteSpace(chart.Title) && string.IsNullOrWhiteSpace(chart.Subtitle)) return;
        var x = chart.Viewport.Padding;
        var y = chart.Viewport.Padding + 8;
        var header = new SvgElement("g")
            .Class(prefix + "__header")
            .Attribute("data-cfx-role", "topology-header");
        if (!string.IsNullOrWhiteSpace(chart.Title)) {
            header.Element("text", text => text
                .Attribute("x", x)
                .Attribute("y", y + 18)
                .Attribute("fill", theme.Foreground)
                .Attribute("font-size", 22)
                .Attribute("font-weight", "700")
                .Text(chart.Title!));
        }

        if (!string.IsNullOrWhiteSpace(chart.Subtitle)) {
            header.Element("text", text => text
                .Attribute("x", x)
                .Attribute("y", y + 42)
                .Attribute("fill", theme.MutedForeground)
                .Attribute("font-size", 13)
                .Text(chart.Subtitle!));
        }

        sb.Append(ElementMarkup(header));
    }

    private static void DrawGroups(StringBuilder sb, TopologyChart chart, string prefix, TopologyTheme theme, TopologyRenderOptions options, TopologyHighlightState highlight) {
        var layer = new SvgElement("g")
            .Class(prefix + "__groups")
            .Attribute("data-cfx-role", "topology-groups");
        foreach (var group in chart.Groups) {
            var highlighted = highlight.IsGroupHighlighted(group);
            var selected = IsSelected(options.SelectedGroupIds, group.Id);
            var accent = GroupAccentColor(group, theme);
            var parent = AddOptionalLink(layer, group.Href, prefix, options);
            var groupElement = parent.Element("g", element => {
                element
                    .Attribute("id", SafeElementId(chart.Id, "group", group.Id))
                    .Class(prefix + "__group " + prefix + "__group--" + CssToken(group.Status.ToString()) + (selected ? " " + prefix + "--selected" : string.Empty) + highlight.CssClass(prefix, highlighted) + CustomCssClasses(group.CssClass))
                    .Attribute("data-cfx-role", "topology-group")
                    .Attribute("data-group-id", group.Id)
                    .Attribute("data-group-layout-policy", group.LayoutPolicy.ToString())
                    .Attribute("data-group-applied-layout-policy", group.AppliedLayoutPolicy.ToString())
                    .Attribute("data-cfx-status", group.Status.ToString())
                    .Attribute("data-cfx-selected", selected);
                if (group.Longitude.HasValue) element.Attribute("data-group-longitude", F(group.Longitude.Value));
                if (group.Latitude.HasValue) element.Attribute("data-group-latitude", F(group.Latitude.Value));
                if (group.Metadata.TryGetValue("geoVisible", out var groupGeoVisible)) element.Attribute("data-group-geo-visible", groupGeoVisible);
                if (!string.IsNullOrWhiteSpace(group.Symbol)) element.Attribute("data-group-symbol", TrimTo(group.Symbol!.Trim(), 12));
                if (!string.IsNullOrWhiteSpace(group.Color)) element.Attribute("data-group-color", accent);
                AddTopologyDataAttributes(element, "data-cfx-meta-", group.Metadata, options.IncludeDataAttributes);
                if (highlight.IsActive && !highlighted) element.Attribute("opacity", highlight.DimmedOpacity);
            });

            if (options.IncludeTooltips && !string.IsNullOrWhiteSpace(group.Tooltip)) groupElement.Element("title", title => title.Text(group.Tooltip!));
            groupElement.Element("rect", rect => rect
                .Class(prefix + "__group-card")
                .Attribute("x", group.X)
                .Attribute("y", group.Y)
                .Attribute("width", group.Width)
                .Attribute("height", group.Height)
                .Attribute("rx", 12)
                .Attribute("fill", StatusFill(accent, theme.Background))
                .Attribute("stroke", accent)
                .Attribute("stroke-width", selected ? 2.4 : 1)
                .Attribute("stroke-opacity", selected ? 0.9 : 0.48));
            if (options.IncludeGroupLabels) {
                var cx = group.X + group.Width / 2;
                groupElement.Element("circle", circle => circle
                    .Attribute("cx", cx - 52)
                    .Attribute("cy", group.Y + 26)
                    .Attribute("r", 10)
                    .Attribute("fill", StatusFill(accent, theme.Background))
                    .Attribute("stroke", accent));
                AddGroupSymbol(groupElement, group, cx - 52, group.Y + 26, accent);
                groupElement.Element("text", text => text
                    .Attribute("x", cx)
                    .Attribute("y", group.Y + 30)
                    .Attribute("text-anchor", "middle")
                    .Attribute("fill", accent)
                    .Attribute("font-size", 16)
                    .Attribute("font-weight", "700")
                    .Text(group.Label));
                if (!string.IsNullOrWhiteSpace(group.Subtitle)) {
                    groupElement.Element("text", text => text
                        .Attribute("x", cx)
                        .Attribute("y", group.Y + 50)
                        .Attribute("text-anchor", "middle")
                        .Attribute("fill", theme.MutedForeground)
                        .Attribute("font-size", 12)
                        .Text(group.Subtitle!));
                }
            }
        }

        sb.Append(ElementMarkup(layer));
    }

    private static string GroupAccentColor(TopologyGroup group, TopologyTheme theme) => string.IsNullOrWhiteSpace(group.Color) ? theme.StatusColor(group.Status) : group.Color!.Trim();

    private static void AddGroupSymbol(SvgElement parent, TopologyGroup group, double cx, double cy, string color) {
        var symbol = string.IsNullOrWhiteSpace(group.Symbol) ? string.Empty : group.Symbol!.Trim();
        if (symbol.Equals("region", StringComparison.OrdinalIgnoreCase) || symbol.Equals("globe", StringComparison.OrdinalIgnoreCase)) {
            parent.Element("circle", circle => circle
                .Attribute("cx", cx)
                .Attribute("cy", cy)
                .Attribute("r", 5.8)
                .Attribute("fill", "none")
                .Attribute("stroke", color)
                .Attribute("stroke-width", 1.3));
            parent.Element("path", path => path
                .Attribute("d", "M " + F(cx - 5.2) + " " + F(cy) + " H " + F(cx + 5.2) + " M " + F(cx) + " " + F(cy - 5.8) + " C " + F(cx - 3.2) + " " + F(cy - 2.6) + " " + F(cx - 3.2) + " " + F(cy + 2.6) + " " + F(cx) + " " + F(cy + 5.8) + " M " + F(cx) + " " + F(cy - 5.8) + " C " + F(cx + 3.2) + " " + F(cy - 2.6) + " " + F(cx + 3.2) + " " + F(cy + 2.6) + " " + F(cx) + " " + F(cy + 5.8))
                .Attribute("fill", "none")
                .Attribute("stroke", color)
                .Attribute("stroke-width", 1.1)
                .Attribute("stroke-linecap", "round"));
            return;
        }

        if (string.IsNullOrWhiteSpace(symbol)) {
            parent.Element("circle", circle => circle
                .Attribute("cx", cx)
                .Attribute("cy", cy)
                .Attribute("r", 4)
                .Attribute("fill", "none")
                .Attribute("stroke", color));
            return;
        }

        parent.Element("text", text => text
            .Attribute("x", cx)
            .Attribute("y", cy + 3.2)
            .Attribute("text-anchor", "middle")
            .Attribute("fill", color)
            .Attribute("font-size", 8)
            .Attribute("font-weight", "800")
            .Text(TrimTo(symbol, 3)));
    }

    private static void DrawEdges(StringBuilder sb, TopologyChart chart, string prefix, TopologyTheme theme, TopologyRenderOptions options, string svgId, TopologyHighlightState highlight) {
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var layer = new SvgElement("g")
            .Class(prefix + "__edges")
            .Attribute("data-cfx-role", "topology-edges");
        foreach (var edge in chart.Edges) {
            var points = EdgePoints(chart, edge, nodes);
            var routeOffset = EdgeRouteOffset(chart, edge);
            var color = edge.IsMuted ? theme.Border : theme.StatusColor(edge.Status);
            var dash = edge.IsMuted ? "none" : EdgeDash(edge);
            var highlighted = highlight.IsEdgeHighlighted(edge);
            var selected = IsSelected(options.SelectedEdgeIds, edge.Id);
            var diagnostics = EdgeRouteDiagnostics(chart, edge, nodes);
            var isGeographicCurve = IsGeographicCurve(chart, edge, nodes);
            var curveControl = isGeographicCurve ? GeographicCurveControlPoint(chart, edge, nodes, points) : new ChartPoint(0, 0);
            var parent = AddOptionalLink(layer, edge.Href, prefix, options);
            var edgeGroup = parent.Element("g", group => {
                group
                    .Attribute("id", SafeElementId(chart.Id, "edge", edge.Id))
                    .Class(prefix + "__edge-wrap " + prefix + "__edge-wrap--" + CssToken(edge.Status.ToString()) + (edge.IsMuted ? " " + prefix + "__edge-wrap--muted" : string.Empty) + (selected ? " " + prefix + "--selected" : string.Empty) + highlight.CssClass(prefix, highlighted) + CustomCssClasses(edge.CssClass))
                    .Attribute("data-cfx-role", "topology-edge")
                    .Attribute("data-edge-id", edge.Id)
                    .Attribute("data-source-node-id", edge.SourceNodeId)
                    .Attribute("data-target-node-id", edge.TargetNodeId)
                    .Attribute("data-source-group-id", EdgeNodeGroupId(nodes, edge.SourceNodeId))
                    .Attribute("data-target-group-id", EdgeNodeGroupId(nodes, edge.TargetNodeId))
                    .Attribute("data-edge-kind", edge.Kind.ToString())
                    .Attribute("data-cfx-status", edge.Status.ToString())
                    .Attribute("data-cfx-selected", selected)
                    .Attribute("data-edge-muted", edge.IsMuted)
                    .Attribute("data-edge-line-style", edge.LineStyle.ToString())
                    .Attribute("data-edge-layout-inference", EdgeLayoutInferenceToken(edge.LayoutInference))
                    .Attribute("data-route-strategy", diagnostics.Strategy)
                    .Attribute("data-route-corridor", diagnostics.Corridor)
                    .Attribute("data-route-candidate-count", diagnostics.CandidateCount)
                    .Attribute("data-route-fallback-reason", diagnostics.FallbackReason)
                    .Attribute("data-route-segment-count", diagnostics.SegmentCount)
                    .Attribute("data-route-obstacle-hits", diagnostics.ObstacleHits)
                    .Attribute("data-route-label-obstacle-hits", diagnostics.LabelObstacleHits)
                    .Attribute("data-route-overlap-score", diagnostics.RouteOverlapScore)
                    .Attribute("data-route-offset", routeOffset)
                    .Attribute("data-route-curve", isGeographicCurve ? "geographic" : edge.Routing.ToString())
                    .Attribute("data-source-port", edge.SourcePort.ToString())
                    .Attribute("data-target-port", edge.TargetPort.ToString())
                    .Attribute("data-route-lane", edge.RouteLane)
                    .Attribute("data-waypoint-count", edge.Waypoints.Count);
                if (isGeographicCurve) {
                    group
                        .Attribute("data-route-control-x", curveControl.X)
                        .Attribute("data-route-control-y", curveControl.Y);
                }

                AddTopologyDataAttributes(group, "data-cfx-meta-", edge.Metadata, options.IncludeDataAttributes);
                AddTopologyDataAttributes(group, "data-cfx-metric-", edge.Metrics, options.IncludeDataAttributes);
                if (highlight.IsActive && !highlighted) group.Attribute("opacity", highlight.DimmedOpacity);
            });

            if (options.IncludeTooltips && !string.IsNullOrWhiteSpace(edge.Tooltip)) edgeGroup.Element("title", title => title.Text(edge.Tooltip!));
            edgeGroup.Element("path", path => {
                path
                    .Class(prefix + "__edge")
                    .Attribute("d", EdgePath(chart, edge, nodes, points))
                    .Attribute("stroke", color)
                    .Attribute("stroke-width", selected ? 3.4 : edge.IsMuted ? 1.45 : 2.2)
                    .Attribute("stroke-dasharray", dash)
                    .Attribute("opacity", edge.IsMuted ? 0.72 : 0.94);
                if (options.IncludeDirectionMarkers && edge.Direction is TopologyDirection.Backward or TopologyDirection.Bidirectional) path.Attribute("marker-start", "url(#" + svgId + "-arrow-" + StatusMarkerToken(edge.Status) + ")");
                if (options.IncludeDirectionMarkers && edge.Direction is TopologyDirection.Forward or TopologyDirection.Bidirectional) path.Attribute("marker-end", "url(#" + svgId + "-arrow-" + StatusMarkerToken(edge.Status) + ")");
            });
        }

        sb.Append(ElementMarkup(layer));
    }

    private static void DrawEdgeLabels(StringBuilder sb, TopologyChart chart, string prefix, TopologyTheme theme, TopologyRenderOptions options, TopologyHighlightState highlight) {
        if (!options.IncludeEdgeLabels) return;
        var layer = new SvgElement("g")
            .Class(prefix + "__edge-labels")
            .Attribute("data-cfx-role", "topology-edge-labels");
        foreach (var layout in EdgeLabelLayouts(chart, options)) {
            var edge = layout.Edge;
            var cx = layout.CenterX;
            var cy = layout.CenterY;
            var highlighted = highlight.IsEdgeHighlighted(edge);
            var selected = IsSelected(options.SelectedEdgeIds, edge.Id);
            layer.Element("g", group => {
                group
                    .Class(prefix + "__edge-label" + (selected ? " " + prefix + "--selected" : string.Empty) + highlight.CssClass(prefix, highlighted))
                    .Attribute("data-cfx-role", "topology-edge-label")
                    .Attribute("data-edge-id", edge.Id)
                    .Attribute("data-label-x", cx)
                    .Attribute("data-label-y", cy)
                    .Attribute("data-cfx-selected", selected);
                if (highlight.IsActive && !highlighted) group.Attribute("opacity", highlight.DimmedOpacity);
                if (options.IncludeEdgeLabelBackplates) {
                    group.Element("rect", rect => rect
                        .Attribute("x", cx - layout.Width / 2)
                        .Attribute("y", cy - layout.Height / 2)
                        .Attribute("width", layout.Width)
                        .Attribute("height", layout.Height)
                        .Attribute("rx", 9)
                        .Attribute("fill", theme.Background)
                        .Attribute("stroke", theme.Border));
                }

                AddEdgeLabelLines(group, layout, cx, cy, edge.IsMuted ? theme.MutedForeground : theme.StatusColor(edge.Status), theme.MutedForeground);
            });
        }

        sb.Append(ElementMarkup(layer));
    }

    private static void AddEdgeLabelLines(SvgElement group, TopologyEdgeLabelLayout layout, double cx, double cy, string primaryColor, string secondaryColor) {
        var lines = new List<(string Text, bool Primary)>();
        if (!string.IsNullOrWhiteSpace(layout.Label)) lines.Add((layout.Label, true));
        if (!string.IsNullOrWhiteSpace(layout.SecondaryLabel)) lines.Add((layout.SecondaryLabel, false));
        if (!string.IsNullOrWhiteSpace(layout.TertiaryLabel)) lines.Add((layout.TertiaryLabel, false));
        var start = cy - (lines.Count - 1) * 8;
        for (var i = 0; i < lines.Count; i++) {
            var line = lines[i];
            var size = line.Primary ? 12 : 10;
            var weight = line.Primary ? "700" : "500";
            var color = line.Primary ? primaryColor : secondaryColor;
            group.Element("text", text => text
                .Attribute("x", cx)
                .Attribute("y", start + i * 16 + (line.Primary ? 4 : 3))
                .Attribute("text-anchor", "middle")
                .Attribute("fill", color)
                .Attribute("font-size", size)
                .Attribute("font-weight", weight)
                .Text(line.Text));
        }
    }

    private static bool IsSelected(List<string> ids, string id) {
        foreach (var selectedId in ids) {
            if (string.Equals(selectedId, id, StringComparison.Ordinal)) return true;
        }

        return false;
    }

    private static string EdgePath(TopologyChart chart, TopologyEdge edge, IReadOnlyDictionary<string, TopologyNode> nodes, IReadOnlyList<ChartPoint> points) {
        if (IsGeographicCurve(chart, edge, nodes) && points.Count >= 2) {
            var control = GeographicCurveControlPoint(chart, edge, nodes, points);
            return new SvgPathDataBuilder(64)
                .MoveTo(points[0])
                .QuadraticTo(control.X, control.Y, points[points.Count - 1].X, points[points.Count - 1].Y)
                .Build();
        }

        return EdgePath(points, edge.Routing);
    }

    private static string EdgePath(IReadOnlyList<ChartPoint> points, TopologyEdgeRouting routing) {
        var x1 = points[0].X;
        var y1 = points[0].Y;
        var x2 = points[points.Count - 1].X;
        var y2 = points[points.Count - 1].Y;
        if (routing == TopologyEdgeRouting.Straight) return "M " + F(x1) + " " + F(y1) + " L " + F(x2) + " " + F(y2);
        if (routing == TopologyEdgeRouting.Curved && points.Count == 2) {
            var lift = Math.Max(40, Math.Abs(x2 - x1) * 0.12);
            return "M " + F(x1) + " " + F(y1) + " C " + F(x1) + " " + F(y1 - lift) + " " + F(x2) + " " + F(y2 - lift) + " " + F(x2) + " " + F(y2);
        }

        var path = new SvgPathDataBuilder(points.Count * 16).MoveTo(points[0]);
        for (var i = 1; i < points.Count; i++) path.LineTo(points[i]);
        return path.Build();
    }

    private static string? EdgeNodeGroupId(IReadOnlyDictionary<string, TopologyNode> nodes, string nodeId) {
        return nodes.TryGetValue(nodeId, out var node) && !string.IsNullOrWhiteSpace(node.GroupId) ? node.GroupId : null;
    }

    private static string EdgeLayoutInferenceToken(TopologyEdgeLayoutInference inference) {
        if (inference == TopologyEdgeLayoutInference.None) return "none";
        var tokens = new List<string>();
        if ((inference & TopologyEdgeLayoutInference.SourcePort) != 0) tokens.Add("source-port");
        if ((inference & TopologyEdgeLayoutInference.TargetPort) != 0) tokens.Add("target-port");
        if ((inference & TopologyEdgeLayoutInference.RouteLane) != 0) tokens.Add("route-lane");
        return string.Join(" ", tokens);
    }

    private static SvgElement AddOptionalLink(SvgElement parent, string? href, string prefix, TopologyRenderOptions options) {
        var safe = SafeHref(href);
        if (safe == null) return parent;
        return parent.Element("a", link => {
            link
                .Class(prefix + "__link")
                .Attribute("href", safe);
            if (!options.OpenLinksInNewTab) return;
            link
                .Attribute("target", "_blank")
                .Attribute("rel", "noopener noreferrer");
        });
    }

    private static void AddTopologyDataAttributes(SvgElement element, string prefix, IReadOnlyDictionary<string, string> values, bool include) {
        if (!include) return;
        foreach (var item in values.OrderBy(item => item.Key, StringComparer.Ordinal)) {
            var key = SanitizeDataAttributeKey(item.Key);
            if (string.IsNullOrWhiteSpace(key)) continue;
            element.Attribute(prefix + key, item.Value ?? string.Empty);
        }
    }

    private static string SanitizeDataAttributeKey(string value) {
        var sb = new StringBuilder(value.Length);
        foreach (var ch in value.Trim()) {
            if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_') sb.Append(ch);
            else sb.Append('-');
        }

        return sb.ToString().Trim('-').ToLowerInvariant();
    }

    private static string ElementMarkup(SvgElement element) {
        var writer = new SvgMarkupWriter();
        element.WriteTo(writer);
        return writer.Build();
    }

    private static string BuildDescription(TopologyChart chart) {
        return (string.IsNullOrWhiteSpace(chart.Title) ? "Topology chart" : chart.Title) + " with " + chart.Groups.Count.ToString(CultureInfo.InvariantCulture) + " groups, " + chart.Nodes.Count.ToString(CultureInfo.InvariantCulture) + " nodes, and " + chart.Edges.Count.ToString(CultureInfo.InvariantCulture) + " edges.";
    }

    private static string CssToken(string value) => value.ToLowerInvariant();
}
