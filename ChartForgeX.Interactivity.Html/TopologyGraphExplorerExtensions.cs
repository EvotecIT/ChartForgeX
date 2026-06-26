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

        var scene = GraphScene.Create(TopologySceneId(chart), TopologySceneTitle(chart));
        scene.Subtitle = chart.Subtitle;
        if (options.UseSuperTopologyDefaults) scene.Options.UseSuperTopologyDefaults(options.EnableManipulation);
        scene.Options.Cluster.Mode = options.IncludeGroupsAsClusters ? GraphClusterMode.Hybrid : GraphClusterMode.Explicit;
        if (!options.IncludeGroupsAsClusters) scene.Options.Cluster.Adaptive = false;
        scene.Options.Cluster.CollapseOnLoad = scene.Options.LevelOfDetail.CollapseClustersOnLoad;
        scene.Metadata["source.model"] = nameof(TopologyChart);
        scene.Metadata["topology.layout"] = chart.LayoutMode.ToString();
        scene.Metadata["topology.direction"] = chart.LayoutDirection.ToString();
        scene.Metadata["topology.nodeCount"] = chart.Nodes.Count.ToString(CultureInfo.InvariantCulture);
        scene.Metadata["topology.edgeCount"] = chart.Edges.Count.ToString(CultureInfo.InvariantCulture);
        scene.Metadata["topology.groupCount"] = chart.Groups.Count.ToString(CultureInfo.InvariantCulture);

        var groupIds = new HashSet<string>(chart.Groups.Select(group => group.Id), StringComparer.Ordinal);
        foreach (var node in chart.Nodes) {
            scene.Nodes.Add(ToGraphNode(chart, node, groupIds, options));
        }

        foreach (var edge in chart.Edges) {
            scene.Edges.Add(ToGraphEdge(edge));
        }

        if (options.IncludeGroupsAsClusters) {
            foreach (var group in chart.Groups) {
                var memberIds = chart.Nodes.Where(node => string.Equals(node.GroupId, group.Id, StringComparison.Ordinal)).Select(node => node.Id).ToArray();
                var cluster = new GraphSceneCluster {
                    Id = group.Id,
                    Label = group.Label,
                    Kind = "topology-group"
                };
                cluster.NodeIds.AddRange(memberIds);
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

    private static GraphSceneNode ToGraphNode(TopologyChart chart, TopologyNode node, ISet<string> groupIds, TopologyGraphSceneOptions options) {
        var graphNode = new GraphSceneNode {
            Id = node.Id,
            Label = node.Label,
            Kind = Token(node.Kind),
            GroupId = string.IsNullOrWhiteSpace(node.GroupId) ? null : node.GroupId,
            Status = Token(node.Status),
            Shape = NodeShape(node),
            Size = NodeSize(node),
            IconText = FirstText(node.Symbol, node.Badge),
            ImageAlt = node.Label,
            Fixed = ShouldPreserveCoordinates(chart, node, options),
            Hidden = node.DisplayMode == TopologyNodeDisplayMode.Hidden
        };
        if (options.IncludeGroupsAsClusters && options.UseGroupsAsClusterIds && !string.IsNullOrWhiteSpace(node.GroupId) && groupIds.Contains(node.GroupId!)) graphNode.ClusterId = node.GroupId;
        if (ShouldPreserveCoordinates(chart, node, options)) {
            graphNode.X = node.X + node.Width / 2;
            graphNode.Y = node.Y + node.Height / 2;
        }

        graphNode.Style.BackgroundColor = node.BackgroundColor;
        graphNode.Style.BorderColor = node.Color;
        graphNode.Style.LabelColor = node.Color;

        AddMetadata(graphNode.Metadata, "topology.id", node.Id);
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

    private static GraphSceneEdge ToGraphEdge(TopologyEdge edge) {
        var sourceNodeId = edge.SourceNodeId;
        var targetNodeId = edge.TargetNodeId;
        var directed = edge.Direction == TopologyDirection.Forward || edge.Direction == TopologyDirection.Backward || edge.Direction == TopologyDirection.Bidirectional;
        var sourceArrow = edge.Direction == TopologyDirection.Bidirectional;
        var targetArrow = directed;
        if (edge.Direction == TopologyDirection.Backward) {
            sourceNodeId = edge.TargetNodeId;
            targetNodeId = edge.SourceNodeId;
        }

        var graphEdge = new GraphSceneEdge {
            Id = edge.Id,
            SourceNodeId = sourceNodeId,
            TargetNodeId = targetNodeId,
            Label = edge.Label,
            Kind = Token(edge.Kind),
            Status = Token(edge.Status),
            Directed = directed,
            Shape = edge.Routing == TopologyEdgeRouting.Curved ? GraphEdgeShape.Curve : GraphEdgeShape.Line,
            Curvature = edge.Routing == TopologyEdgeRouting.Curved ? 34 : 0,
            Dashed = edge.LineStyle == TopologyEdgeLineStyle.Dashed || edge.LineStyle == TopologyEdgeLineStyle.Dotted,
            Weight = EdgeWeight(edge),
            ShowLabel = !string.IsNullOrWhiteSpace(edge.Label)
        };
        graphEdge.SourceArrow = sourceArrow;
        graphEdge.TargetArrow = targetArrow;
        graphEdge.Style.Color = edge.Color;
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
}
