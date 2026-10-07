using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private Dictionary<string, string> RootMetadata() => new(StringComparer.Ordinal) {
        ["class"] = CssPrefix,
        ["data-chart-id"] = _source.Id ?? "topology", ["data-layout-mode"] = _chart.LayoutMode.ToString(),
        ["data-layout-direction"] = _chart.LayoutDirection.ToString(), ["data-visual-style"] = _options.VisualStyle.ToString(),
        ["data-cfx-scenario-count"] = _chart.Scenarios.Count.ToString(CultureInfo.InvariantCulture),
        ["data-cfx-scenarios"] = TopologyScenarioJson.Summaries(_chart) ?? "[]", ["data-cfx-scenario-ids"] = TopologyScenarioJson.ScenarioIds(_chart) ?? string.Empty,
        ["data-cfx-active-scenario"] = _highlight.ActiveScenarioId ?? string.Empty
    };

    private Dictionary<string, string> NodeMetadata(TopologyNode node) {
        var data = CommonMetadata(node.Id, "node", node.Status, TopologyScenarioStepKind.Node, node.Metadata, node.Metrics);
        data["class"] = CssPrefix + "__node" + CustomCssClasses(node.CssClass);
        data["data-node-kind"] = node.Kind.ToString(); data["data-node-label"] = node.Label;
        data["data-node-display-mode"] = EffectiveNodeDisplayMode(node, _options).ToString();
        data["data-group-id"] = node.GroupId ?? string.Empty;
        data["data-group-label"] = _chart.Groups.FirstOrDefault(group => group.Id == node.GroupId)?.Label ?? string.Empty;
        data["data-node-badge"] = node.Badge ?? string.Empty; data["data-node-detail-count"] = node.Details.Count.ToString(CultureInfo.InvariantCulture);
        data["data-cfx-selected"] = _options.SelectedNodeIds.Contains(node.Id) ? "true" : "false";
        Coordinates(data, "node", node.Longitude, node.Latitude);
        Icon(data, "node", node.IconId, ResolveNodeIcon(node, _options));
        if (node.Metadata.TryGetValue("geoVisible", out var visible)) data["data-node-geo-visible"] = visible;
        var artwork = ResolveRenderableNodeArtwork(node, _options);
        if (artwork != null) {
            data["data-node-icon-artwork"] = ArtworkKind(artwork);
            data["data-node-artwork-source"] = ResolveNodeArtworkSource(node, _options) ?? string.Empty;
        }
        if (!string.IsNullOrWhiteSpace(node.Color)) data["data-node-color"] = node.Color!;
        if (!string.IsNullOrWhiteSpace(node.BackgroundColor)) data["data-node-background-color"] = node.BackgroundColor!;
        return data;
    }

    private Dictionary<string, string> GroupMetadata(TopologyGroup group) {
        var data = CommonMetadata(group.Id, "group", group.Status, null, group.Metadata, null);
        data["class"] = CssPrefix + "__group" + CustomCssClasses(group.CssClass);
        data["data-group-label"] = group.Label; data["data-group-color"] = group.Color ?? string.Empty;
        data["data-group-layout-policy"] = group.LayoutPolicy.ToString(); data["data-group-symbol"] = group.Symbol ?? string.Empty;
        data["data-cfx-selected"] = _options.SelectedGroupIds.Contains(group.Id) ? "true" : "false";
        Coordinates(data, "group", group.Longitude, group.Latitude); Icon(data, "group", group.IconId, ResolveGroupIcon(group, _options));
        data["data-group-applied-layout-policy"] = group.AppliedLayoutPolicy.ToString();
        if (group.Metadata.TryGetValue("geoVisible", out var visible)) data["data-group-geo-visible"] = visible;
        return data;
    }

    private Dictionary<string, string> EdgeMetadata(TopologyEdge edge) {
        if (_edgeMetadata.TryGetValue(edge.Id, out var cached)) return cached;
        var data = CommonMetadata(edge.Id, "edge", edge.Status, TopologyScenarioStepKind.Edge, edge.Metadata, edge.Metrics);
        data["class"] = CssPrefix + "__edge" + CustomCssClasses(edge.CssClass);
        data["data-source-node-id"] = edge.SourceNodeId; data["data-target-node-id"] = edge.TargetNodeId;
        data["data-source-group-id"] = _nodesById[edge.SourceNodeId].GroupId ?? string.Empty;
        data["data-target-group-id"] = _nodesById[edge.TargetNodeId].GroupId ?? string.Empty;
        data["data-edge-label"] = edge.Label ?? string.Empty; data["data-edge-secondary-label"] = edge.SecondaryLabel ?? string.Empty;
        data["data-edge-tertiary-label"] = edge.TertiaryLabel ?? string.Empty; data["data-edge-kind"] = edge.Kind.ToString();
        data["data-edge-line-style"] = edge.LineStyle.ToString(); data["data-edge-muted"] = edge.IsMuted ? "true" : "false";
        data["data-direction"] = edge.Direction.ToString();
        data["data-edge-emphasis"] = edge.Emphasis.ToString(); data["data-edge-layout-inference"] = edge.LayoutInference.ToString();
        data["data-edge-color"] = edge.Color ?? string.Empty;
        if (edge.StrokeWidth.HasValue) data["data-edge-stroke-width"] = Number(edge.StrokeWidth.Value * _scale);
        if (edge.Opacity.HasValue) data["data-edge-opacity"] = Number(edge.Opacity.Value);
        data["data-edge-dash-pattern"] = string.Join(" ", edge.DashPattern.Select(length => Number(length * _scale)));
        data["data-source-marker"] = EffectiveSourceMarker(edge).ToString(); data["data-target-marker"] = EffectiveTargetMarker(edge).ToString();
        data["data-source-label"] = edge.SourceLabel ?? string.Empty; data["data-target-label"] = edge.TargetLabel ?? string.Empty;
        data["data-edge-render-order"] = _edgeRenderOrders[edge].ToString(CultureInfo.InvariantCulture);
        if (edge.PreferredLength.HasValue) data["data-edge-preferred-length"] = Number(edge.PreferredLength.Value);
        data["data-edge-minimum-rank-span"] = edge.MinimumRankSpan.ToString(CultureInfo.InvariantCulture);
        data["data-edge-routing-priority"] = edge.RoutingPriority.ToString(CultureInfo.InvariantCulture);
        data["data-source-port"] = edge.SourcePort.ToString(); data["data-target-port"] = edge.TargetPort.ToString();
        data["data-source-port-id"] = edge.SourcePortId ?? string.Empty; data["data-target-port-id"] = edge.TargetPortId ?? string.Empty;
        data["data-cfx-selected"] = _options.SelectedEdgeIds.Contains(edge.Id) ? "true" : "false";
        data["data-waypoint-count"] = edge.Waypoints.Count.ToString(CultureInfo.InvariantCulture);
        data["data-route-lane"] = edge.RouteLane.ToString("R", CultureInfo.InvariantCulture); data["data-route-curve"] = edge.Routing.ToString();
        data["data-route-offset"] = Number(EdgeRouteOffset(_chart, edge) * _scale);
        data["data-label-offset-x"] = Number(edge.LabelOffsetX * _scale); data["data-label-offset-y"] = Number(edge.LabelOffsetY * _scale);
        if (_trunks.TryGetValue(edge.Id, out var trunk)) data["data-trunk-owner-id"] = trunk.Owner;
        var points = _routes[edge.Id]; var first = Point(points[0]); var last = Point(points[points.Count - 1]);
        data["data-route-start-x"] = first.X.ToString("R", CultureInfo.InvariantCulture); data["data-route-start-y"] = first.Y.ToString("R", CultureInfo.InvariantCulture);
        data["data-route-end-x"] = last.X.ToString("R", CultureInfo.InvariantCulture); data["data-route-end-y"] = last.Y.ToString("R", CultureInfo.InvariantCulture);
        if (IsGeographicCurve(_chart, edge, _nodesById)) {
            data["data-route-curve"] = "geographic";
            var control = Point(GeographicCurveControlPoint(_chart, edge, _nodesById, EdgePoints(_chart, edge, _nodesById)));
            data["data-route-control-x"] = Number(control.X); data["data-route-control-y"] = Number(control.Y);
        }
        var diagnostic = EdgeRouteDiagnostics(_chart, edge, _nodesById);
        data["data-route-strategy"] = diagnostic.Strategy; data["data-route-corridor"] = diagnostic.Corridor;
        data["data-route-candidate-count"] = diagnostic.CandidateCount.ToString(CultureInfo.InvariantCulture);
        data["data-route-fallback-reason"] = diagnostic.FallbackReason;
        data["data-route-segment-count"] = diagnostic.SegmentCount.ToString(CultureInfo.InvariantCulture);
        data["data-route-obstacle-count"] = diagnostic.ObstacleCount.ToString(CultureInfo.InvariantCulture);
        data["data-route-obstacle-hits"] = diagnostic.ObstacleHits.ToString(CultureInfo.InvariantCulture);
        data["data-route-label-obstacle-hits"] = diagnostic.LabelObstacleHits.ToString(CultureInfo.InvariantCulture);
        data["data-route-overlap-score"] = diagnostic.RouteOverlapScore.ToString("R", CultureInfo.InvariantCulture);
        _edgeMetadata.Add(edge.Id, data);
        return data;
    }

    private Dictionary<string, string> CalloutMetadata(TopologyGeographicCallout callout) {
        var data = GroupMetadata(callout.Group);
        var anchor = Point(new ChartForgeX.Primitives.ChartPoint(callout.AnchorX, callout.AnchorY));
        data["data-cfx-visual-role"] = "topology-geographic-callout";
        data["data-callout-placement"] = callout.Placement;
        data["data-callout-anchor-x"] = anchor.X.ToString("R", CultureInfo.InvariantCulture);
        data["data-callout-anchor-y"] = anchor.Y.ToString("R", CultureInfo.InvariantCulture);
        data["data-callout-node-count"] = callout.NodeCount.ToString(CultureInfo.InvariantCulture);
        data["data-callout-healthy-count"] = callout.HealthyCount.ToString(CultureInfo.InvariantCulture);
        data["data-callout-warning-count"] = callout.WarningCount.ToString(CultureInfo.InvariantCulture);
        data["data-callout-critical-count"] = callout.CriticalCount.ToString(CultureInfo.InvariantCulture);
        data["data-callout-unknown-count"] = callout.UnknownCount.ToString(CultureInfo.InvariantCulture);
        data["data-callout-disabled-count"] = callout.DisabledCount.ToString(CultureInfo.InvariantCulture);
        return data;
    }

    private Dictionary<string, string> CommonMetadata(string id, string kind, TopologyHealthStatus status, TopologyScenarioStepKind? scenarioKind,
        IDictionary<string, string> metadata, IDictionary<string, string>? metrics) {
        var data = new Dictionary<string, string>(StringComparer.Ordinal) { ["data-" + kind + "-id"] = id, ["data-cfx-status"] = status.ToString() };
        var memberships = _chart.Scenarios.Select(scenario => new { scenario.Id, Indices = scenario.Steps.Select((step, index) => new { step, index })
            .Where(entry => entry.step.Kind == scenarioKind && entry.step.Id == id || scenarioKind == TopologyScenarioStepKind.Node && entry.step.Kind == TopologyScenarioStepKind.Edge
                && _chart.Edges.Any(edge => edge.Id == entry.step.Id && (edge.SourceNodeId == id || edge.TargetNodeId == id)))
            .Select(entry => entry.index).ToArray() }).Where(entry => entry.Indices.Length > 0).ToArray();
        data["data-scenario-ids"] = string.Join(" ", memberships.Select(entry => entry.Id));
        data["data-scenario-step-indices"] = string.Join(" ", memberships.Select(entry => entry.Id + ":" + string.Join(",", entry.Indices)));
        if (_options.IncludeDataAttributes) {
            foreach (var entry in metadata) data["data-cfx-meta-" + DataToken(entry.Key)] = entry.Value;
            if (metrics != null) foreach (var entry in metrics) data["data-cfx-metric-" + DataToken(entry.Key)] = entry.Value;
        }
        return data;
    }

    private static string DataToken(string value) => new(value.Select(character => char.IsLetterOrDigit(character) || character == '-' || character == '_' ? character : '-').ToArray());
    private string CssPrefix => NormalizeCssClassPrefix(_options.CssClassPrefix, "cfx-topology");
    private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    private static string ArtworkKind(TopologyIconArtwork artwork) => artwork.HasSvgBody || artwork.HasSvgPath ? "svg" : artwork.HasImageHref || artwork.HasPreviewPath ? "image" : "empty";

    private static void Coordinates(Dictionary<string, string> data, string kind, double? longitude, double? latitude) {
        if (longitude.HasValue) data["data-" + kind + "-longitude"] = longitude.Value.ToString("R", CultureInfo.InvariantCulture);
        if (latitude.HasValue) data["data-" + kind + "-latitude"] = latitude.Value.ToString("R", CultureInfo.InvariantCulture);
    }

    private static void Icon(Dictionary<string, string> data, string kind, string? id, TopologyIconDefinition? icon) {
        if (id != null) data["data-" + kind + "-icon-id"] = id;
        if (icon == null) return;
        data["data-" + kind + "-icon-pack"] = icon.PackId; data["data-" + kind + "-icon-label"] = icon.Label;
        data["data-" + kind + "-icon-shape"] = icon.Shape.ToString();
        if (icon.Artwork != null) data["data-" + kind + "-icon-artwork"] = ArtworkKind(icon.Artwork);
    }
}
