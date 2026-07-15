using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Interactivity;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;

namespace ChartForgeX.Interactivity.Html;

/// <summary>
/// Bridges static topology models into the reusable graph explorer scene contract.
/// </summary>
public static class TopologyGraphExplorerExtensions {
    /// <summary>
    /// Projects a topology chart into a host-neutral graph scene for large interactive exploration.
    /// </summary>
    /// <param name="chart">The source topology chart.</param>
    /// <param name="configure">Optional bridge configuration callback.</param>
    /// <returns>A graph scene that preserves topology ids, metadata, groups, and relationships.</returns>
    public static GraphScene ToGraphScene(this TopologyChart chart, Action<TopologyGraphSceneOptions>? configure = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        var options = new TopologyGraphSceneOptions();
        configure?.Invoke(options);

        var ids = TopologyGraphIdMap.Create(chart);
        var iconCatalog = options.IconCatalog ?? TopologyIconCatalog.Default();
        var scene = GraphScene.Create(ids.ChartId, TopologySceneTitle(chart));
        scene.Subtitle = chart.Subtitle;
        if (options.UseSuperTopologyDefaults) scene.Options.UseSuperTopologyDefaults(options.EnableManipulation);
        ApplyManipulationOptions(scene, options);
        scene.Options.Cluster.Mode = options.IncludeGroupsAsClusters ? GraphClusterMode.Hybrid : GraphClusterMode.Explicit;
        if (!options.IncludeGroupsAsClusters) scene.Options.Cluster.Adaptive = false;
        scene.Options.Cluster.CollapseOnLoad = scene.Options.LevelOfDetail.CollapseClustersOnLoad;
        scene.Metadata["source.model"] = nameof(TopologyChart);
        AddMetadata(scene.Metadata, "topology.id", chart.Id);
        scene.Metadata["topology.layout"] = chart.LayoutMode.ToString();
        scene.Metadata["topology.direction"] = chart.LayoutDirection.ToString();
        scene.Metadata["topology.nodeCount"] = chart.Nodes.Count.ToString(CultureInfo.InvariantCulture);
        scene.Metadata["topology.edgeCount"] = chart.Edges.Count.ToString(CultureInfo.InvariantCulture);
        scene.Metadata["topology.groupCount"] = chart.Groups.Count.ToString(CultureInfo.InvariantCulture);

        var groupIds = new HashSet<string>(chart.Groups.Select(group => ids.GroupId(group.Id)), StringComparer.Ordinal);
        foreach (var node in chart.Nodes) {
            scene.Nodes.Add(ToGraphNode(chart, node, groupIds, options, ids, iconCatalog));
        }

        if (scene.Nodes.Any(node => !string.IsNullOrWhiteSpace(node.ParentId))) scene.Options.Enable(GraphSceneFeatures.HierarchyNavigation);

        var topologyNodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        for (var index = 0; index < chart.Edges.Count; index++) {
            scene.Edges.Add(ToGraphEdge(chart, chart.Edges[index], ids, topologyNodes, options, index));
        }

        if (options.IncludeGroupsAsClusters) {
            foreach (var group in chart.Groups) {
                var groupId = ids.GroupId(group.Id);
                var memberIds = chart.Nodes.Where(node => string.Equals(node.GroupId, group.Id, StringComparison.Ordinal)).Select(node => ids.NodeId(node.Id)).ToArray();
                if (memberIds.Length == 0) continue;
                var cluster = new GraphSceneCluster {
                    Id = groupId,
                    Label = group.Label,
                    Kind = "topology-group"
                };
                cluster.NodeIds.AddRange(memberIds);
                AddMetadata(cluster.Metadata, "topology.id", group.Id);
                cluster.Metadata["topology.status"] = group.Status.ToString();
                cluster.Metadata["topology.layoutPolicy"] = group.LayoutPolicy.ToString();
                AddMetadata(cluster.Metadata, "topology.subtitle", group.Subtitle);
                AddMetadata(cluster.Metadata, "topology.iconId", group.IconId);
                AddMetadata(cluster.Metadata, "topology.symbol", group.Symbol);
                AddMetadata(cluster.Metadata, "topology.color", group.Color);
                CopyMetadata(group.Metadata, cluster.Metadata, "topology.meta.");
                scene.Clusters.Add(cluster);
            }
        }

        return scene;
    }

    /// <summary>
    /// Renders a topology chart through the graph explorer adapter by first projecting it into <see cref="GraphScene"/>.
    /// </summary>
    /// <param name="chart">The source topology chart.</param>
    /// <param name="configureScene">Optional topology-to-graph projection configuration.</param>
    /// <param name="configureHtml">Optional graph explorer HTML configuration.</param>
    /// <returns>A complete self-contained HTML document.</returns>
    public static string ToGraphExplorerHtmlPage(this TopologyChart chart, Action<TopologyGraphSceneOptions>? configureScene = null, Action<HtmlGraphExplorerOptions>? configureHtml = null) {
        return chart.ToGraphScene(configureScene).ToGraphExplorerHtmlPage(configureHtml);
    }

    /// <summary>
    /// Renders a topology chart as an embeddable graph explorer fragment by first projecting it into <see cref="GraphScene"/>.
    /// </summary>
    /// <param name="chart">The source topology chart.</param>
    /// <param name="configureScene">Optional topology-to-graph projection configuration.</param>
    /// <param name="configureHtml">Optional graph explorer HTML configuration.</param>
    /// <returns>A self-contained HTML fragment.</returns>
    public static string ToGraphExplorerHtmlFragment(this TopologyChart chart, Action<TopologyGraphSceneOptions>? configureScene = null, Action<HtmlGraphExplorerOptions>? configureHtml = null) {
        return chart.ToGraphScene(configureScene).ToGraphExplorerHtmlFragment(configureHtml);
    }

    private static GraphSceneNode ToGraphNode(TopologyChart chart, TopologyNode node, ISet<string> groupIds, TopologyGraphSceneOptions options, TopologyGraphIdMap ids, TopologyIconCatalog iconCatalog) {
        var groupId = string.IsNullOrWhiteSpace(node.GroupId) ? null : ids.GroupId(node.GroupId!);
        var icon = string.IsNullOrWhiteSpace(node.IconId) ? null : iconCatalog.Resolve(node.IconId);
        var artwork = ResolveArtwork(node, icon, options);
        var imageUrl = ArtworkImageUrl(artwork);
        var graphNode = new GraphSceneNode {
            Id = ids.NodeId(node.Id),
            Label = node.Label,
            SecondaryLabel = node.Subtitle,
            BadgeText = node.Badge,
            Kind = Token(node.Kind),
            GroupId = groupId,
            Status = Token(node.Status),
            Shape = node.DisplayMode == TopologyNodeDisplayMode.Hidden ? GraphNodeShape.Text : NodeShape(node, icon, imageUrl),
            Size = NodeSize(node),
            IconText = FirstText(node.Symbol, icon?.Symbol),
            ImageUrl = imageUrl,
            ImageAlt = node.Label,
            Fixed = ShouldPreserveCoordinates(chart, node, options),
            Hidden = node.DisplayMode == TopologyNodeDisplayMode.Hidden
        };
        ApplyHierarchy(node, graphNode, options, ids);
        if (options.IncludeGroupsAsClusters && options.UseGroupsAsClusterIds && !string.IsNullOrWhiteSpace(groupId) && groupIds.Contains(groupId!)) graphNode.ClusterId = groupId;
        if (ShouldPreserveCoordinates(chart, node, options)) {
            graphNode.X = node.X + node.Width / 2;
            graphNode.Y = node.Y + node.Height / 2;
        }

        var theme = chart.Theme ?? TopologyTheme.Light();
        var accentColor = FirstText(node.Color, icon?.Color, theme.StatusColor(node.Status))!;
        graphNode.Style.BackgroundColor = TopologyRenderPrimitives.NodeFill(node, theme, accentColor, new TopologyRenderOptions());
        graphNode.Style.BorderColor = accentColor;
        graphNode.Style.LabelColor = accentColor;
        graphNode.Style.Shadow = node.DisplayMode is TopologyNodeDisplayMode.Card or TopologyNodeDisplayMode.Artwork;

        AddMetadata(graphNode.Metadata, "topology.id", node.Id);
        AddMetadata(graphNode.Metadata, "topology.groupId", node.GroupId);
        AddMetadata(graphNode.Metadata, "topology.subtitle", node.Subtitle);
        AddMetadata(graphNode.Metadata, "topology.kind", node.Kind.ToString());
        AddMetadata(graphNode.Metadata, "topology.displayMode", node.DisplayMode?.ToString());
        AddMetadata(graphNode.Metadata, "topology.iconId", node.IconId);
        AddMetadata(graphNode.Metadata, "topology.iconQualifiedId", icon?.QualifiedId);
        AddMetadata(graphNode.Metadata, "topology.iconCategory", icon?.Category);
        AddMetadata(graphNode.Metadata, "topology.badge", node.Badge);
        AddMetadata(graphNode.Metadata, "topology.href", node.Href);
        AddMetadata(graphNode.Metadata, "topology.tooltip", node.Tooltip);
        AddMetadata(graphNode.Metadata, "topology.color", node.Color);
        AddMetadata(graphNode.Metadata, "topology.backgroundColor", node.BackgroundColor);
        CopyMetadata(node.Metadata, graphNode.Metadata, "topology.meta.");
        CopyMetadata(node.Metrics, graphNode.Metadata, "topology.metric.");
        return graphNode;
    }

    private static GraphSceneEdge ToGraphEdge(TopologyChart chart, TopologyEdge edge, TopologyGraphIdMap ids, IReadOnlyDictionary<string, TopologyNode> topologyNodes, TopologyGraphSceneOptions options, int index) {
        var sourceNodeId = ids.NodeId(edge.SourceNodeId);
        var targetNodeId = ids.NodeId(edge.TargetNodeId);
        var directed = edge.Direction == VisualLinkDirection.Forward || edge.Direction == VisualLinkDirection.Backward || edge.Direction == VisualLinkDirection.Bidirectional;
        var sourceArrow = edge.Direction == VisualLinkDirection.Bidirectional;
        var targetArrow = directed;
        if (edge.Direction == VisualLinkDirection.Backward) {
            sourceNodeId = ids.NodeId(edge.TargetNodeId);
            targetNodeId = ids.NodeId(edge.SourceNodeId);
        }

        var label = EdgeLabel(edge);
        var dashPattern = edge.IsMuted ? "none" : TopologyRenderPrimitives.EdgeDash(edge);
        var graphEdge = new GraphSceneEdge {
            Id = ids.EdgeId(index),
            SourceNodeId = sourceNodeId,
            TargetNodeId = targetNodeId,
            Label = label,
            Kind = Token(edge.Kind),
            Status = Token(edge.Status),
            Directed = directed,
            Shape = EdgeShape(edge),
            Curvature = edge.Routing == TopologyEdgeRouting.Curved ? 34 : 0,
            Dashed = !string.Equals(dashPattern, "none", StringComparison.Ordinal),
            Weight = EdgeWeight(edge),
            ShowLabel = !string.IsNullOrWhiteSpace(label)
        };
        graphEdge.SourceArrow = sourceArrow;
        graphEdge.TargetArrow = targetArrow;
        graphEdge.Style.Color = TopologyRenderPrimitives.EdgeColor(edge, chart.Theme ?? TopologyTheme.Light(), new TopologyRenderOptions());
        graphEdge.Style.Width = EdgeStyleWidth(edge);
        if (graphEdge.Dashed) graphEdge.Style.DashPattern = dashPattern;
        AddRoutePoints(graphEdge, chart, edge, topologyNodes, options);
        AddMetadata(graphEdge.Metadata, "topology.id", edge.Id);
        AddMetadata(graphEdge.Metadata, "topology.sourceNodeId", edge.SourceNodeId);
        AddMetadata(graphEdge.Metadata, "topology.targetNodeId", edge.TargetNodeId);
        AddMetadata(graphEdge.Metadata, "topology.direction", edge.Direction.ToString());
        AddMetadata(graphEdge.Metadata, "topology.routing", edge.Routing.ToString());
        AddMetadata(graphEdge.Metadata, "topology.lineStyle", edge.LineStyle.ToString());
        AddMetadata(graphEdge.Metadata, "topology.emphasis", edge.Emphasis.ToString());
        AddMetadata(graphEdge.Metadata, "topology.secondaryLabel", edge.SecondaryLabel);
        AddMetadata(graphEdge.Metadata, "topology.tertiaryLabel", edge.TertiaryLabel);
        AddMetadata(graphEdge.Metadata, "topology.href", edge.Href);
        AddMetadata(graphEdge.Metadata, "topology.tooltip", edge.Tooltip);
        AddMetadata(graphEdge.Metadata, "topology.color", edge.Color);
        CopyMetadata(edge.Metadata, graphEdge.Metadata, "topology.meta.");
        CopyMetadata(edge.Metrics, graphEdge.Metadata, "topology.metric.");
        return graphEdge;
    }

    private static string TopologySceneId(TopologyChart chart) {
        return string.IsNullOrWhiteSpace(chart.Id) ? "topology" : chart.Id!;
    }

    private static string TopologySceneTitle(TopologyChart chart) {
        if (!string.IsNullOrWhiteSpace(chart.Title)) return chart.Title!;
        return string.IsNullOrWhiteSpace(chart.Id) ? "Topology" : chart.Id!;
    }

    private static bool ShouldPreserveCoordinates(TopologyChart chart, TopologyNode node, TopologyGraphSceneOptions options) {
        if (!options.PreserveManualCoordinates && !options.PreserveNonZeroCoordinates) return false;
        if (options.PreserveManualCoordinates && chart.LayoutMode == TopologyLayoutMode.Manual && node.HasPositionOverride) return true;
        return options.PreserveNonZeroCoordinates && (Math.Abs(node.X) > 0.001 || Math.Abs(node.Y) > 0.001);
    }

    private static void ApplyManipulationOptions(GraphScene scene, TopologyGraphSceneOptions options) {
        if (!options.EnableManipulation) return;
        scene.Options.Enable(GraphSceneFeatures.Manipulation);
        scene.Options.Manipulation.EnableEditing();
    }

    private static GraphNodeShape NodeShape(TopologyNode node, TopologyIconDefinition? icon, string? imageUrl) {
        if (!string.IsNullOrWhiteSpace(imageUrl)) return node.DisplayMode == TopologyNodeDisplayMode.Artwork ? GraphNodeShape.RectangularImage : GraphNodeShape.Image;
        if (icon != null) {
            switch (icon.Shape) {
                case TopologyIconShape.Database:
                    return GraphNodeShape.Database;
                case TopologyIconShape.Cloud:
                case TopologyIconShape.Domain:
                case TopologyIconShape.Forest:
                case TopologyIconShape.Site:
                    return GraphNodeShape.Ellipse;
                case TopologyIconShape.Certificate:
                    return GraphNodeShape.Diamond;
                case TopologyIconShape.Team:
                    return GraphNodeShape.Star;
                case TopologyIconShape.Network:
                case TopologyIconShape.NetworkSegment:
                case TopologyIconShape.NetworkSwitch:
                case TopologyIconShape.Router:
                case TopologyIconShape.Firewall:
                case TopologyIconShape.LoadBalancer:
                    return GraphNodeShape.Square;
                case TopologyIconShape.Server:
                case TopologyIconShape.Storage:
                case TopologyIconShape.DomainController:
                case TopologyIconShape.ReadOnlyDomainController:
                case TopologyIconShape.Application:
                case TopologyIconShape.Service:
                case TopologyIconShape.Desktop:
                case TopologyIconShape.Laptop:
                    return GraphNodeShape.Box;
            }
        }

        var display = node.DisplayMode ?? TopologyNodeDisplayMode.Card;
        return display == TopologyNodeDisplayMode.Dot || display == TopologyNodeDisplayMode.Icon ? GraphNodeShape.Circle : GraphNodeShape.Box;
    }

    private static GraphNodeShape NodeShape(TopologyNode node) => NodeShape(node, null, null);

    private static void ApplyHierarchy(TopologyNode source, GraphSceneNode target, TopologyGraphSceneOptions options, TopologyGraphIdMap ids) {
        if (!options.PreserveHierarchyMetadata) return;
        if (source.Metadata.TryGetValue("hierarchy.parentId", out var parentId) && ids.TryNodeId(parentId, out var graphParentId)) target.ParentId = graphParentId;
        if (TryHierarchyLevel(source.Metadata, out var level)) target.Level = level;
    }

    private static bool TryHierarchyLevel(IReadOnlyDictionary<string, string> metadata, out int level) {
        if (metadata.TryGetValue("hierarchy.level", out var hierarchyLevel) && int.TryParse(hierarchyLevel, NumberStyles.Integer, CultureInfo.InvariantCulture, out level)) return true;
        if (metadata.TryGetValue("layer", out var layer) && int.TryParse(layer, NumberStyles.Integer, CultureInfo.InvariantCulture, out level)) return true;
        level = 0;
        return false;
    }

    private static TopologyIconArtwork? ResolveArtwork(TopologyNode node, TopologyIconDefinition? icon, TopologyGraphSceneOptions options) {
        if (!options.IncludeResolvedIconArtwork) return null;
        if (node.Artwork != null && node.Artwork.IsSafe) return node.Artwork;
        return icon?.Artwork != null && icon.Artwork.IsSafe ? icon.Artwork : null;
    }

    private static string? ArtworkImageUrl(TopologyIconArtwork? artwork) {
        if (artwork == null) return null;
        if (artwork.HasImageHref && TopologyIconArtwork.IsSafeImageHref(artwork.ImageHref)) return artwork.ImageHref!.Trim();
        if (!artwork.HasSvgBody || !TopologyIconArtwork.IsSafeSvgFragment(artwork.SvgBody)) return null;
        var viewBox = SafeViewBox(artwork.SvgViewBox);
        var svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"" + viewBox + "\">" + artwork.SvgBody + "</svg>";
        return "data:image/svg+xml;base64," + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(svg));
    }

    private static string SafeViewBox(string? value) {
        var parts = (value ?? string.Empty).Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4) return "0 0 24 24";
        var numbers = new double[4];
        for (var index = 0; index < parts.Length; index++) {
            if (!double.TryParse(parts[index], NumberStyles.Float, CultureInfo.InvariantCulture, out numbers[index]) || double.IsNaN(numbers[index]) || double.IsInfinity(numbers[index])) return "0 0 24 24";
        }

        if (numbers[2] <= 0 || numbers[3] <= 0) return "0 0 24 24";
        return string.Join(" ", numbers.Select(number => number.ToString("0.###", CultureInfo.InvariantCulture)));
    }

    private static double NodeSize(TopologyNode node) {
        var visualSize = Math.Sqrt(Math.Max(1, node.Width * node.Height)) / 7.5;
        return Math.Max(7, Math.Min(28, visualSize));
    }

    private static double EdgeWeight(TopologyEdge edge) {
        if (edge.Emphasis == TopologyEdgeEmphasis.Strong) return 1.8;
        if (edge.Emphasis == TopologyEdgeEmphasis.Subtle || edge.IsMuted) return 0.7;
        return 1;
    }

    private static double? EdgeStyleWidth(TopologyEdge edge) {
        if (edge.Emphasis == TopologyEdgeEmphasis.Normal && !edge.IsMuted) return null;
        var options = new TopologyRenderOptions { VisualStyle = TopologyVisualStyle.MonitoringDashboard };
        return TopologyRenderPrimitives.EdgeStrokeWidth(edge, false, options);
    }

    private static GraphEdgeShape EdgeShape(TopologyEdge edge) {
        if (edge.Waypoints.Count > 0) return GraphEdgeShape.Polyline;
        if (edge.Routing == TopologyEdgeRouting.Curved) return GraphEdgeShape.Curve;
        return edge.Routing is TopologyEdgeRouting.Orthogonal or TopologyEdgeRouting.ObstacleAvoidingOrthogonal ? GraphEdgeShape.Polyline : GraphEdgeShape.Line;
    }

    private static void AddRoutePoints(GraphSceneEdge graphEdge, TopologyChart chart, TopologyEdge edge, IReadOnlyDictionary<string, TopologyNode> topologyNodes, TopologyGraphSceneOptions options) {
        if (graphEdge.Shape != GraphEdgeShape.Polyline) return;
        if (!topologyNodes.TryGetValue(edge.SourceNodeId, out var source) || !topologyNodes.TryGetValue(edge.TargetNodeId, out var target)) return;
        if (!ShouldPreserveCoordinates(chart, source, options) || !ShouldPreserveCoordinates(chart, target, options)) return;
        var points = TopologyRenderPrimitives.EdgePoints(chart, edge, topologyNodes);
        if (edge.Direction == VisualLinkDirection.Backward) points.Reverse();
        AlignRouteEndpointsToGraphNodes(points, edge.Direction == VisualLinkDirection.Backward ? target : source, edge.Direction == VisualLinkDirection.Backward ? source : target);
        foreach (var point in points) graphEdge.RoutePoints.Add(new GraphScenePoint(point.X, point.Y));
        graphEdge.Metadata["topology.routePointCount"] = graphEdge.RoutePoints.Count.ToString(CultureInfo.InvariantCulture);
    }

    private static void AlignRouteEndpointsToGraphNodes(IList<ChartPoint> points, TopologyNode source, TopologyNode target) {
        if (points.Count < 2) return;
        points[0] = ProjectRouteEndpoint(source, points[1]);
        points[points.Count - 1] = ProjectRouteEndpoint(target, points[points.Count - 2]);
    }

    private static ChartPoint ProjectRouteEndpoint(TopologyNode node, ChartPoint guide) {
        var centerX = node.X + node.Width / 2;
        var centerY = node.Y + node.Height / 2;
        if (node.DisplayMode == TopologyNodeDisplayMode.Hidden) return new ChartPoint(centerX, centerY);
        var dx = guide.X - centerX;
        var dy = guide.Y - centerY;
        var length = Math.Max(1, Math.Sqrt(dx * dx + dy * dy));
        var unitX = dx / length;
        var unitY = dy / length;
        var size = NodeSize(node);
        var halfWidth = NodeShape(node) == GraphNodeShape.Box ? size * 1.45 : size;
        var halfHeight = NodeShape(node) == GraphNodeShape.Box ? size * 1.05 : size;
        var xInset = Math.Abs(unitX) < 0.001 ? double.PositiveInfinity : halfWidth / Math.Abs(unitX);
        var yInset = Math.Abs(unitY) < 0.001 ? double.PositiveInfinity : halfHeight / Math.Abs(unitY);
        var inset = Math.Min(xInset, yInset);
        if (double.IsInfinity(inset) || double.IsNaN(inset)) inset = Math.Max(halfWidth, halfHeight);
        return new ChartPoint(centerX + unitX * inset, centerY + unitY * inset);
    }

    private static string? EdgeLabel(TopologyEdge edge) {
        var parts = new[] { edge.Label, edge.SecondaryLabel, edge.TertiaryLabel }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .ToArray();
        return parts.Length == 0 ? null : string.Join(" / ", parts);
    }

    private static string Token<TEnum>(TEnum value) where TEnum : struct {
        return value.ToString()!.ToLowerInvariant();
    }

    private static string? FirstText(params string?[] values) {
        foreach (var value in values) {
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }

        return null;
    }

    private static void AddMetadata(IDictionary<string, string> metadata, string key, string? value) {
        if (!string.IsNullOrWhiteSpace(value)) metadata[key] = value!;
    }

    private static void CopyMetadata(IReadOnlyDictionary<string, string> source, IDictionary<string, string> target, string prefix) {
        foreach (var pair in source) {
            if (!string.IsNullOrWhiteSpace(pair.Key) && pair.Value != null) target[prefix + pair.Key] = pair.Value;
        }
    }

    private sealed class TopologyGraphIdMap {
        private readonly Dictionary<string, string> _nodeIds = new(StringComparer.Ordinal);
        private readonly List<string> _edgeIds = new();
        private readonly Dictionary<string, string> _groupIds = new(StringComparer.Ordinal);

        private TopologyGraphIdMap(string chartId) {
            ChartId = chartId;
        }

        public string ChartId { get; }

        public static TopologyGraphIdMap Create(TopologyChart chart) {
            var chartIds = new HashSet<string>(StringComparer.Ordinal);
            var map = new TopologyGraphIdMap(UniqueToken(string.IsNullOrWhiteSpace(chart.Id) ? "topology" : chart.Id!, "topology", chartIds));
            var nodeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var node in chart.Nodes) map._nodeIds[node.Id] = UniqueToken(node.Id, "node", nodeIds);
            var edgeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var edge in chart.Edges) map._edgeIds.Add(UniqueToken(edge.Id, "edge", edgeIds));
            var groupIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var group in chart.Groups) map._groupIds[group.Id] = UniqueToken(group.Id, "group", groupIds);
            foreach (var groupId in chart.Nodes.Select(node => node.GroupId).Where(groupId => !string.IsNullOrWhiteSpace(groupId)).Select(groupId => groupId!)) {
                if (!map._groupIds.ContainsKey(groupId)) map._groupIds[groupId] = UniqueToken(groupId, "group", groupIds);
            }

            return map;
        }

        public string NodeId(string id) => _nodeIds[id];

        public bool TryNodeId(string id, out string graphNodeId) => _nodeIds.TryGetValue(id, out graphNodeId!);

        public string EdgeId(int index) => _edgeIds[index];

        public string GroupId(string id) => _groupIds[id];

        private static string UniqueToken(string value, string prefix, ISet<string> used) {
            var token = ToToken(value, prefix);
            var candidate = token;
            var suffix = 2;
            while (!used.Add(candidate)) {
                candidate = token + "-" + suffix.ToString(CultureInfo.InvariantCulture);
                suffix++;
            }

            return candidate;
        }

        private static string ToToken(string value, string prefix) {
            var trimmed = value.Trim();
            if (IsToken(trimmed)) return trimmed;
            var builder = new System.Text.StringBuilder(trimmed.Length);
            foreach (var ch in trimmed) builder.Append(char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' || ch == '.' ? ch : '-');
            var token = builder.ToString().Trim('-', '_', '.');
            return token.Length == 0 ? prefix : token;
        }

        private static bool IsToken(string value) {
            if (value.Length == 0) return false;
            foreach (var ch in value) {
                if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' || ch == '.') continue;
                return false;
            }

            return true;
        }
    }
}
