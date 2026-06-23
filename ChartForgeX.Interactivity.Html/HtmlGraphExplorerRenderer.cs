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
public sealed class HtmlGraphExplorerRenderer {
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

    private static string RenderGraph(GraphScene scene, HtmlGraphExplorerOptions options) {
        scene.Validate();
        var positions = ComputePositions(scene);
        var graphId = SafeId(scene.Id);
        var writer = new StringBuilder();
        writer.Append("<section class=\"cfx-graph-explorer\"");
        Attribute(writer, "data-cfx-graph-id", scene.Id);
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
        WriteStage(writer, scene, options, positions, graphId);
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
            WriteFilter(writer, "kind", scene.Nodes.Select(node => node.Kind).Concat(scene.Edges.Select(edge => edge.Kind)).Concat(scene.Clusters.Select(cluster => cluster.Kind)));
        }

        if (options.IncludeClusterControls && scene.Clusters.Count > 0 && scene.Options.HasFeature(GraphSceneFeatures.Clustering)) WriteButton(writer, "clusters", "Clusters");
        if (scene.Options.HasFeature(GraphSceneFeatures.NeighborhoodFocus)) WriteButton(writer, "focus", "Focus");
        if (scene.Options.HasFeature(GraphSceneFeatures.MultiSelection)) WriteButton(writer, "clear-selection", "Clear");
        if (scene.Options.HasFeature(GraphSceneFeatures.Viewport)) {
            WriteButton(writer, "fit", "Fit");
            WriteButton(writer, "zoom-in", "+");
            WriteButton(writer, "zoom-out", "-");
        }

        if (options.IncludePhysicsControls && scene.Options.HasFeature(GraphSceneFeatures.RuntimePhysics)) {
            WriteButton(writer, "physics", "Physics");
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
        writer.Append("<div class=\"cfx-graph-stage\"><canvas class=\"cfx-graph-canvas\" data-cfx-role=\"graph-canvas\" width=\"960\" height=\"560\"></canvas>");
        writer.Append("<svg class=\"cfx-graph-svg\" data-cfx-role=\"graph-scene\" width=\"960\" height=\"560\" viewBox=\"0 0 960 560\" role=\"img\"");
        Attribute(writer, "aria-labelledby", graphId + "-title");
        writer.Append("><title");
        Attribute(writer, "id", graphId + "-title");
        writer.Append('>');
        writer.Append(Text(scene.Title));
        writer.Append("</title><defs><marker");
        Attribute(writer, "id", graphId + "-arrow");
        writer.Append(" viewBox=\"0 0 10 10\" refX=\"9\" refY=\"5\" markerWidth=\"6\" markerHeight=\"6\" orient=\"auto-start-reverse\"><path d=\"M 0 0 L 10 5 L 0 10 z\"></path></marker></defs>");
        writer.Append("<g data-cfx-role=\"graph-viewport\">");
        WriteClusters(writer, scene, positions);
        WriteEdges(writer, scene, positions, graphId + "-arrow");
        WriteEdgeLabels(writer, scene, positions);
        WriteNodes(writer, scene, positions);
        writer.Append("</g></svg></div>");
    }

    private static void WriteClusters(StringBuilder writer, GraphScene scene, IReadOnlyDictionary<string, Point> positions) {
        foreach (var cluster in scene.Clusters) {
            var members = cluster.NodeIds.Where(positions.ContainsKey).Select(id => positions[id]).ToArray();
            var x = members.Length == 0 ? Width / 2 : members.Average(point => point.X);
            var y = members.Length == 0 ? Height / 2 : members.Average(point => point.Y);
            writer.Append("<g class=\"cfx-graph-cluster\" tabindex=\"0\" data-cfx-role=\"graph-cluster\"");
            Attribute(writer, "data-cluster-id", cluster.Id);
            Attribute(writer, "data-cluster-label", cluster.Label);
            Attribute(writer, "data-cluster-kind", cluster.Kind);
            Attribute(writer, "data-cluster-node-ids", string.Join(",", cluster.NodeIds));
            Attribute(writer, "data-cluster-collapsed", cluster.Collapsed ? "true" : "false");
            Attribute(writer, "data-cfx-status", cluster.Kind);
            Attribute(writer, "data-cfx-search", SearchText(cluster.Metadata));
            Attribute(writer, "transform", "translate(" + Number(x) + " " + Number(y) + ")");
            writer.Append("><circle r=\"42\"></circle><text y=\"5\">");
            writer.Append(Text(cluster.Label));
            writer.Append("</text></g>");
        }
    }

    private static void WriteEdges(StringBuilder writer, GraphScene scene, IReadOnlyDictionary<string, Point> positions, string markerId) {
        var nodeSizes = scene.Nodes.ToDictionary(node => node.Id, node => node.Size, StringComparer.Ordinal);
        foreach (var edge in scene.Edges) {
            if (!positions.TryGetValue(edge.SourceNodeId, out var source) || !positions.TryGetValue(edge.TargetNodeId, out var target)) continue;
            var targetSize = nodeSizes.TryGetValue(edge.TargetNodeId, out var size) ? size : 8;
            var path = EdgePath(edge, source, target, targetSize);
            writer.Append("<path class=\"cfx-graph-edge\" tabindex=\"0\" data-cfx-role=\"graph-edge\"");
            Attribute(writer, "data-edge-id", edge.Id);
            Attribute(writer, "data-edge-label", edge.Label);
            Attribute(writer, "data-edge-kind", edge.Kind);
            Attribute(writer, "data-cfx-status", edge.Status);
            Attribute(writer, "data-source-node-id", edge.SourceNodeId);
            Attribute(writer, "data-target-node-id", edge.TargetNodeId);
            Attribute(writer, "data-edge-weight", Number(edge.Weight));
            Attribute(writer, "data-edge-length", Number(edge.Length));
            Attribute(writer, "data-edge-directed", edge.Directed ? "true" : "false");
            Attribute(writer, "data-edge-shape", EdgeShape(edge.Shape));
            Attribute(writer, "data-edge-curvature", Number(edge.Curvature));
            Attribute(writer, "data-edge-dashed", edge.Dashed ? "true" : "false");
            Attribute(writer, "data-edge-show-label", edge.ShowLabel ? "true" : "false");
            Attribute(writer, "data-cfx-search", SearchText(edge.Metadata));
            Attribute(writer, "d", path);
            if (edge.Directed) Attribute(writer, "marker-end", "url(#" + markerId + ")");
            writer.Append("></path>");
        }
    }

    private static void WriteEdgeLabels(StringBuilder writer, GraphScene scene, IReadOnlyDictionary<string, Point> positions) {
        foreach (var edge in scene.Edges.Where(edge => edge.ShowLabel && !string.IsNullOrWhiteSpace(edge.Label))) {
            if (!positions.TryGetValue(edge.SourceNodeId, out var source) || !positions.TryGetValue(edge.TargetNodeId, out var target)) continue;
            var point = EdgeLabelPoint(edge, source, target);
            writer.Append("<text class=\"cfx-graph-edge-label\" data-cfx-role=\"graph-edge-label\"");
            Attribute(writer, "data-edge-label-for", edge.Id);
            Attribute(writer, "x", Number(point.X));
            Attribute(writer, "y", Number(point.Y));
            writer.Append('>');
            writer.Append(Text(edge.Label!));
            writer.Append("</text>");
        }
    }

    private static void WriteNodes(StringBuilder writer, GraphScene scene, IReadOnlyDictionary<string, Point> positions) {
        foreach (var node in scene.Nodes) {
            var point = positions[node.Id];
            writer.Append("<g class=\"cfx-graph-node\" tabindex=\"0\" data-cfx-role=\"graph-node\"");
            Attribute(writer, "data-node-id", node.Id);
            Attribute(writer, "data-node-label", node.Label);
            Attribute(writer, "data-node-kind", node.Kind);
            Attribute(writer, "data-node-group", node.GroupId);
            Attribute(writer, "data-node-cluster", node.ClusterId);
            Attribute(writer, "data-cfx-status", node.Status);
            Attribute(writer, "data-node-size", Number(node.Size));
            Attribute(writer, "data-node-fixed", node.Fixed ? "true" : "false");
            Attribute(writer, "data-node-shape", NodeShape(node.Shape));
            Attribute(writer, "data-node-image-url", node.ImageUrl);
            Attribute(writer, "data-node-icon", node.IconText);
            Attribute(writer, "data-cfx-search", SearchText(node.Metadata));
            Attribute(writer, "data-node-x", Number(point.X));
            Attribute(writer, "data-node-y", Number(point.Y));
            Attribute(writer, "transform", "translate(" + Number(point.X) + " " + Number(point.Y) + ")");
            writer.Append('>');
            WriteNodeMark(writer, node);
            writer.Append("<text y=\"");
            writer.Append(Number(node.Size + 18));
            writer.Append("\">");
            writer.Append(Text(node.Label));
            writer.Append("</text></g>");
        }
    }

    private static void WriteNodeMark(StringBuilder writer, GraphSceneNode node) {
        var size = Math.Max(4, node.Size);
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

    private static void WriteButton(StringBuilder writer, string action, string label) {
        writer.Append("<button class=\"cfx-graph-tool\" type=\"button\"");
        Attribute(writer, "data-cfx-graph-action", action);
        writer.Append(" aria-pressed=\"false\">");
        writer.Append(Text(label));
        writer.Append("</button>");
    }

    private static string SearchText(IReadOnlyDictionary<string, string> metadata) {
        if (metadata.Count == 0) return string.Empty;
        return string.Join(" ", metadata.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Key + " " + pair.Value));
    }

    private static IReadOnlyDictionary<string, Point> ComputePositions(GraphScene scene) {
        var positions = new Dictionary<string, Point>(StringComparer.Ordinal);
        var nodes = scene.Nodes;
        for (var i = 0; i < nodes.Count; i++) {
            var node = nodes[i];
            if (node.HasExplicitPosition) {
                positions[node.Id] = new Point(node.X, node.Y);
            }
        }

        var generated = nodes.Where(node => !node.HasExplicitPosition).ToArray();
        if (generated.Length == 0) return positions;

        var adjacency = BuildAdjacency(scene);
        var components = ConnectedComponents(nodes, adjacency);
        var centers = ComponentCenters(components);
        for (var i = 0; i < components.Count; i++) {
            PlaceComponent(components[i], centers[i], adjacency, positions);
        }

        if (positions.Count == generated.Length) NormalizeGeneratedPositions(positions, generated);
        return positions;
    }

    private static Dictionary<string, List<string>> BuildAdjacency(GraphScene scene) {
        var adjacency = scene.Nodes.ToDictionary(node => node.Id, _ => new List<string>(), StringComparer.Ordinal);
        foreach (var edge in scene.Edges) {
            if (!adjacency.ContainsKey(edge.SourceNodeId) || !adjacency.ContainsKey(edge.TargetNodeId)) continue;
            adjacency[edge.SourceNodeId].Add(edge.TargetNodeId);
            adjacency[edge.TargetNodeId].Add(edge.SourceNodeId);
        }

        return adjacency;
    }

    private static List<List<GraphSceneNode>> ConnectedComponents(IReadOnlyList<GraphSceneNode> nodes, IReadOnlyDictionary<string, List<string>> adjacency) {
        var byId = nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var components = new List<List<GraphSceneNode>>();
        foreach (var start in nodes.OrderByDescending(node => Degree(node.Id, adjacency)).ThenBy(node => node.Id, StringComparer.Ordinal)) {
            if (!visited.Add(start.Id)) continue;
            var component = new List<GraphSceneNode>();
            var queue = new Queue<string>();
            queue.Enqueue(start.Id);
            while (queue.Count > 0) {
                var id = queue.Dequeue();
                if (!byId.TryGetValue(id, out var node)) continue;
                component.Add(node);
                if (!adjacency.TryGetValue(id, out var neighbors)) continue;
                foreach (var neighbor in neighbors.OrderBy(value => value, StringComparer.Ordinal)) {
                    if (visited.Add(neighbor)) queue.Enqueue(neighbor);
                }
            }

            components.Add(component);
        }

        return components
            .OrderByDescending(component => component.Count)
            .ThenBy(component => component[0].Id, StringComparer.Ordinal)
            .ToList();
    }

    private static List<Point> ComponentCenters(IReadOnlyList<List<GraphSceneNode>> components) {
        var centers = new List<Point>(components.Count);
        for (var i = 0; i < components.Count; i++) {
            if (i == 0) {
                centers.Add(new Point(Width / 2, Height / 2));
                continue;
            }

            var angle = GoldenAngle(i - 1) - Math.PI / 2;
            var ring = 135 + 76 * Math.Sqrt(i);
            centers.Add(new Point(Width / 2 + Math.Cos(angle) * ring, Height / 2 + Math.Sin(angle) * ring * 0.66));
        }

        return centers;
    }

    private static void PlaceComponent(IReadOnlyList<GraphSceneNode> component, Point fallbackCenter, IReadOnlyDictionary<string, List<string>> adjacency, IDictionary<string, Point> positions) {
        if (component.Count == 0) return;
        var explicitMembers = component.Where(node => node.HasExplicitPosition && positions.ContainsKey(node.Id)).ToArray();
        var center = explicitMembers.Length == 0
            ? fallbackCenter
            : new Point(explicitMembers.Average(node => positions[node.Id].X), explicitMembers.Average(node => positions[node.Id].Y));
        var generated = component.Where(node => !node.HasExplicitPosition).ToArray();
        if (generated.Length == 0) return;
        if (generated.Length == 1) {
            positions[generated[0].Id] = new Point(center.X + StableOffset(generated[0].Id, 13), center.Y + StableOffset(generated[0].Id + ":y", 10));
            return;
        }

        var depths = NodeDepths(component, adjacency);
        var maxDegree = Math.Max(1, component.Max(node => Degree(node.Id, adjacency)));
        var hubLimit = Math.Max(1, Math.Min(4, (int)Math.Ceiling(Math.Sqrt(component.Count) / 2)));
        var hubs = component
            .Where(node => Degree(node.Id, adjacency) >= Math.Max(2, maxDegree * 0.68))
            .OrderByDescending(node => Degree(node.Id, adjacency))
            .ThenBy(node => node.Id, StringComparer.Ordinal)
            .Take(hubLimit)
            .ToArray();
        if (hubs.Length == 0) hubs = component.OrderByDescending(node => Degree(node.Id, adjacency)).ThenBy(node => node.Id, StringComparer.Ordinal).Take(1).ToArray();

        var communityAngles = CommunityAngles(component);
        var communityCounts = component.GroupBy(CommunityKey, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var communityRanks = new Dictionary<string, int>(StringComparer.Ordinal);
        var componentRadius = Math.Max(72, Math.Sqrt(component.Count) * 38);
        foreach (var node in generated.OrderByDescending(node => hubs.Any(hub => string.Equals(hub.Id, node.Id, StringComparison.Ordinal))).ThenBy(node => depths[node.Id]).ThenByDescending(node => Degree(node.Id, adjacency)).ThenBy(node => node.Id, StringComparer.Ordinal)) {
            var hubIndex = Array.FindIndex(hubs, hub => string.Equals(hub.Id, node.Id, StringComparison.Ordinal));
            if (hubIndex >= 0) {
                var hubRadius = hubs.Length == 1 ? 0 : 22 + hubIndex * 7;
                var hubAngle = GoldenAngle(hubIndex);
                positions[node.Id] = new Point(center.X + Math.Cos(hubAngle) * hubRadius, center.Y + Math.Sin(hubAngle) * hubRadius * 0.72);
                continue;
            }

            var key = CommunityKey(node);
            communityRanks.TryGetValue(key, out var rank);
            communityRanks[key] = rank + 1;
            var count = Math.Max(1, communityCounts[key]);
            var depth = Math.Max(1, depths[node.Id]);
            var sectorWidth = communityAngles.Count <= 1 ? Math.PI * 1.75 : Math.Min(Math.PI * 0.82, Math.PI * 2 / communityAngles.Count * 0.72);
            var rankOffset = ((rank + 0.5) / count - 0.5) * sectorWidth;
            var angle = communityAngles[key] + rankOffset + StableOffset(node.Id + ":angle", 0.11);
            var radius = Math.Min(componentRadius, 48 + depth * 46 + Math.Sqrt(rank + 1) * 16 + StableOffset(node.Id + ":radius", 9));
            positions[node.Id] = new Point(center.X + Math.Cos(angle) * radius, center.Y + Math.Sin(angle) * radius * 0.74);
        }
    }

    private static Dictionary<string, int> NodeDepths(IReadOnlyList<GraphSceneNode> component, IReadOnlyDictionary<string, List<string>> adjacency) {
        var componentIds = new HashSet<string>(component.Select(node => node.Id), StringComparer.Ordinal);
        var ordered = component.OrderByDescending(node => Degree(node.Id, adjacency)).ThenBy(node => node.Id, StringComparer.Ordinal).ToArray();
        var depths = component.ToDictionary(node => node.Id, _ => int.MaxValue, StringComparer.Ordinal);
        var queue = new Queue<string>();
        foreach (var node in ordered.Take(Math.Max(1, Math.Min(3, (int)Math.Ceiling(Math.Sqrt(component.Count) / 3))))) {
            depths[node.Id] = 0;
            queue.Enqueue(node.Id);
        }

        while (queue.Count > 0) {
            var id = queue.Dequeue();
            if (!adjacency.TryGetValue(id, out var neighbors)) continue;
            foreach (var neighbor in neighbors.OrderBy(value => value, StringComparer.Ordinal)) {
                if (!componentIds.Contains(neighbor) || depths[neighbor] <= depths[id] + 1) continue;
                depths[neighbor] = depths[id] + 1;
                queue.Enqueue(neighbor);
            }
        }

        foreach (var node in component) if (depths[node.Id] == int.MaxValue) depths[node.Id] = 2;
        return depths;
    }

    private static Dictionary<string, double> CommunityAngles(IReadOnlyList<GraphSceneNode> component) {
        var groups = component
            .GroupBy(CommunityKey, StringComparer.Ordinal)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .ToArray();
        var angles = new Dictionary<string, double>(StringComparer.Ordinal);
        for (var i = 0; i < groups.Length; i++) {
            var angle = groups.Length == 1 ? -Math.PI / 2 : -Math.PI / 2 + Math.PI * 2 * i / groups.Length;
            angles[groups[i].Key] = angle;
        }

        return angles;
    }

    private static void NormalizeGeneratedPositions(IDictionary<string, Point> positions, IReadOnlyList<GraphSceneNode> generated) {
        var points = generated.Select(node => positions[node.Id]).ToArray();
        var minX = points.Min(point => point.X);
        var maxX = points.Max(point => point.X);
        var minY = points.Min(point => point.Y);
        var maxY = points.Max(point => point.Y);
        var width = Math.Max(1, maxX - minX);
        var height = Math.Max(1, maxY - minY);
        var targetWidth = Width - 150;
        var targetHeight = Height - 130;
        var scale = Math.Min(1, Math.Min(targetWidth / width, targetHeight / height));
        if (scale >= 1) return;
        foreach (var node in generated) {
            var point = positions[node.Id];
            positions[node.Id] = new Point(Width / 2 + (point.X - Width / 2) * scale, Height / 2 + (point.Y - Height / 2) * scale);
        }
    }

    private static string CommunityKey(GraphSceneNode node) {
        if (!string.IsNullOrWhiteSpace(node.ClusterId)) return "cluster:" + node.ClusterId;
        if (!string.IsNullOrWhiteSpace(node.GroupId)) return "group:" + node.GroupId;
        if (!string.IsNullOrWhiteSpace(node.Kind)) return "kind:" + node.Kind;
        return "graph";
    }

    private static int Degree(string nodeId, IReadOnlyDictionary<string, List<string>> adjacency) => adjacency.TryGetValue(nodeId, out var neighbors) ? neighbors.Count : 0;

    private static double GoldenAngle(int index) => index * 2.39996322972865332;

    private static double StableOffset(string value, double amplitude) => (StableUnit(value) - 0.5) * 2 * amplitude;

    private static double StableUnit(string value) {
        unchecked {
            var hash = 2166136261u;
            foreach (var ch in value) {
                hash ^= ch;
                hash *= 16777619u;
            }

            return (hash & 0x00ffffff) / 16777215.0;
        }
    }

    private static string EdgePath(GraphSceneEdge edge, Point source, Point target, double targetSize) {
        var control = EdgeControl(edge, source, target);
        var renderTarget = DirectedTargetPoint(edge, source, target, control, targetSize);
        return control.HasValue
            ? "M " + Number(source.X) + " " + Number(source.Y) + " Q " + Number(control.Value.X) + " " + Number(control.Value.Y) + " " + Number(renderTarget.X) + " " + Number(renderTarget.Y)
            : "M " + Number(source.X) + " " + Number(source.Y) + " L " + Number(renderTarget.X) + " " + Number(renderTarget.Y);
    }

    private static Point EdgeLabelPoint(GraphSceneEdge edge, Point source, Point target) {
        var control = EdgeControl(edge, source, target);
        return control.HasValue
            ? new Point((source.X + 2 * control.Value.X + target.X) / 4, (source.Y + 2 * control.Value.Y + target.Y) / 4 - 7)
            : new Point((source.X + target.X) / 2, (source.Y + target.Y) / 2 - 7);
    }

    private static Point? EdgeControl(GraphSceneEdge edge, Point source, Point target) {
        if (edge.Shape != GraphEdgeShape.Curve && Math.Abs(edge.Curvature) < 0.001) return null;
        var dx = target.X - source.X;
        var dy = target.Y - source.Y;
        var length = Math.Max(1, Math.Sqrt(dx * dx + dy * dy));
        var offset = Math.Abs(edge.Curvature) < 0.001 ? 34 : edge.Curvature;
        return new Point((source.X + target.X) / 2 - dy / length * offset, (source.Y + target.Y) / 2 + dx / length * offset);
    }

    private static Point DirectedTargetPoint(GraphSceneEdge edge, Point source, Point target, Point? control, double targetSize) {
        if (!edge.Directed) return target;
        var from = control ?? source;
        var dx = target.X - from.X;
        var dy = target.Y - from.Y;
        var length = Math.Max(1, Math.Sqrt(dx * dx + dy * dy));
        var inset = Math.Max(6, targetSize + 7);
        return new Point(target.X - dx / length * inset, target.Y - dy / length * inset);
    }

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
        HtmlGraphRenderBackend.WebGl => "webgl",
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
        foreach (var ch in value) builder.Append(char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' ? ch : '-');
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
