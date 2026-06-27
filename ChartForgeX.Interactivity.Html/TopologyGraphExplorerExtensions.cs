using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Interactivity;
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
        var scene = GraphScene.Create(ids.ChartId, TopologySceneTitle(chart));
        scene.Subtitle = chart.Subtitle;
        if (options.UseSuperTopologyDefaults) scene.Options.UseSuperTopologyDefaults(options.EnableManipulation);
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
            scene.Nodes.Add(ToGraphNode(chart, node, groupIds, options, ids));
        }

        for (var index = 0; index < chart.Edges.Count; index++) {
            scene.Edges.Add(ToGraphEdge(chart.Edges[index], ids, index));
        }

        if (options.IncludeGroupsAsClusters) {
            foreach (var group in chart.Groups) {
                var groupId = ids.GroupId(group.Id);
                var memberIds = chart.Nodes.Where(node => string.Equals(node.GroupId, group.Id, StringComparison.Ordinal)).Select(node => ids.NodeId(node.Id)).ToArray();
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

    private static GraphSceneNode ToGraphNode(TopologyChart chart, TopologyNode node, ISet<string> groupIds, TopologyGraphSceneOptions options, TopologyGraphIdMap ids) {
        var groupId = string.IsNullOrWhiteSpace(node.GroupId) ? null : ids.GroupId(node.GroupId!);
        var graphNode = new GraphSceneNode {
            Id = ids.NodeId(node.Id),
            Label = node.Label,
            Kind = Token(node.Kind),
            GroupId = groupId,
            Status = Token(node.Status),
            Shape = node.DisplayMode == TopologyNodeDisplayMode.Hidden ? GraphNodeShape.Text : NodeShape(node),
            Size = NodeSize(node),
            IconText = FirstText(node.Symbol, node.Badge),
            ImageAlt = node.Label,
            Fixed = ShouldPreserveCoordinates(chart, node, options),
            Hidden = node.DisplayMode == TopologyNodeDisplayMode.Hidden
        };
        if (options.IncludeGroupsAsClusters && options.UseGroupsAsClusterIds && !string.IsNullOrWhiteSpace(groupId) && groupIds.Contains(groupId!)) graphNode.ClusterId = groupId;
        if (ShouldPreserveCoordinates(chart, node, options)) {
            graphNode.X = node.X + node.Width / 2;
            graphNode.Y = node.Y + node.Height / 2;
        }

        graphNode.Style.BackgroundColor = node.BackgroundColor;
        graphNode.Style.BorderColor = node.Color;
        graphNode.Style.LabelColor = node.Color;

        AddMetadata(graphNode.Metadata, "topology.id", node.Id);
        AddMetadata(graphNode.Metadata, "topology.groupId", node.GroupId);
        AddMetadata(graphNode.Metadata, "topology.subtitle", node.Subtitle);
        AddMetadata(graphNode.Metadata, "topology.kind", node.Kind.ToString());
        AddMetadata(graphNode.Metadata, "topology.displayMode", node.DisplayMode?.ToString());
        AddMetadata(graphNode.Metadata, "topology.iconId", node.IconId);
        AddMetadata(graphNode.Metadata, "topology.badge", node.Badge);
        AddMetadata(graphNode.Metadata, "topology.href", node.Href);
        AddMetadata(graphNode.Metadata, "topology.tooltip", node.Tooltip);
        AddMetadata(graphNode.Metadata, "topology.color", node.Color);
        AddMetadata(graphNode.Metadata, "topology.backgroundColor", node.BackgroundColor);
        CopyMetadata(node.Metadata, graphNode.Metadata, "topology.meta.");
        CopyMetadata(node.Metrics, graphNode.Metadata, "topology.metric.");
        return graphNode;
    }

    private static GraphSceneEdge ToGraphEdge(TopologyEdge edge, TopologyGraphIdMap ids, int index) {
        var sourceNodeId = ids.NodeId(edge.SourceNodeId);
        var targetNodeId = ids.NodeId(edge.TargetNodeId);
        var directed = edge.Direction == TopologyDirection.Forward || edge.Direction == TopologyDirection.Backward || edge.Direction == TopologyDirection.Bidirectional;
        var sourceArrow = edge.Direction == TopologyDirection.Bidirectional;
        var targetArrow = directed;
        if (edge.Direction == TopologyDirection.Backward) {
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
            Shape = edge.Routing == TopologyEdgeRouting.Curved ? GraphEdgeShape.Curve : GraphEdgeShape.Line,
            Curvature = edge.Routing == TopologyEdgeRouting.Curved ? 34 : 0,
            Dashed = !string.Equals(dashPattern, "none", StringComparison.Ordinal),
            Weight = EdgeWeight(edge),
            ShowLabel = !string.IsNullOrWhiteSpace(label)
        };
        graphEdge.SourceArrow = sourceArrow;
        graphEdge.TargetArrow = targetArrow;
        graphEdge.Style.Color = edge.Color;
        if (graphEdge.Dashed) graphEdge.Style.DashPattern = dashPattern;
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
        if (options.PreserveManualCoordinates && chart.LayoutMode == TopologyLayoutMode.Manual) return true;
        return options.PreserveNonZeroCoordinates && (Math.Abs(node.X) > 0.001 || Math.Abs(node.Y) > 0.001);
    }

    private static GraphNodeShape NodeShape(TopologyNode node) {
        var display = node.DisplayMode ?? TopologyNodeDisplayMode.Card;
        return display == TopologyNodeDisplayMode.Dot || display == TopologyNodeDisplayMode.Icon ? GraphNodeShape.Circle : GraphNodeShape.Box;
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
