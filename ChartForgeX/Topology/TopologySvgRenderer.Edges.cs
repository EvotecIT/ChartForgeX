using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;
using ChartForgeX.Themes;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

public sealed partial class TopologySvgRenderer {
    private static void AddEdges(SvgElement root, TopologyChart chart, string prefix, TopologyTheme theme, TopologyRenderOptions options, string svgId, TopologyHighlightState highlight, bool includeRouteDiagnostics = true) {
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var layer = new SvgElement("g")
            .Class(prefix + "__edges")
            .Attribute("data-cfx-role", "topology-edges");
        var markerKeys = new TopologySvgMarkerKeys(chart);
        foreach (var (edge, renderOrder) in OrderedEdgesForRendering(chart, options)) {
            var points = EdgePoints(chart, edge, nodes);
            var paintedPoints = TopologyDenseRoutePlanner.PaintPoints(chart, edge, points, includeOwner: true);
            var trunk = TopologyDenseRoutePlanner.SharedTrunk(chart, edge);
            var paintEdge = edge;
            if (trunk.Owner != null) {
                paintEdge = TopologyLayoutEngine.Clone(edge);
                paintEdge.TargetMarker = TopologyMarkerKind.None;
            }
            var routeOffset = EdgeRouteOffset(chart, edge);
            var color = EdgeColor(edge, theme, options);
            var dash = EffectiveEdgeDash(edge);
            var highlighted = highlight.IsEdgeHighlighted(edge);
            var selected = IsSelected(options.SelectedEdgeIds, edge.Id);
            var isGeographicCurve = IsGeographicCurve(chart, edge, nodes);
            var curveControl = isGeographicCurve ? GeographicCurveControlPoint(chart, edge, nodes, points) : new ChartPoint(0, 0);
            var parent = AddOptionalLink(layer, edge.Href, prefix, options);
            var edgeGroup = parent.Element("g", group => {
                group
                    .Attribute("id", TopologySvgIds.Element(chart, options, "edge", edge.Id))
                    .Class(prefix + "__edge-wrap " + prefix + "__edge-wrap--" + CssToken(edge.Status.ToString()) + (edge.IsMuted ? " " + prefix + "__edge-wrap--muted" : string.Empty) + (selected ? " " + prefix + "--selected" : string.Empty) + highlight.CssClass(prefix, highlighted) + CustomCssClasses(edge.CssClass))
                    .Attribute("data-cfx-role", "topology-edge")
                    .Attribute("data-edge-id", edge.Id)
                    .Attribute("data-trunk-owner-id", trunk.Owner?.Id)
                    .Attribute("data-edge-label", edge.Label)
                    .Attribute("data-edge-secondary-label", edge.SecondaryLabel)
                    .Attribute("data-edge-tertiary-label", edge.TertiaryLabel)
                    .Attribute("data-source-node-id", edge.SourceNodeId)
                    .Attribute("data-target-node-id", edge.TargetNodeId)
                    .Attribute("data-source-group-id", EdgeNodeGroupId(nodes, edge.SourceNodeId))
                    .Attribute("data-target-group-id", EdgeNodeGroupId(nodes, edge.TargetNodeId))
                    .Attribute("data-edge-kind", edge.Kind.ToString())
                    .Attribute("data-cfx-status", edge.Status.ToString())
                    .Attribute("data-cfx-selected", selected)
                    .Attribute("data-edge-muted", edge.IsMuted)
                    .Attribute("data-edge-color", string.IsNullOrWhiteSpace(edge.Color) ? null : color)
                    .Attribute("data-edge-line-style", edge.LineStyle.ToString())
                    .Attribute("data-edge-emphasis", edge.Emphasis.ToString())
                    .Attribute("data-edge-stroke-width", edge.StrokeWidth.HasValue ? F(edge.StrokeWidth.Value) : null)
                    .Attribute("data-edge-opacity", edge.Opacity.HasValue ? F(edge.Opacity.Value) : null)
                    .Attribute("data-edge-dash-pattern", edge.DashPattern.Count == 0 ? null : EdgeDash(edge))
                    .Attribute("data-source-marker", EffectiveSourceMarker(edge).ToString())
                    .Attribute("data-target-marker", EffectiveTargetMarker(edge).ToString())
                    .Attribute("data-edge-preferred-length", edge.PreferredLength.HasValue ? F(edge.PreferredLength.Value) : null)
                    .Attribute("data-edge-minimum-rank-span", edge.MinimumRankSpan)
                    .Attribute("data-edge-routing-priority", edge.RoutingPriority)
                    .Attribute("data-source-label", edge.SourceLabel)
                    .Attribute("data-target-label", edge.TargetLabel)
                    .Attribute("data-edge-render-order", renderOrder)
                    .Attribute("data-edge-layout-inference", EdgeLayoutInferenceToken(edge.LayoutInference))
                    .Attribute("data-route-offset", routeOffset)
                    .Attribute("data-route-start-x", points[0].X)
                    .Attribute("data-route-start-y", points[0].Y)
                    .Attribute("data-route-end-x", points[points.Count - 1].X)
                    .Attribute("data-route-end-y", points[points.Count - 1].Y)
                    .Attribute("data-route-curve", isGeographicCurve ? "geographic" : edge.Routing.ToString())
                    .Attribute("data-source-port", edge.SourcePort.ToString())
                    .Attribute("data-target-port", edge.TargetPort.ToString())
                    .Attribute("data-source-port-id", edge.SourcePortId)
                    .Attribute("data-target-port-id", edge.TargetPortId)
                    .Attribute("data-route-lane", edge.RouteLane)
                    .Attribute("data-label-offset-x", edge.LabelOffsetX)
                    .Attribute("data-label-offset-y", edge.LabelOffsetY)
                    .Attribute("data-waypoint-count", edge.Waypoints.Count);
                if (includeRouteDiagnostics) {
                    var diagnostics = EdgeRouteDiagnostics(chart, edge, nodes);
                    group.Attribute("data-route-strategy", diagnostics.Strategy).Attribute("data-route-corridor", diagnostics.Corridor)
                        .Attribute("data-route-candidate-count", diagnostics.CandidateCount).Attribute("data-route-fallback-reason", diagnostics.FallbackReason)
                        .Attribute("data-route-segment-count", diagnostics.SegmentCount).Attribute("data-route-obstacle-count", diagnostics.ObstacleCount)
                        .Attribute("data-route-obstacle-hits", diagnostics.ObstacleHits).Attribute("data-route-label-obstacle-hits", diagnostics.LabelObstacleHits)
                        .Attribute("data-route-overlap-score", diagnostics.RouteOverlapScore);
                }
                AddScenarioDataAttributes(group, chart, TopologyScenarioStepKind.Edge, edge.Id);
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
            AddEdgeHalo(edgeGroup, chart, edge, nodes, paintedPoints, prefix, theme, options, selected);
            if (TopologyDenseRoutePlanner.IsTrunkBranch(chart, edge)) edgeGroup.Element("path", path => path
                .Attribute("data-cfx-role", "topology-shared-trunk-hit")
                .Attribute("d", EdgePath(chart, edge, nodes, points, options))
                .Attribute("fill", "none").Attribute("stroke", "transparent")
                .Attribute("stroke-width", Math.Max(8, EdgeStrokeWidth(edge, selected, options)))
                .Attribute("pointer-events", "stroke"));
            AddPremiumEdgePath(edgeGroup, chart, paintEdge, nodes, paintedPoints, prefix, options, svgId, selected, color, dash, markerKeys.Key(edge));
            if (ReferenceEquals(trunk.Owner, edge) && trunk.Tail != null) {
                var tailGroup = edgeGroup.Element("g", group => group.Attribute("data-cfx-role", "topology-shared-trunk-tail")
                    .Attribute("data-trunk-owner-id", edge.Id));
                var tailEdge = TopologyLayoutEngine.Clone(edge);
                tailEdge.SourceMarker = TopologyMarkerKind.None;
                AddEdgeHalo(tailGroup, chart, edge, nodes, trunk.Tail, prefix, theme, options, selected);
                AddPremiumEdgePath(tailGroup, chart, tailEdge, nodes, trunk.Tail, prefix, options, svgId, selected, color, dash, markerKeys.Key(edge));
            }
        }

        root.AddElement(layer);
    }

    private static void AddEdgeHalo(SvgElement edgeGroup, TopologyChart chart, TopologyEdge edge,
        IReadOnlyDictionary<string, TopologyNode> nodes, List<ChartPoint> paintedPoints, string prefix, TopologyTheme theme,
        TopologyRenderOptions options, bool selected) {
        if (ShouldRenderMonitoringRouteHalo(chart, edge, nodes, options)) {
            var geographicHalo = ShouldRenderGeographicRouteHalo(chart, edge, nodes, options);
            edgeGroup.Element("path", path => path
                .Class(prefix + "__edge-halo")
                .Attribute("data-cfx-role", geographicHalo ? "topology-geographic-route-halo" : "topology-edge-route-halo")
                .Attribute("d", EdgePath(chart, edge, nodes, paintedPoints, options))
                .Attribute("fill", "none")
                .Attribute("stroke", theme.Background)
                .Attribute("stroke-width", EdgeStrokeWidth(edge, selected, options) + RouteHaloStrokeExtra(geographicHalo))
                .Attribute("stroke-linecap", "round")
                .Attribute("stroke-linejoin", "round")
                .Attribute("opacity", RouteHaloOpacity(geographicHalo) * EdgeOpacity(edge, options)));
        }
    }

}
