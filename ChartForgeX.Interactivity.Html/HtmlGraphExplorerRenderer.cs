using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using ChartForgeX.Interactivity;

namespace ChartForgeX.Interactivity.Html;

/// <summary>
/// Renders <see cref="GraphScene"/> instances as self-contained dependency-free HTML graph explorers.
/// </summary>
public sealed partial class HtmlGraphExplorerRenderer {
    private const double Width = 960;
    private const double Height = 560;

    /// <summary>
    /// Renders a complete HTML page containing the graph explorer and its inline assets.
    /// </summary>
    /// <param name="scene">The graph scene to render.</param>
    /// <param name="configure">Optional adapter configuration callback.</param>
    /// <returns>A complete self-contained HTML document.</returns>
    public string RenderPage(GraphScene scene, Action<HtmlGraphExplorerOptions>? configure = null) {
        if (scene == null) throw new ArgumentNullException(nameof(scene));
        var options = BuildOptions(configure);
        var title = string.IsNullOrWhiteSpace(options.PageTitle) ? scene.Title : options.PageTitle!;
        var writer = new StringBuilder();
        writer.Append("<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><link rel=\"icon\" href=\"data:,\"><title>");
        writer.Append(Text(title));
        writer.Append("</title><style>");
        writer.Append(BuildFragmentStyle());
        writer.Append("</style></head><body class=\"cfx-graph-shell");
        if (options.Theme == HtmlGraphExplorerTheme.Dark) writer.Append(" cfx-graph-page-dark");
        writer.Append("\">");
        writer.Append(RenderGraph(scene, options));
        AppendScript(writer, options);
        writer.Append("</body></html>");
        return writer.ToString();
    }

    /// <summary>
    /// Renders an embeddable graph explorer fragment containing scoped inline assets.
    /// </summary>
    /// <param name="scene">The graph scene to render.</param>
    /// <param name="configure">Optional adapter configuration callback.</param>
    /// <returns>A self-contained HTML fragment.</returns>
    public string RenderFragment(GraphScene scene, Action<HtmlGraphExplorerOptions>? configure = null) {
        if (scene == null) throw new ArgumentNullException(nameof(scene));
        var options = BuildOptions(configure);
        var writer = new StringBuilder();
        writer.Append("<style data-cfx-graph-assets=\"true\">");
        writer.Append(BuildFragmentStyle());
        writer.Append("</style>");
        writer.Append(RenderGraph(scene, options));
        AppendScript(writer, options);
        return writer.ToString();
    }

    /// <summary>
    /// Gets the scoped graph explorer CSS that hosts can register once.
    /// </summary>
    /// <returns>The raw graph explorer CSS.</returns>
    public static string BuildFragmentStyle() => HtmlGraphExplorerAssets.Style;

    /// <summary>
    /// Gets the raw dependency-free graph explorer browser runtime.
    /// </summary>
    /// <returns>The raw JavaScript graph explorer runtime.</returns>
    public static string BuildInteractionScript() => HtmlGraphExplorerAssets.Script;

    private static HtmlGraphExplorerOptions BuildOptions(Action<HtmlGraphExplorerOptions>? configure) {
        var options = new HtmlGraphExplorerOptions();
        configure?.Invoke(options);
        return options;
    }

    private static string ResolveDomId(GraphScene scene, string graphId, HtmlGraphExplorerOptions options) {
        if (!string.IsNullOrWhiteSpace(options.IdScope)) return SafeId(options.IdScope!);
        return graphId + "-" + StableSceneHash(scene);
    }

    private static string RenderGraph(GraphScene scene, HtmlGraphExplorerOptions options) {
        scene.Validate();
        var effectiveClusters = scene.GetEffectiveClusters();
        var positions = ComputePositions(scene);
        var graphId = SafeId(scene.Id);
        var domId = ResolveDomId(scene, graphId, options);
        var acceleratedMarkup = ShouldUseAcceleratedMarkup(scene, options);
        var writer = new StringBuilder();
        writer.Append("<section class=\"cfx-graph-explorer");
        if (ConsumesTouchMovement(scene)) writer.Append(" cfx-graph-interactive-touch");
        writer.Append('"');
        Attribute(writer, "aria-labelledby", domId + "-heading");
        Attribute(writer, "data-cfx-graph-id", scene.Id);
        Attribute(writer, "data-cfx-graph-title", scene.Title);
        Attribute(writer, "data-cfx-metadata", MetadataJson(scene.Metadata));
        Attribute(writer, "data-cfx-graph-renderer", Backend(options.RenderBackend));
        Attribute(writer, "data-cfx-graph-canvas-fallback", options.AllowCanvasFallback ? "true" : "false");
        Attribute(writer, "data-cfx-graph-theme", Theme(options.Theme));
        Attribute(writer, "data-cfx-graph-theme-persist", options.PersistThemePreference ? "true" : "false");
        Attribute(writer, "data-cfx-graph-features", scene.Options.Features.ToString());
        WritePhysicsAttributes(writer, scene);
        Attribute(writer, "data-cfx-graph-layout", LayoutMode(scene.Options.Layout.Mode));
        Attribute(writer, "data-cfx-graph-layout-direction", scene.Options.Layout.Direction.ToString());
        Attribute(writer, "data-cfx-graph-layout-level-separation", Number(scene.Options.Layout.LevelSeparation));
        Attribute(writer, "data-cfx-graph-layout-node-spacing", Number(scene.Options.Layout.NodeSpacing));
        Attribute(writer, "data-cfx-graph-node-count", scene.Nodes.Count.ToString(CultureInfo.InvariantCulture));
        Attribute(writer, "data-cfx-graph-edge-count", scene.Edges.Count.ToString(CultureInfo.InvariantCulture));
        Attribute(writer, "data-cfx-graph-cluster-count", effectiveClusters.Count.ToString(CultureInfo.InvariantCulture));
        Attribute(writer, "data-cfx-graph-cluster-mode", scene.Options.Cluster.Mode.ToString());
        Attribute(writer, "data-cfx-graph-cluster-adaptive", scene.Options.Cluster.Adaptive ? "true" : "false");
        Attribute(writer, "data-cfx-graph-cluster-min-size", scene.Options.Cluster.MinimumClusterSize.ToString(CultureInfo.InvariantCulture));
        Attribute(writer, "data-cfx-graph-cluster-target-size", scene.Options.Cluster.TargetClusterSize.ToString(CultureInfo.InvariantCulture));
        Attribute(writer, "data-cfx-graph-cluster-collapse-on-load", scene.Options.Cluster.CollapseOnLoad ? "true" : "false");
        Attribute(writer, "data-cfx-graph-manipulation", scene.Options.HasFeature(GraphSceneFeatures.Manipulation) ? "true" : "false");
        Attribute(writer, "data-cfx-graph-manipulation-capabilities", ManipulationCapabilities(scene.Options.Manipulation));
        Attribute(writer, "data-cfx-graph-accelerated-markup", acceleratedMarkup ? "true" : "false");
        WriteScalabilityAttributes(writer, scene);
        WriteHierarchyAttributes(writer, scene);
        writer.Append('>');
        WriteHeader(writer, scene, options, effectiveClusters, domId);
        WriteStage(writer, scene, options, positions, domId, effectiveClusters, acceleratedMarkup);
        writer.Append("<output class=\"cfx-graph-tooltip\" hidden></output>");
        writer.Append("</section>");
        return writer.ToString();
    }

    private static void WriteStage(StringBuilder writer, GraphScene scene, HtmlGraphExplorerOptions options, IReadOnlyDictionary<string, Point> positions, string graphId, IReadOnlyList<GraphSceneCluster> clusters, bool acceleratedMarkup) {
        var clusterMembership = BuildClusterMembership(scene, clusters);
        var clusteringEnabled = scene.Options.HasFeature(GraphSceneFeatures.Clustering);
        var collapseClustersOnLoad = clusteringEnabled && (scene.Options.Cluster.CollapseOnLoad || (scene.Options.HasFeature(GraphSceneFeatures.LevelOfDetail) && scene.Options.LevelOfDetail.CollapseClustersOnLoad));
        var collapsedNodeIds = clusteringEnabled
            ? BuildCollapsedNodeIds(scene, clusters, clusterMembership, collapseClustersOnLoad)
            : new HashSet<string>(StringComparer.Ordinal);
        var collapsedEdgeIds = BuildCollapsedEdgeIds(scene, clusterMembership, collapsedNodeIds);
        var collapsedNodePositions = clusteringEnabled
            ? BuildCollapsedNodeRenderPositions(scene, clusters, positions, clusterMembership, collapseClustersOnLoad)
            : new Dictionary<string, Point>(StringComparer.Ordinal);
        var collapsedNodeRadii = collapsedNodePositions.Count == 0
            ? new Dictionary<string, double>(StringComparer.Ordinal)
            : BuildCollapsedNodeRenderRadii(scene, clusters, positions, clusterMembership, collapseClustersOnLoad);
        var focusableGraphItems = scene.Options.HasFeature(GraphSceneFeatures.Selection);
        if (acceleratedMarkup) WriteGraphDocument(writer, scene, positions, clusterMembership, collapsedNodeIds, collapsedEdgeIds, graphId + "-arrow");
        writer.Append("<div class=\"cfx-graph-stage\" data-cfx-role=\"graph-stage\" role=\"region\"");
        Attribute(writer, "aria-label", scene.Title + " interactive graph");
        Attribute(writer, "aria-describedby", graphId + "-instructions");
        writer.Append("><p class=\"cfx-visually-hidden\"");
        Attribute(writer, "id", graphId + "-instructions");
        writer.Append(">Use the graph controls to search, filter, fit, or export. In the graph, use arrow keys to move between items and Enter or Space to select. Drag nodes to rearrange the topology.</p>");
        WriteStageControls(writer, scene, options, clusters);
        writer.Append("<canvas class=\"cfx-graph-canvas\" data-cfx-role=\"graph-canvas\" width=\"960\" height=\"560\" role=\"img\" aria-hidden=\"true\"");
        Attribute(writer, "aria-label", scene.Title + ". Interactive graph. Use arrow keys to move between nodes and Enter or Space to select.");
        Attribute(writer, "aria-describedby", graphId + "-instructions");
        writer.Append("></canvas><canvas class=\"cfx-graph-webgl\" data-cfx-role=\"graph-webgl\" width=\"960\" height=\"560\" role=\"img\" aria-hidden=\"true\"");
        Attribute(writer, "aria-label", scene.Title + ". Interactive graph. Use arrow keys to move between nodes and Enter or Space to select.");
        Attribute(writer, "aria-describedby", graphId + "-instructions");
        writer.Append("></canvas><canvas class=\"cfx-graph-overview\" data-cfx-role=\"graph-overview\" width=\"168\" height=\"98\" aria-hidden=\"true\"></canvas>");
        writer.Append("<svg class=\"cfx-graph-svg\" data-cfx-role=\"graph-scene\" width=\"960\" height=\"560\" viewBox=\"0 0 960 560\" role=\"");
        writer.Append(focusableGraphItems ? "group" : "img");
        writer.Append("\" aria-hidden=\"false\"");
        Attribute(writer, "aria-labelledby", graphId + "-title");
        Attribute(writer, "aria-describedby", graphId + "-instructions");
        writer.Append("><title");
        Attribute(writer, "id", graphId + "-title");
        writer.Append('>');
        writer.Append(Text(scene.Title));
        writer.Append("</title>");
        WriteArrowMarkers(writer, scene, graphId + "-arrow");
        writer.Append("<rect class=\"cfx-graph-bg\" width=\"960\" height=\"560\"></rect>");
        writer.Append("<g data-cfx-role=\"graph-viewport\">");
        if (clusteringEnabled) WriteClusters(writer, scene, clusters, positions, clusterMembership, collapseClustersOnLoad, focusableGraphItems);
        if (!acceleratedMarkup) {
            WriteEdges(writer, scene, positions, clusterMembership, collapsedNodePositions, collapsedNodeRadii, graphId + "-arrow", collapsedEdgeIds, focusableGraphItems);
            WriteEdgeLabels(writer, scene, positions, collapsedNodePositions, collapsedNodeRadii, collapsedEdgeIds);
            WriteNodes(writer, scene, positions, clusterMembership, collapsedNodeIds, focusableGraphItems, false);
        }
        writer.Append("</g></svg></div>");
    }

    private static void WriteClusters(StringBuilder writer, GraphScene scene, IReadOnlyList<GraphSceneCluster> clusters, IReadOnlyDictionary<string, Point> positions, IReadOnlyDictionary<string, string> clusterMembership, bool collapseClustersOnLoad, bool focusableGraphItems) {
        foreach (var cluster in clusters) {
            var collapsed = cluster.Collapsed || collapseClustersOnLoad;
            var memberIds = ClusterMemberIds(scene, cluster, clusterMembership);
            var members = memberIds.Where(positions.ContainsKey).Select(id => positions[id]).ToArray();
            var x = members.Length == 0 ? Width / 2 : members.Average(point => point.X);
            var y = members.Length == 0 ? Height / 2 : members.Average(point => point.Y);
            var radius = CollapsedClusterRadius(members.Length);
            writer.Append("<g class=\"cfx-graph-cluster");
            if (!collapsed) writer.Append(" cfx-graph-cluster-expanded");
            writer.Append("\" data-cfx-role=\"graph-cluster\"");
            Attribute(writer, "tabindex", "-1");
            Attribute(writer, "aria-hidden", collapsed ? "false" : "true");
            if (focusableGraphItems) {
                Attribute(writer, "role", "button");
                Attribute(writer, "aria-pressed", "false");
                if (collapsed) Attribute(writer, "aria-label", cluster.Label);
            }
            Attribute(writer, "data-cluster-id", cluster.Id);
            Attribute(writer, "data-cluster-label", cluster.Label);
            Attribute(writer, "data-cluster-kind", cluster.Kind);
            Attribute(writer, "data-cluster-parent", cluster.ParentClusterId);
            Attribute(writer, "data-cluster-node-ids", string.Join(",", memberIds));
            Attribute(writer, "data-cluster-collapsed", collapsed ? "true" : "false");
            Attribute(writer, "data-cfx-search", SearchText(cluster.Metadata));
            Attribute(writer, "data-cfx-metadata", MetadataJson(cluster.Metadata));
            Attribute(writer, "transform", "translate(" + Number(x) + " " + Number(y) + ")");
            writer.Append("><circle r=\"");
            writer.Append(Number(radius));
            writer.Append("\"></circle><text y=\"5\">");
            writer.Append(Text(cluster.Label));
            writer.Append("</text></g>");
        }
    }

    private static double CollapsedClusterRadius(int memberCount) {
        return Math.Max(34, Math.Min(54, 20 + Math.Sqrt(memberCount) * 7));
    }

    private static HashSet<string> BuildCollapsedNodeIds(GraphScene scene, IReadOnlyList<GraphSceneCluster> clusters, IReadOnlyDictionary<string, string> clusterMembership, bool collapseClustersOnLoad) {
        var nodeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var cluster in clusters.Where(cluster => cluster.Collapsed || collapseClustersOnLoad)) {
            foreach (var nodeId in ClusterMemberIds(scene, cluster, clusterMembership)) {
                nodeIds.Add(nodeId);
            }
        }

        return nodeIds;
    }

    private static HashSet<string> BuildCollapsedEdgeIds(GraphScene scene, IReadOnlyDictionary<string, string> clusterMembership, HashSet<string> collapsedNodeIds) {
        var edgeIds = new HashSet<string>(StringComparer.Ordinal);
        if (collapsedNodeIds.Count == 0) return edgeIds;
        var nodesById = scene.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        foreach (var edge in scene.Edges) {
            if (!collapsedNodeIds.Contains(edge.SourceNodeId) || !collapsedNodeIds.Contains(edge.TargetNodeId)) continue;
            if (!nodesById.TryGetValue(edge.SourceNodeId, out var sourceNode) || !nodesById.TryGetValue(edge.TargetNodeId, out var targetNode)) continue;
            var sourceClusterId = NodeClusterId(sourceNode, clusterMembership);
            var targetClusterId = NodeClusterId(targetNode, clusterMembership);
            if (!string.IsNullOrWhiteSpace(sourceClusterId) && string.Equals(sourceClusterId, targetClusterId, StringComparison.Ordinal)) edgeIds.Add(edge.Id);
        }

        return edgeIds;
    }

    private static Dictionary<string, Point> BuildCollapsedNodeRenderPositions(GraphScene scene, IReadOnlyList<GraphSceneCluster> clusters, IReadOnlyDictionary<string, Point> positions, IReadOnlyDictionary<string, string> clusterMembership, bool collapseClustersOnLoad) {
        var renderPositions = new Dictionary<string, Point>(StringComparer.Ordinal);
        foreach (var cluster in clusters.Where(cluster => cluster.Collapsed || collapseClustersOnLoad)) {
            var memberIds = ClusterMemberIds(scene, cluster, clusterMembership);
            var members = memberIds.Where(positions.ContainsKey).Select(id => positions[id]).ToArray();
            if (members.Length == 0) continue;
            var point = new Point(members.Average(member => member.X), members.Average(member => member.Y));
            foreach (var nodeId in memberIds) renderPositions[nodeId] = point;
        }

        return renderPositions;
    }

    private static Dictionary<string, double> BuildCollapsedNodeRenderRadii(GraphScene scene, IReadOnlyList<GraphSceneCluster> clusters, IReadOnlyDictionary<string, Point> positions, IReadOnlyDictionary<string, string> clusterMembership, bool collapseClustersOnLoad) {
        var renderRadii = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var cluster in clusters.Where(cluster => cluster.Collapsed || collapseClustersOnLoad)) {
            var memberIds = ClusterMemberIds(scene, cluster, clusterMembership);
            var radius = CollapsedClusterRadius(memberIds.Count(positions.ContainsKey));
            foreach (var nodeId in memberIds) renderRadii[nodeId] = radius;
        }

        return renderRadii;
    }

    private static void WriteNodes(StringBuilder writer, GraphScene scene, IReadOnlyDictionary<string, Point> positions, IReadOnlyDictionary<string, string> clusterMembership, HashSet<string> collapsedNodeIds, bool focusableGraphItems, bool acceleratedMarkup, ISet<string>? labeledNodeIds = null) {
        foreach (var node in scene.Nodes) {
            var point = positions[node.Id];
            var size = SafeNodeSize(node);
            writer.Append("<g class=\"cfx-graph-node");
            if (collapsedNodeIds.Contains(node.Id)) writer.Append(" cfx-graph-cluster-collapsed-member");
            if (node.Hidden) writer.Append(" cfx-graph-hidden");
            writer.Append("\" tabindex=\"");
            writer.Append("-1");
            writer.Append("\" data-cfx-role=\"graph-node\"");
            if (focusableGraphItems) {
                Attribute(writer, "role", "button");
                Attribute(writer, "aria-pressed", "false");
            }
            Attribute(writer, "data-node-id", node.Id);
            Attribute(writer, "data-node-label", node.Label);
            Attribute(writer, "data-node-kind", node.Kind);
            Attribute(writer, "data-node-group", node.GroupId);
            Attribute(writer, "data-node-cluster", NodeClusterId(node, clusterMembership));
            Attribute(writer, "data-node-parent", node.ParentId);
            Attribute(writer, "data-cfx-status", node.Status);
            Attribute(writer, "data-node-size", Number(size));
            Attribute(writer, "data-node-fixed", node.Fixed ? "true" : "false");
            Attribute(writer, "data-node-hidden", node.Hidden ? "true" : "false");
            Attribute(writer, "data-node-level", node.Level.HasValue ? node.Level.Value.ToString(CultureInfo.InvariantCulture) : null);
            Attribute(writer, "data-node-shape", NodeShape(node));
            Attribute(writer, "data-node-image-url", node.ImageUrl);
            Attribute(writer, "data-node-image-alt", node.ImageAlt);
            Attribute(writer, "data-node-icon", node.IconText);
            Attribute(writer, "data-node-secondary-label", node.SecondaryLabel);
            Attribute(writer, "data-node-badge", node.BadgeText);
            Attribute(writer, "data-node-background-color", node.Style.BackgroundColor);
            Attribute(writer, "data-node-border-color", node.Style.BorderColor);
            Attribute(writer, "data-node-label-color", node.Style.LabelColor);
            Attribute(writer, "data-node-label-background-color", node.Style.LabelBackgroundColor);
            Attribute(writer, "data-node-shadow", node.Style.Shadow ? "true" : "false");
            if (focusableGraphItems) Attribute(writer, "aria-label", node.Label);
            Attribute(writer, "data-cfx-search", SearchText(node.Metadata));
            Attribute(writer, "data-cfx-metadata", MetadataJson(node.Metadata));
            Attribute(writer, "data-node-x", Number(point.X));
            Attribute(writer, "data-node-y", Number(point.Y));
            Attribute(writer, "transform", "translate(" + Number(point.X) + " " + Number(point.Y) + ")");
            writer.Append('>');
            if (!acceleratedMarkup) WriteNodeMark(writer, node);
            writer.Append("</g>");
        }

        if (acceleratedMarkup) return;
        writer.Append("<g class=\"cfx-graph-node-details-layer\" data-cfx-role=\"graph-node-details-layer\" pointer-events=\"none\">");
        foreach (var node in scene.Nodes) {
            var point = positions[node.Id];
            var size = SafeNodeSize(node);
            writer.Append("<g class=\"cfx-graph-node-details");
            if (collapsedNodeIds.Contains(node.Id)) writer.Append(" cfx-graph-cluster-collapsed-member");
            if (node.Hidden) writer.Append(" cfx-graph-hidden");
            writer.Append('"');
            Attribute(writer, "data-cfx-role", "graph-node-details");
            Attribute(writer, "data-node-details-for", node.Id);
            Attribute(writer, "data-cfx-status", node.Status);
            Attribute(writer, "transform", "translate(" + Number(point.X) + " " + Number(point.Y) + ")");
            writer.Append('>');
            WriteNodeDetails(writer, node, size, labeledNodeIds == null || labeledNodeIds.Contains(node.Id));
            writer.Append("</g>");
        }
        writer.Append("</g>");
    }

    private static void WriteNodeMark(StringBuilder writer, GraphSceneNode node) {
        var size = SafeNodeSize(node);
        if (node.Shape == GraphNodeShape.Box) {
            writer.Append("<rect x=\"");
            writer.Append(Number(-size * 1.45));
            writer.Append("\" y=\"");
            writer.Append(Number(-size * 1.05));
            writer.Append("\" width=\"");
            writer.Append(Number(size * 2.9));
            writer.Append("\" height=\"");
            writer.Append(Number(size * 2.1));
            writer.Append("\" rx=\"6\"");
            WriteNodeMarkStyle(writer, node);
            writer.Append("></rect>");
        } else if (node.Shape == GraphNodeShape.Square) {
            writer.Append("<rect x=\"");
            writer.Append(Number(-size));
            writer.Append("\" y=\"");
            writer.Append(Number(-size));
            writer.Append("\" width=\"");
            writer.Append(Number(size * 2));
            writer.Append("\" height=\"");
            writer.Append(Number(size * 2));
            writer.Append("\" rx=\"4\"");
            WriteNodeMarkStyle(writer, node);
            writer.Append("></rect>");
        } else if (node.Shape == GraphNodeShape.Database) {
            WriteDatabaseNodeMark(writer, node, size);
        } else if (node.Shape == GraphNodeShape.Ellipse) {
            writer.Append("<ellipse rx=\"");
            writer.Append(Number(size * 1.55));
            writer.Append("\" ry=\"");
            writer.Append(Number(size));
            writer.Append('"');
            WriteNodeMarkStyle(writer, node);
            writer.Append("></ellipse>");
        } else if (node.Shape is GraphNodeShape.Diamond or GraphNodeShape.Triangle or GraphNodeShape.TriangleDown or GraphNodeShape.Star) {
            writer.Append("<polygon points=\"");
            writer.Append(PolygonPoints(node.Shape, size));
            writer.Append('"');
            WriteNodeMarkStyle(writer, node);
            writer.Append("></polygon>");
        } else if (node.Shape == GraphNodeShape.Text) {
            writer.Append("<circle r=\"");
            writer.Append(Number(Math.Max(1, size * 0.18)));
            writer.Append("\" opacity=\"0\"");
            WriteNodeMarkStyle(writer, node);
            writer.Append("></circle>");
        } else if (node.Shape == GraphNodeShape.Image && !string.IsNullOrWhiteSpace(node.ImageUrl)) {
            WriteCircularImageNodeMark(writer, node, size);
        } else if (node.Shape == GraphNodeShape.RectangularImage && !string.IsNullOrWhiteSpace(node.ImageUrl)) {
            WriteRectangularImageNodeMark(writer, node, size);
        } else {
            writer.Append("<circle r=\"");
            writer.Append(Number(size));
            writer.Append('"');
            WriteNodeMarkStyle(writer, node);
            writer.Append("></circle>");
        }

        if (!string.IsNullOrWhiteSpace(node.IconText) && string.IsNullOrWhiteSpace(node.ImageUrl)) {
            writer.Append("<text class=\"cfx-graph-node-icon\" y=\"4\">");
            writer.Append(Text(node.IconText!));
            writer.Append("</text>");
        }
    }

    private static string EdgeAccessibleName(GraphSceneEdge edge, GraphSceneNode? sourceNode, GraphSceneNode? targetNode) {
        if (!string.IsNullOrWhiteSpace(edge.Label)) return edge.Label!;
        var source = string.IsNullOrWhiteSpace(sourceNode?.Label) ? edge.SourceNodeId : sourceNode!.Label;
        var target = string.IsNullOrWhiteSpace(targetNode?.Label) ? edge.TargetNodeId : targetNode!.Label;
        return source + " to " + target;
    }

    private static string SearchText(IReadOnlyDictionary<string, string> metadata) {
        if (metadata.Count == 0) return string.Empty;
        return string.Join(" ", metadata.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Key + " " + pair.Value));
    }

    private static string MetadataJson(IReadOnlyDictionary<string, string> metadata) {
        if (metadata.Count == 0) return string.Empty;
        var writer = new StringBuilder();
        writer.Append('{');
        var first = true;
        foreach (var pair in metadata.OrderBy(pair => pair.Key, StringComparer.Ordinal)) {
            if (!first) writer.Append(',');
            first = false;
            writer.Append(JsonString(pair.Key));
            writer.Append(':');
            writer.Append(JsonString(pair.Value));
        }

        writer.Append('}');
        return writer.ToString();
    }

    private static string ManipulationCapabilities(GraphManipulationOptions options) {
        var values = new List<string>();
        if (options.CanAddNodes) values.Add("addNodes");
        if (options.CanEditNodes) values.Add("editNodes");
        if (options.CanDeleteNodes) values.Add("deleteNodes");
        if (options.CanAddEdges) values.Add("addEdges");
        if (options.CanEditEdges) values.Add("editEdges");
        if (options.CanDeleteEdges) values.Add("deleteEdges");
        if (options.CanDragGroups) values.Add("dragGroups");
        if (options.CanPersistPositions) values.Add("persistPositions");
        return string.Join(",", values);
    }

    private static string JsonString(string value) {
        var writer = new StringBuilder(value.Length + 2);
        writer.Append('"');
        foreach (var ch in value) {
            switch (ch) {
                case '\\':
                    writer.Append("\\\\");
                    break;
                case '"':
                    writer.Append("\\\"");
                    break;
                case '\b':
                    writer.Append("\\b");
                    break;
                case '\f':
                    writer.Append("\\f");
                    break;
                case '\n':
                    writer.Append("\\n");
                    break;
                case '\r':
                    writer.Append("\\r");
                    break;
                case '\t':
                    writer.Append("\\t");
                    break;
                case '<':
                    writer.Append("\\u003c");
                    break;
                case '>':
                    writer.Append("\\u003e");
                    break;
                case '&':
                    writer.Append("\\u0026");
                    break;
                default:
                    if (char.IsControl(ch)) writer.Append("\\u").Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                    else writer.Append(ch);
                    break;
            }
        }

        writer.Append('"');
        return writer.ToString();
    }

    private static string EdgePath(GraphSceneEdge edge, Point source, Point target, GraphSceneNode? targetNode) {
        return EdgePath(edge, source, target, null, targetNode, null, null);
    }

    private static string EdgePath(GraphSceneEdge edge, Point source, Point target, GraphSceneNode? sourceNode, GraphSceneNode? targetNode, double? targetBoundaryInset, double? sourceBoundaryInset) {
        if (string.Equals(edge.SourceNodeId, edge.TargetNodeId, StringComparison.Ordinal)) return SelfLoopPath(target, targetNode);
        if (edge.RoutePoints.Count > 1 && !targetBoundaryInset.HasValue && !sourceBoundaryInset.HasValue) return PolylinePath(PolylineRenderPoints(edge, source, target, sourceNode, targetNode, targetBoundaryInset, sourceBoundaryInset));
        var control = EdgeControl(edge, source, target);
        var renderSource = SourceBoundaryPoint(edge, source, target, control, sourceNode, sourceBoundaryInset);
        var renderTarget = TargetBoundaryPoint(edge, source, target, control, targetNode, targetBoundaryInset);
        return control.HasValue
            ? "M " + Number(renderSource.X) + " " + Number(renderSource.Y) + " Q " + Number(control.Value.X) + " " + Number(control.Value.Y) + " " + Number(renderTarget.X) + " " + Number(renderTarget.Y)
            : "M " + Number(renderSource.X) + " " + Number(renderSource.Y) + " L " + Number(renderTarget.X) + " " + Number(renderTarget.Y);
    }

    private static Point EdgeLabelPoint(GraphSceneEdge edge, Point source, Point target, GraphSceneNode? sourceNode, GraphSceneNode? targetNode, double? targetBoundaryInset = null, double? sourceBoundaryInset = null) {
        if (string.Equals(edge.SourceNodeId, edge.TargetNodeId, StringComparison.Ordinal)) return SelfLoopLabelPoint(target, targetNode);
        if (edge.RoutePoints.Count > 1 && !targetBoundaryInset.HasValue && !sourceBoundaryInset.HasValue) return PolylineMidpoint(PolylineRenderPoints(edge, source, target, sourceNode, targetNode, targetBoundaryInset, sourceBoundaryInset), -7);
        var control = EdgeControl(edge, source, target);
        var renderSource = SourceBoundaryPoint(edge, source, target, control, sourceNode, sourceBoundaryInset);
        var renderTarget = TargetBoundaryPoint(edge, source, target, control, targetNode, targetBoundaryInset);
        return control.HasValue
            ? new Point((renderSource.X + 2 * control.Value.X + renderTarget.X) / 4, (renderSource.Y + 2 * control.Value.Y + renderTarget.Y) / 4 - 7)
            : new Point((renderSource.X + renderTarget.X) / 2, (renderSource.Y + renderTarget.Y) / 2 - 7);
    }

    private static Point? EdgeControl(GraphSceneEdge edge, Point source, Point target) {
        var curvature = IsFinite(edge.Curvature) ? edge.Curvature : 0;
        if ((edge.Shape is GraphEdgeShape.Line or GraphEdgeShape.Polyline) && Math.Abs(curvature) < 0.001) return null;
        var dx = target.X - source.X;
        var dy = target.Y - source.Y;
        var length = Math.Max(1, Math.Sqrt(dx * dx + dy * dy));
        var offset = Math.Abs(curvature) < 0.001 ? 34 : curvature;
        return new Point((source.X + target.X) / 2 - dy / length * offset, (source.Y + target.Y) / 2 + dx / length * offset);
    }

    private static Point SourceBoundaryPoint(GraphSceneEdge edge, Point source, Point target, Point? control, GraphSceneNode? sourceNode, double? sourceBoundaryInset) {
        var to = control ?? target;
        var dx = to.X - source.X;
        var dy = to.Y - source.Y;
        var length = Math.Max(1, Math.Sqrt(dx * dx + dy * dy));
        var inset = sourceBoundaryInset ?? (HasSourceArrow(edge) ? TargetBoundaryInset(sourceNode, dx / length, dy / length) : 0);
        if (inset <= 0) return source;
        return new Point(source.X + dx / length * inset, source.Y + dy / length * inset);
    }

    private static Point DirectedTargetPoint(GraphSceneEdge edge, Point source, Point target, Point? control, GraphSceneNode? targetNode) {
        return TargetBoundaryPoint(edge, source, target, control, targetNode, null);
    }

    private static Point TargetBoundaryPoint(GraphSceneEdge edge, Point source, Point target, Point? control, GraphSceneNode? targetNode, double? targetBoundaryInset) {
        if (!HasTargetArrow(edge) && !targetBoundaryInset.HasValue) return target;
        var from = control ?? source;
        var dx = target.X - from.X;
        var dy = target.Y - from.Y;
        var length = Math.Max(1, Math.Sqrt(dx * dx + dy * dy));
        var inset = targetBoundaryInset ?? TargetBoundaryInset(targetNode, dx / length, dy / length);
        return new Point(target.X - dx / length * inset, target.Y - dy / length * inset);
    }

    private static bool HasSourceArrow(GraphSceneEdge edge) => edge.SourceArrow;

    private static bool HasTargetArrow(GraphSceneEdge edge) => edge.TargetArrow || edge.Directed;

    private static string SelfLoopPath(Point center, GraphSceneNode? node) {
        var right = TargetBoundaryInset(node, 1, 0) + 5;
        var left = TargetBoundaryInset(node, -1, 0) + 5;
        var top = TargetBoundaryInset(node, 0, -1) + 42;
        return "M " + Number(center.X + right) + " " + Number(center.Y)
            + " C " + Number(center.X + right + 44) + " " + Number(center.Y - top)
            + " " + Number(center.X - left - 44) + " " + Number(center.Y - top)
            + " " + Number(center.X - left) + " " + Number(center.Y);
    }

    private static Point SelfLoopLabelPoint(Point center, GraphSceneNode? node) {
        var top = TargetBoundaryInset(node, 0, -1) + 42;
        return new Point(center.X, center.Y - top - 7);
    }

    private static double TargetBoundaryInset(GraphSceneNode? node, double unitX, double unitY) {
        if (node?.Hidden == true) return 0;
        var size = Math.Max(4, node?.Size ?? 8);
        var shape = EffectiveNodeShape(node);
        if (TryNodeBoundaryExtents(shape, size, out var halfWidth, out var halfHeight)) {
            if (Math.Abs(unitX) < 0.001 && Math.Abs(unitY) < 0.001) return Math.Max(6, Math.Max(halfWidth, halfHeight) + 7);
            var xInset = Math.Abs(unitX) < 0.001 ? double.PositiveInfinity : halfWidth / Math.Abs(unitX);
            var yInset = Math.Abs(unitY) < 0.001 ? double.PositiveInfinity : halfHeight / Math.Abs(unitY);
            return Math.Max(6, Math.Min(xInset, yInset) + 7);
        }

        return Math.Max(6, size + (shape == GraphNodeShape.Image ? 11 : 7));
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    private static bool CanRunRuntimePhysics(GraphScene scene) {
        return scene.Options.HasFeature(GraphSceneFeatures.RuntimePhysics)
            && scene.Options.Physics.Solver != GraphPhysicsSolver.None
            && scene.Options.Physics.Solver != GraphPhysicsSolver.StaticPrepared;
    }

    private static bool ShouldRenderViewportControls(GraphScene scene, HtmlGraphExplorerOptions options) {
        if (!options.IncludeViewportControls || !scene.Options.HasFeature(GraphSceneFeatures.Viewport)) return false;
        return !scene.Metadata.TryGetValue("vis.interaction.navigationButtons", out var value) || !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ConsumesTouchMovement(GraphScene scene) {
        return scene.Options.HasFeature(GraphSceneFeatures.Viewport) || scene.Options.HasFeature(GraphSceneFeatures.DragNodes);
    }

    private static double SafeNodeSize(GraphSceneNode node) => Math.Max(4, node.Size);

    private static void AppendScript(StringBuilder writer, HtmlGraphExplorerOptions options) {
        writer.Append("<script");
        if (!string.IsNullOrWhiteSpace(options.ScriptNonce)) Attribute(writer, "nonce", options.ScriptNonce);
        writer.Append('>');
        writer.Append("(() => {\n");
        writer.Append(BuildInteractionScript());
        writer.Append("})();");
        writer.Append("</script>");
    }

    private static string SafeId(string value) {
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value) builder.Append(char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' || ch == '.' ? ch : '-');
        return builder.Length == 0 ? "graph" : builder.ToString();
    }

    private static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(max, value));

    private static string Number(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string Text(string value) => WebUtility.HtmlEncode(value);

    private static void Attribute(StringBuilder writer, string name, string? value) {
        if (string.IsNullOrWhiteSpace(value)) return;
        writer.Append(' ');
        writer.Append(name);
        writer.Append("=\"");
        writer.Append(WebUtility.HtmlEncode(value));
        writer.Append('"');
    }

    private readonly struct Point {
        internal Point(double x, double y) {
            X = x;
            Y = y;
        }

        internal double X { get; }

        internal double Y { get; }
    }
}
