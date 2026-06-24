using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using ChartForgeX.Interactivity;

namespace ChartForgeX.Interactivity.Html;

/// <summary>
/// Renders <see cref="GraphScene"/> instances as self-contained dependency-free HTML graph explorers.
/// </summary>
public sealed partial class HtmlGraphExplorerRenderer {
    private const double Width = 960;
    private const double Height = 560;
    private static int s_renderInstance;

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
        writer.Append("<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>");
        writer.Append(Text(title));
        writer.Append("</title><style>");
        writer.Append(BuildFragmentStyle());
        writer.Append("</style></head><body class=\"cfx-graph-shell\">");
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

    private static string ResolveDomId(string graphId, HtmlGraphExplorerOptions options) {
        if (!string.IsNullOrWhiteSpace(options.IdScope)) return SafeId(options.IdScope!);
        return graphId + "-" + Interlocked.Increment(ref s_renderInstance).ToString(CultureInfo.InvariantCulture);
    }

    private static string RenderGraph(GraphScene scene, HtmlGraphExplorerOptions options) {
        scene.Validate();
        var positions = ComputePositions(scene);
        var graphId = SafeId(scene.Id);
        var domId = ResolveDomId(graphId, options);
        var writer = new StringBuilder();
        writer.Append("<section class=\"cfx-graph-explorer");
        if (ConsumesTouchMovement(scene)) writer.Append(" cfx-graph-interactive-touch");
        writer.Append('"');
        Attribute(writer, "data-cfx-graph-id", scene.Id);
        Attribute(writer, "data-cfx-metadata", MetadataJson(scene.Metadata));
        Attribute(writer, "data-cfx-graph-renderer", Backend(options.RenderBackend));
        Attribute(writer, "data-cfx-graph-canvas-fallback", options.AllowCanvasFallback ? "true" : "false");
        Attribute(writer, "data-cfx-graph-features", scene.Options.Features.ToString());
        Attribute(writer, "data-cfx-graph-physics", scene.Options.Physics.Solver.ToString());
        Attribute(writer, "data-cfx-graph-layout", "structured-prepared");
        Attribute(writer, "data-cfx-graph-stabilization-iterations", scene.Options.Physics.StabilizationIterations.ToString(CultureInfo.InvariantCulture));
        Attribute(writer, "data-cfx-graph-min-velocity", Number(scene.Options.Physics.MinVelocity));
        Attribute(writer, "data-cfx-graph-max-velocity", Number(scene.Options.Physics.MaxVelocity));
        Attribute(writer, "data-cfx-graph-damping", Number(scene.Options.Physics.Damping));
        Attribute(writer, "data-cfx-graph-link-distance", Number(scene.Options.Physics.LinkDistance));
        Attribute(writer, "data-cfx-graph-repulsion", Number(scene.Options.Physics.Repulsion));
        Attribute(writer, "data-cfx-graph-center-gravity", Number(scene.Options.Physics.CenterGravity));
        Attribute(writer, "data-cfx-graph-adaptive-timestep", scene.Options.Physics.AdaptiveTimestep ? "true" : "false");
        Attribute(writer, "data-cfx-graph-node-count", scene.Nodes.Count.ToString(CultureInfo.InvariantCulture));
        Attribute(writer, "data-cfx-graph-edge-count", scene.Edges.Count.ToString(CultureInfo.InvariantCulture));
        Attribute(writer, "data-cfx-graph-cluster-count", scene.Clusters.Count.ToString(CultureInfo.InvariantCulture));
        Attribute(writer, "data-cfx-lod-cluster-threshold", scene.Options.LevelOfDetail.ClusterNodeThreshold.ToString(CultureInfo.InvariantCulture));
        Attribute(writer, "data-cfx-lod-hide-edge-labels-threshold", scene.Options.LevelOfDetail.HideEdgeLabelsThreshold.ToString(CultureInfo.InvariantCulture));
        Attribute(writer, "data-cfx-lod-compact-node-threshold", scene.Options.LevelOfDetail.CompactNodeThreshold.ToString(CultureInfo.InvariantCulture));
        Attribute(writer, "data-cfx-lod-canvas-threshold", scene.Options.LevelOfDetail.CanvasPreferredNodeThreshold.ToString(CultureInfo.InvariantCulture));
        Attribute(writer, "data-cfx-lod-collapse-clusters", scene.Options.LevelOfDetail.CollapseClustersOnLoad ? "true" : "false");
        Attribute(writer, "data-cfx-performance-frame-budget", scene.Options.Performance.FrameBudgetMilliseconds.ToString(CultureInfo.InvariantCulture));
        Attribute(writer, "data-cfx-performance-max-svg-nodes", scene.Options.Performance.MaxInteractiveSvgNodes.ToString(CultureInfo.InvariantCulture));
        Attribute(writer, "data-cfx-performance-max-svg-edges", scene.Options.Performance.MaxInteractiveSvgEdges.ToString(CultureInfo.InvariantCulture));
        Attribute(writer, "data-cfx-performance-max-canvas-nodes", scene.Options.Performance.MaxInteractiveCanvasNodes.ToString(CultureInfo.InvariantCulture));
        Attribute(writer, "data-cfx-performance-max-canvas-edges", scene.Options.Performance.MaxInteractiveCanvasEdges.ToString(CultureInfo.InvariantCulture));
        Attribute(writer, "data-cfx-performance-telemetry-interval", scene.Options.Performance.TelemetrySampleInterval.ToString(CultureInfo.InvariantCulture));
        writer.Append('>');
        WriteHeader(writer, scene, options);
        WriteStage(writer, scene, options, positions, domId);
        writer.Append("<output class=\"cfx-graph-tooltip\" hidden></output>");
        writer.Append("</section>");
        return writer.ToString();
    }

    private static void WriteHeader(StringBuilder writer, GraphScene scene, HtmlGraphExplorerOptions options) {
        writer.Append("<header class=\"cfx-graph-header\"><div><h1 class=\"cfx-graph-title\">");
        writer.Append(Text(scene.Title));
        writer.Append("</h1>");
        if (!string.IsNullOrWhiteSpace(scene.Subtitle)) {
            writer.Append("<p class=\"cfx-graph-subtitle\">");
            writer.Append(Text(scene.Subtitle!));
            writer.Append("</p>");
        }

        writer.Append("</div><div class=\"cfx-graph-toolbar\">");
        if (options.IncludeSearch && scene.Options.HasFeature(GraphSceneFeatures.Search)) writer.Append("<input class=\"cfx-graph-search\" type=\"search\" data-cfx-graph-search=\"true\" placeholder=\"Search\">");
        if (options.IncludeFilters && scene.Options.HasFeature(GraphSceneFeatures.Filtering)) {
            WriteFilter(writer, "status", scene.Nodes.Select(node => node.Status).Concat(scene.Edges.Select(edge => edge.Status)));
            var kindValues = scene.Nodes.Select(node => node.Kind).Concat(scene.Edges.Select(edge => edge.Kind));
            if (scene.Options.HasFeature(GraphSceneFeatures.Clustering)) kindValues = kindValues.Concat(scene.Clusters.Select(cluster => cluster.Kind));
            WriteFilter(writer, "kind", kindValues);
        }

        if (options.IncludeClusterControls && scene.Clusters.Count > 0 && scene.Options.HasFeature(GraphSceneFeatures.Clustering)) WriteButton(writer, "clusters", "Clusters", true);
        if (scene.Options.HasFeature(GraphSceneFeatures.Selection) && scene.Options.HasFeature(GraphSceneFeatures.NeighborhoodFocus)) WriteButton(writer, "focus", "Focus", true);
        if (scene.Options.HasFeature(GraphSceneFeatures.Selection) && scene.Options.HasFeature(GraphSceneFeatures.MultiSelection)) WriteButton(writer, "clear-selection", "Clear");
        if (scene.Options.HasFeature(GraphSceneFeatures.Viewport)) {
            WriteButton(writer, "fit", "Fit");
            WriteButton(writer, "zoom-in", "+");
            WriteButton(writer, "zoom-out", "-");
        }

        if (options.IncludePhysicsControls && CanRunRuntimePhysics(scene)) {
            WriteButton(writer, "physics", "Physics", true);
            if (scene.Options.HasFeature(GraphSceneFeatures.Stabilization)) WriteButton(writer, "stabilize", "Stabilize");
        }

        if (scene.Options.HasFeature(GraphSceneFeatures.Export)) {
            WriteButton(writer, "export-svg", "SVG");
            WriteButton(writer, "export-png", "PNG");
            WriteButton(writer, "export-json", "JSON");
        }

        writer.Append("</div></header>");
    }

    private static void WriteStage(StringBuilder writer, GraphScene scene, HtmlGraphExplorerOptions options, IReadOnlyDictionary<string, Point> positions, string graphId) {
        var clusterMembership = BuildClusterMembership(scene);
        var clusteringEnabled = scene.Options.HasFeature(GraphSceneFeatures.Clustering);
        var collapseClustersOnLoad = clusteringEnabled && scene.Options.HasFeature(GraphSceneFeatures.LevelOfDetail) && scene.Options.LevelOfDetail.CollapseClustersOnLoad;
        var collapsedNodeIds = clusteringEnabled
            ? BuildCollapsedNodeIds(scene, clusterMembership, collapseClustersOnLoad)
            : new HashSet<string>(StringComparer.Ordinal);
        var collapsedEdgeIds = BuildCollapsedEdgeIds(scene, clusterMembership, collapsedNodeIds);
        var collapsedNodePositions = clusteringEnabled
            ? BuildCollapsedNodeRenderPositions(scene, positions, clusterMembership, collapseClustersOnLoad)
            : new Dictionary<string, Point>(StringComparer.Ordinal);
        var collapsedNodeRadii = collapsedNodePositions.Count == 0
            ? new Dictionary<string, double>(StringComparer.Ordinal)
            : BuildCollapsedNodeRenderRadii(scene, positions, clusterMembership, collapseClustersOnLoad);
        writer.Append("<div class=\"cfx-graph-stage\"><canvas class=\"cfx-graph-canvas\" data-cfx-role=\"graph-canvas\" width=\"960\" height=\"560\"></canvas><canvas class=\"cfx-graph-overview\" data-cfx-role=\"graph-overview\" width=\"168\" height=\"98\" aria-hidden=\"true\"></canvas>");
        writer.Append("<svg class=\"cfx-graph-svg\" data-cfx-role=\"graph-scene\" width=\"960\" height=\"560\" viewBox=\"0 0 960 560\" role=\"img\"");
        Attribute(writer, "aria-labelledby", graphId + "-title");
        writer.Append("><title");
        Attribute(writer, "id", graphId + "-title");
        writer.Append('>');
        writer.Append(Text(scene.Title));
        writer.Append("</title><defs><marker");
        Attribute(writer, "id", graphId + "-arrow");
        writer.Append(" viewBox=\"0 0 10 10\" refX=\"9\" refY=\"5\" markerWidth=\"6\" markerHeight=\"6\" orient=\"auto-start-reverse\"><path d=\"M 0 0 L 10 5 L 0 10 z\"></path></marker></defs>");
        writer.Append("<rect class=\"cfx-graph-bg\" width=\"960\" height=\"560\"></rect>");
        writer.Append("<g data-cfx-role=\"graph-viewport\">");
        var focusableGraphItems = scene.Options.HasFeature(GraphSceneFeatures.Selection);
        if (clusteringEnabled) WriteClusters(writer, scene, positions, clusterMembership, collapseClustersOnLoad, focusableGraphItems);
        WriteEdges(writer, scene, positions, clusterMembership, collapsedNodePositions, collapsedNodeRadii, graphId + "-arrow", collapsedEdgeIds, focusableGraphItems);
        WriteEdgeLabels(writer, scene, positions, collapsedNodePositions, collapsedEdgeIds);
        WriteNodes(writer, scene, positions, clusterMembership, collapsedNodeIds, focusableGraphItems);
        writer.Append("</g></svg></div>");
    }

    private static void WriteClusters(StringBuilder writer, GraphScene scene, IReadOnlyDictionary<string, Point> positions, IReadOnlyDictionary<string, string> clusterMembership, bool collapseClustersOnLoad, bool focusableGraphItems) {
        foreach (var cluster in scene.Clusters) {
            var collapsed = cluster.Collapsed || collapseClustersOnLoad;
            var memberIds = ClusterMemberIds(scene, cluster, clusterMembership);
            var members = memberIds.Where(positions.ContainsKey).Select(id => positions[id]).ToArray();
            var x = members.Length == 0 ? Width / 2 : members.Average(point => point.X);
            var y = members.Length == 0 ? Height / 2 : members.Average(point => point.Y);
            var radius = CollapsedClusterRadius(members.Length);
            writer.Append("<g class=\"cfx-graph-cluster");
            if (!collapsed) writer.Append(" cfx-graph-cluster-expanded");
            writer.Append("\" data-cfx-role=\"graph-cluster\"");
            Attribute(writer, "tabindex", collapsed && focusableGraphItems ? "0" : "-1");
            Attribute(writer, "aria-hidden", collapsed ? "false" : "true");
            if (collapsed && focusableGraphItems) Attribute(writer, "aria-label", cluster.Label);
            Attribute(writer, "data-cluster-id", cluster.Id);
            Attribute(writer, "data-cluster-label", cluster.Label);
            Attribute(writer, "data-cluster-kind", cluster.Kind);
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

    private static HashSet<string> BuildCollapsedNodeIds(GraphScene scene, IReadOnlyDictionary<string, string> clusterMembership, bool collapseClustersOnLoad) {
        var nodeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var cluster in scene.Clusters.Where(cluster => cluster.Collapsed || collapseClustersOnLoad)) {
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

    private static Dictionary<string, Point> BuildCollapsedNodeRenderPositions(GraphScene scene, IReadOnlyDictionary<string, Point> positions, IReadOnlyDictionary<string, string> clusterMembership, bool collapseClustersOnLoad) {
        var renderPositions = new Dictionary<string, Point>(StringComparer.Ordinal);
        foreach (var cluster in scene.Clusters.Where(cluster => cluster.Collapsed || collapseClustersOnLoad)) {
            var memberIds = ClusterMemberIds(scene, cluster, clusterMembership);
            var members = memberIds.Where(positions.ContainsKey).Select(id => positions[id]).ToArray();
            if (members.Length == 0) continue;
            var point = new Point(members.Average(member => member.X), members.Average(member => member.Y));
            foreach (var nodeId in memberIds) renderPositions[nodeId] = point;
        }

        return renderPositions;
    }

    private static Dictionary<string, double> BuildCollapsedNodeRenderRadii(GraphScene scene, IReadOnlyDictionary<string, Point> positions, IReadOnlyDictionary<string, string> clusterMembership, bool collapseClustersOnLoad) {
        var renderRadii = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var cluster in scene.Clusters.Where(cluster => cluster.Collapsed || collapseClustersOnLoad)) {
            var memberIds = ClusterMemberIds(scene, cluster, clusterMembership);
            var radius = CollapsedClusterRadius(memberIds.Count(positions.ContainsKey));
            foreach (var nodeId in memberIds) renderRadii[nodeId] = radius;
        }

        return renderRadii;
    }

    private static void WriteEdges(StringBuilder writer, GraphScene scene, IReadOnlyDictionary<string, Point> positions, IReadOnlyDictionary<string, string> clusterMembership, IReadOnlyDictionary<string, Point> collapsedNodePositions, IReadOnlyDictionary<string, double> collapsedNodeRadii, string markerId, HashSet<string> collapsedEdgeIds, bool focusableGraphItems) {
        var nodesById = scene.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        foreach (var edge in scene.Edges) {
            if (!positions.TryGetValue(edge.SourceNodeId, out var source) || !positions.TryGetValue(edge.TargetNodeId, out var target)) continue;
            nodesById.TryGetValue(edge.SourceNodeId, out var sourceNode);
            nodesById.TryGetValue(edge.TargetNodeId, out var targetNode);
            var renderSource = collapsedNodePositions.TryGetValue(edge.SourceNodeId, out var collapsedSource) ? collapsedSource : source;
            var renderTarget = collapsedNodePositions.TryGetValue(edge.TargetNodeId, out var collapsedTarget) ? collapsedTarget : target;
            var targetBoundary = collapsedNodeRadii.TryGetValue(edge.TargetNodeId, out var targetRadius) ? targetRadius : (double?)null;
            var path = EdgePath(edge, renderSource, renderTarget, targetNode, targetBoundary);
            writer.Append("<path class=\"cfx-graph-edge");
            if (collapsedEdgeIds.Contains(edge.Id)) writer.Append(" cfx-graph-cluster-collapsed-member");
            writer.Append("\" tabindex=\"");
            writer.Append(focusableGraphItems ? "0" : "-1");
            writer.Append("\" data-cfx-role=\"graph-edge\"");
            Attribute(writer, "data-edge-id", edge.Id);
            Attribute(writer, "data-edge-label", edge.Label);
            Attribute(writer, "data-edge-kind", edge.Kind);
            Attribute(writer, "data-cfx-status", edge.Status);
            Attribute(writer, "data-source-node-id", edge.SourceNodeId);
            Attribute(writer, "data-target-node-id", edge.TargetNodeId);
            Attribute(writer, "data-source-cluster-id", sourceNode == null ? null : NodeClusterId(sourceNode, clusterMembership));
            Attribute(writer, "data-target-cluster-id", targetNode == null ? null : NodeClusterId(targetNode, clusterMembership));
            Attribute(writer, "data-edge-weight", Number(edge.Weight));
            Attribute(writer, "data-edge-length", Number(edge.Length));
            Attribute(writer, "data-edge-directed", edge.Directed ? "true" : "false");
            Attribute(writer, "data-edge-shape", EdgeShape(edge.Shape));
            Attribute(writer, "data-edge-curvature", Number(edge.Curvature));
            Attribute(writer, "data-edge-dashed", edge.Dashed ? "true" : "false");
            Attribute(writer, "data-edge-show-label", edge.ShowLabel ? "true" : "false");
            Attribute(writer, "data-cfx-search", SearchText(edge.Metadata));
            Attribute(writer, "data-cfx-metadata", MetadataJson(edge.Metadata));
            Attribute(writer, "aria-label", EdgeAccessibleName(edge, sourceNode, targetNode));
            Attribute(writer, "d", path);
            if (edge.Directed) Attribute(writer, "marker-end", "url(#" + markerId + ")");
            writer.Append("></path>");
        }
    }

    private static string[] ClusterMemberIds(GraphScene scene, GraphSceneCluster cluster, IReadOnlyDictionary<string, string> clusterMembership) {
        var members = new HashSet<string>(cluster.NodeIds, StringComparer.Ordinal);
        foreach (var node in scene.Nodes) {
            if (clusterMembership.TryGetValue(node.Id, out var clusterId) && string.Equals(cluster.Id, clusterId, StringComparison.Ordinal)) members.Add(node.Id);
        }

        return members.OrderBy(id => id, StringComparer.Ordinal).ToArray();
    }

    private static void WriteEdgeLabels(StringBuilder writer, GraphScene scene, IReadOnlyDictionary<string, Point> positions, IReadOnlyDictionary<string, Point> collapsedNodePositions, HashSet<string> collapsedEdgeIds) {
        var nodesById = scene.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        foreach (var edge in scene.Edges.Where(edge => edge.ShowLabel && !string.IsNullOrWhiteSpace(edge.Label))) {
            if (!positions.TryGetValue(edge.SourceNodeId, out var source) || !positions.TryGetValue(edge.TargetNodeId, out var target)) continue;
            nodesById.TryGetValue(edge.TargetNodeId, out var targetNode);
            var renderSource = collapsedNodePositions.TryGetValue(edge.SourceNodeId, out var collapsedSource) ? collapsedSource : source;
            var renderTarget = collapsedNodePositions.TryGetValue(edge.TargetNodeId, out var collapsedTarget) ? collapsedTarget : target;
            var point = EdgeLabelPoint(edge, renderSource, renderTarget, targetNode);
            writer.Append("<text class=\"cfx-graph-edge-label");
            if (collapsedEdgeIds.Contains(edge.Id)) writer.Append(" cfx-graph-cluster-collapsed-member");
            writer.Append("\" data-cfx-role=\"graph-edge-label\"");
            Attribute(writer, "data-edge-label-for", edge.Id);
            Attribute(writer, "x", Number(point.X));
            Attribute(writer, "y", Number(point.Y));
            writer.Append('>');
            writer.Append(Text(edge.Label!));
            writer.Append("</text>");
        }
    }

    private static void WriteNodes(StringBuilder writer, GraphScene scene, IReadOnlyDictionary<string, Point> positions, IReadOnlyDictionary<string, string> clusterMembership, HashSet<string> collapsedNodeIds, bool focusableGraphItems) {
        foreach (var node in scene.Nodes) {
            var point = positions[node.Id];
            var size = SafeNodeSize(node);
            writer.Append("<g class=\"cfx-graph-node");
            if (collapsedNodeIds.Contains(node.Id)) writer.Append(" cfx-graph-cluster-collapsed-member");
            writer.Append("\" tabindex=\"");
            writer.Append(focusableGraphItems ? "0" : "-1");
            writer.Append("\" data-cfx-role=\"graph-node\"");
            Attribute(writer, "data-node-id", node.Id);
            Attribute(writer, "data-node-label", node.Label);
            Attribute(writer, "data-node-kind", node.Kind);
            Attribute(writer, "data-node-group", node.GroupId);
            Attribute(writer, "data-node-cluster", NodeClusterId(node, clusterMembership));
            Attribute(writer, "data-cfx-status", node.Status);
            Attribute(writer, "data-node-size", Number(size));
            Attribute(writer, "data-node-fixed", node.Fixed ? "true" : "false");
            Attribute(writer, "data-node-shape", NodeShape(node.Shape));
            Attribute(writer, "data-node-image-url", node.ImageUrl);
            Attribute(writer, "data-node-icon", node.IconText);
            if (focusableGraphItems) Attribute(writer, "aria-label", node.Label);
            Attribute(writer, "data-cfx-search", SearchText(node.Metadata));
            Attribute(writer, "data-cfx-metadata", MetadataJson(node.Metadata));
            Attribute(writer, "data-node-x", Number(point.X));
            Attribute(writer, "data-node-y", Number(point.Y));
            Attribute(writer, "transform", "translate(" + Number(point.X) + " " + Number(point.Y) + ")");
            writer.Append('>');
            WriteNodeMark(writer, node);
            writer.Append("<text y=\"");
            writer.Append(Number(size + 18));
            writer.Append("\">");
            writer.Append(Text(node.Label));
            writer.Append("</text></g>");
        }
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
            writer.Append("\" rx=\"6\"></rect>");
        } else if (node.Shape == GraphNodeShape.Image && !string.IsNullOrWhiteSpace(node.ImageUrl)) {
            writer.Append("<circle r=\"");
            writer.Append(Number(size + 4));
            writer.Append("\"></circle><image");
            Attribute(writer, "href", node.ImageUrl);
            Attribute(writer, "aria-label", node.ImageAlt);
            Attribute(writer, "x", Number(-size));
            Attribute(writer, "y", Number(-size));
            Attribute(writer, "width", Number(size * 2));
            Attribute(writer, "height", Number(size * 2));
            writer.Append("></image>");
        } else {
            writer.Append("<circle r=\"");
            writer.Append(Number(size));
            writer.Append("\"></circle>");
        }

        if (!string.IsNullOrWhiteSpace(node.IconText)) {
            writer.Append("<text class=\"cfx-graph-node-icon\" y=\"4\">");
            writer.Append(Text(node.IconText!));
            writer.Append("</text>");
        }
    }

    private static void WriteFilter(StringBuilder writer, string name, IEnumerable<string?> values) {
        var options = values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        if (options.Length == 0) return;
        writer.Append("<select class=\"cfx-graph-filter\"");
        Attribute(writer, "data-cfx-graph-filter", name);
        writer.Append("><option value=\"\">");
        writer.Append(Text(name));
        writer.Append("</option>");
        foreach (var value in options) {
            writer.Append("<option");
            Attribute(writer, "value", value);
            writer.Append('>');
            writer.Append(Text(value));
            writer.Append("</option>");
        }

        writer.Append("</select>");
    }

    private static string EdgeAccessibleName(GraphSceneEdge edge, GraphSceneNode? sourceNode, GraphSceneNode? targetNode) {
        if (!string.IsNullOrWhiteSpace(edge.Label)) return edge.Label!;
        var source = string.IsNullOrWhiteSpace(sourceNode?.Label) ? edge.SourceNodeId : sourceNode!.Label;
        var target = string.IsNullOrWhiteSpace(targetNode?.Label) ? edge.TargetNodeId : targetNode!.Label;
        return source + " to " + target;
    }

    private static void WriteButton(StringBuilder writer, string action, string label, bool pressedState = false) {
        writer.Append("<button class=\"cfx-graph-tool\" type=\"button\"");
        Attribute(writer, "data-cfx-graph-action", action);
        if (pressedState) writer.Append(" aria-pressed=\"false\"");
        writer.Append('>');
        writer.Append(Text(label));
        writer.Append("</button>");
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
        return EdgePath(edge, source, target, targetNode, null);
    }

    private static string EdgePath(GraphSceneEdge edge, Point source, Point target, GraphSceneNode? targetNode, double? targetBoundaryInset) {
        if (string.Equals(edge.SourceNodeId, edge.TargetNodeId, StringComparison.Ordinal)) return SelfLoopPath(target, targetNode);
        var control = EdgeControl(edge, source, target);
        var renderTarget = DirectedTargetPoint(edge, source, target, control, targetNode, targetBoundaryInset);
        return control.HasValue
            ? "M " + Number(source.X) + " " + Number(source.Y) + " Q " + Number(control.Value.X) + " " + Number(control.Value.Y) + " " + Number(renderTarget.X) + " " + Number(renderTarget.Y)
            : "M " + Number(source.X) + " " + Number(source.Y) + " L " + Number(renderTarget.X) + " " + Number(renderTarget.Y);
    }

    private static Point EdgeLabelPoint(GraphSceneEdge edge, Point source, Point target, GraphSceneNode? targetNode) {
        if (string.Equals(edge.SourceNodeId, edge.TargetNodeId, StringComparison.Ordinal)) return SelfLoopLabelPoint(target, targetNode);
        var control = EdgeControl(edge, source, target);
        return control.HasValue
            ? new Point((source.X + 2 * control.Value.X + target.X) / 4, (source.Y + 2 * control.Value.Y + target.Y) / 4 - 7)
            : new Point((source.X + target.X) / 2, (source.Y + target.Y) / 2 - 7);
    }

    private static Point? EdgeControl(GraphSceneEdge edge, Point source, Point target) {
        var curvature = IsFinite(edge.Curvature) ? edge.Curvature : 0;
        if (edge.Shape != GraphEdgeShape.Curve && Math.Abs(curvature) < 0.001) return null;
        var dx = target.X - source.X;
        var dy = target.Y - source.Y;
        var length = Math.Max(1, Math.Sqrt(dx * dx + dy * dy));
        var offset = Math.Abs(curvature) < 0.001 ? 34 : curvature;
        return new Point((source.X + target.X) / 2 - dy / length * offset, (source.Y + target.Y) / 2 + dx / length * offset);
    }

    private static Point DirectedTargetPoint(GraphSceneEdge edge, Point source, Point target, Point? control, GraphSceneNode? targetNode) {
        return DirectedTargetPoint(edge, source, target, control, targetNode, null);
    }

    private static Point DirectedTargetPoint(GraphSceneEdge edge, Point source, Point target, Point? control, GraphSceneNode? targetNode, double? targetBoundaryInset) {
        if (!edge.Directed) return target;
        var from = control ?? source;
        var dx = target.X - from.X;
        var dy = target.Y - from.Y;
        var length = Math.Max(1, Math.Sqrt(dx * dx + dy * dy));
        var inset = targetBoundaryInset ?? TargetBoundaryInset(targetNode, dx / length, dy / length);
        return new Point(target.X - dx / length * inset, target.Y - dy / length * inset);
    }

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
        var size = Math.Max(4, node?.Size ?? 8);
        if (node?.Shape == GraphNodeShape.Box) {
            var halfWidth = size * 1.45;
            var halfHeight = size * 1.05;
            if (Math.Abs(unitX) < 0.001 && Math.Abs(unitY) < 0.001) return Math.Max(6, Math.Max(halfWidth, halfHeight) + 7);
            var xInset = Math.Abs(unitX) < 0.001 ? double.PositiveInfinity : halfWidth / Math.Abs(unitX);
            var yInset = Math.Abs(unitY) < 0.001 ? double.PositiveInfinity : halfHeight / Math.Abs(unitY);
            return Math.Max(6, Math.Min(xInset, yInset) + 7);
        }

        return Math.Max(6, size + (node?.Shape == GraphNodeShape.Image ? 11 : 7));
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    private static bool CanRunRuntimePhysics(GraphScene scene) {
        return scene.Options.HasFeature(GraphSceneFeatures.RuntimePhysics)
            && scene.Options.Physics.Solver != GraphPhysicsSolver.None
            && scene.Options.Physics.Solver != GraphPhysicsSolver.StaticPrepared;
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

    private static string Backend(HtmlGraphRenderBackend backend) => backend switch {
        HtmlGraphRenderBackend.Canvas => "canvas",
        HtmlGraphRenderBackend.WebGl => "canvas",
        _ => "svg"
    };

    private static string NodeShape(GraphNodeShape shape) => shape switch {
        GraphNodeShape.Box => "box",
        GraphNodeShape.Image => "image",
        _ => "circle"
    };

    private static string EdgeShape(GraphEdgeShape shape) => shape == GraphEdgeShape.Curve ? "curve" : "line";

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
