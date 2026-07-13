using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChartForgeX.Interactivity;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlGraphExplorerRenderer {
    /// <summary>Renders a complete graph or planned hierarchy stage as deterministic, script-free SVG.</summary>
    public string RenderStaticSvg(GraphScene scene, GraphSceneStage? stage = null, Action<GraphSceneStaticRenderOptions>? configure = null) {
        if (scene == null) throw new ArgumentNullException(nameof(scene));
        var options = StaticOptions(configure);
        var projected = ProjectStaticScene(scene, stage);
        var body = RenderStaticSvgBody(projected, options);
        return "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"" + options.Width + "\" height=\"" + options.Height + "\" viewBox=\"0 0 960 560\" role=\"img\" aria-label=\"" + Text(projected.Title) + "\">" + body + "</svg>";
    }

    /// <summary>Renders a complete graph or planned hierarchy stage as deterministic PNG bytes.</summary>
    public byte[] RenderStaticPng(GraphScene scene, GraphSceneStage? stage = null, Action<GraphSceneStaticRenderOptions>? configure = null) {
        if (scene == null) throw new ArgumentNullException(nameof(scene));
        var options = StaticOptions(configure);
        var projected = ProjectStaticScene(scene, stage);
        var body = RenderStaticSvgBody(projected, options);
        if (!SvgRasterRenderer.TryRenderFragment(body, "0 0 960 560", "xMidYMid meet", options.Width, options.Height, out var rgba)) throw new InvalidOperationException("Static graph SVG could not be rasterized.");
        CompositeTransparentPixels(rgba, 255, 255, 255);
        return PngWriter.WriteRgba(options.Width, options.Height, rgba);
    }

    private static GraphSceneStaticRenderOptions StaticOptions(Action<GraphSceneStaticRenderOptions>? configure) {
        var options = new GraphSceneStaticRenderOptions();
        configure?.Invoke(options);
        options.Validate();
        return options;
    }

    private static string RenderStaticSvgBody(GraphScene scene, GraphSceneStaticRenderOptions options) {
        var writer = new StringBuilder();
        var positions = ComputePositions(scene);
        var labeledNodeIds = SelectStaticNodeLabels(scene, options.MaximumNodeLabels);
        var clusters = scene.GetEffectiveClusters();
        var clusterMembership = BuildClusterMembership(scene, clusters);
        var markerId = SafeId(scene.Id) + "-static-arrow";
        writer.Append("<style>");
        writer.Append(HtmlGraphExplorerAssets.Style);
        writer.Append("</style><title>");
        writer.Append(Text(scene.Title));
        writer.Append("</title>");
        WriteArrowMarkers(writer, scene, markerId);
        writer.Append("<rect class=\"cfx-graph-bg\" width=\"960\" height=\"560\" style=\"fill:#ffffff\"></rect><g data-cfx-role=\"graph-viewport\"");
        Attribute(writer, "transform", StaticViewportTransform(scene, positions, labeledNodeIds));
        writer.Append('>');
        if (clusters.Count > 0) WriteClusters(writer, scene, clusters, positions, clusterMembership, false, false);
        var emptyPositions = new Dictionary<string, Point>(StringComparer.Ordinal);
        var emptyIds = new HashSet<string>(StringComparer.Ordinal);
        WriteEdges(writer, scene, positions, clusterMembership, emptyPositions, new Dictionary<string, double>(StringComparer.Ordinal), markerId, emptyIds, false);
        WriteEdgeLabels(writer, scene, positions, emptyPositions, new Dictionary<string, double>(StringComparer.Ordinal), emptyIds);
        WriteNodes(writer, scene, positions, clusterMembership, emptyIds, false, false, labeledNodeIds);
        writer.Append("</g>");
        return writer.ToString();
    }

    private static string StaticViewportTransform(GraphScene scene, IReadOnlyDictionary<string, Point> positions, ISet<string> labeledNodeIds) {
        if (scene.Nodes.Count == 0) return "translate(0 0) scale(1)";
        var minX = double.PositiveInfinity; var minY = double.PositiveInfinity; var maxX = double.NegativeInfinity; var maxY = double.NegativeInfinity;
        foreach (var node in scene.Nodes) {
            var point = positions[node.Id];
            var includeLabel = labeledNodeIds.Contains(node.Id);
            var halfWidth = PreparedNodeHalfWidth(node, includeLabel);
            var halfHeight = PreparedNodeHalfHeight(node, includeLabel);
            minX = Math.Min(minX, point.X - halfWidth); maxX = Math.Max(maxX, point.X + halfWidth);
            minY = Math.Min(minY, point.Y - halfHeight); maxY = Math.Max(maxY, point.Y + halfHeight);
        }
        var contentWidth = Math.Max(1, maxX - minX);
        var contentHeight = Math.Max(1, maxY - minY);
        var scale = Math.Min((Width - 120) / contentWidth, (Height - 100) / contentHeight);
        scale = Math.Max(0.05, Math.Min(scene.Nodes.Count <= 20 ? 3 : scene.Nodes.Count <= 100 ? 1.8 : 1.15, scale));
        var centerX = (minX + maxX) / 2;
        var centerY = (minY + maxY) / 2;
        return "translate(" + Number(Width / 2 - centerX * scale) + " " + Number(Height / 2 - centerY * scale) + ") scale(" + Number(scale) + ")";
    }

    private static ISet<string> SelectStaticNodeLabels(GraphScene scene, int maximumLabels) {
        if (maximumLabels >= scene.Nodes.Count) return new HashSet<string>(scene.Nodes.Select(node => node.Id), StringComparer.Ordinal);
        if (maximumLabels == 0) return new HashSet<string>(StringComparer.Ordinal);
        var degree = scene.Nodes.ToDictionary(node => node.Id, _ => 0, StringComparer.Ordinal);
        foreach (var edge in scene.Edges) { degree[edge.SourceNodeId]++; degree[edge.TargetNodeId]++; }
        var order = BuildHierarchyOrder(scene);
        var selected = new HashSet<string>(StringComparer.Ordinal);
        foreach (var root in scene.Nodes.Where(node => string.IsNullOrWhiteSpace(node.ParentId)).OrderBy(node => order[node.Id])) {
            if (selected.Count == maximumLabels) return selected;
            selected.Add(root.Id);
        }

        var frontier = scene.Nodes.Where(node => node.Metadata.ContainsKey("stageHiddenDescendants")).ToArray();
        if (scene.Nodes.Count >= 500 && frontier.Length == 0) {
            var byId = scene.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
            var depths = new Dictionary<string, int>(StringComparer.Ordinal);
            var visiting = new HashSet<string>(StringComparer.Ordinal);
            foreach (var node in scene.Nodes) ResolveStaticDepth(node, byId, depths, visiting);
            var maximumDepth = depths.Values.DefaultIfEmpty(0).Max();
            var firstLabeledDepth = Math.Max(1, maximumDepth - 2);
            var levels = scene.Nodes.Where(node => depths[node.Id] >= firstLabeledDepth).GroupBy(node => depths[node.Id]).OrderBy(group => group.Key).ToArray();
            for (var index = 0; index < levels.Length && selected.Count < maximumLabels; index++) {
                var remainingLevels = levels.Length - index;
                var allocation = (int)Math.Ceiling((maximumLabels - selected.Count) / (double)remainingLevels);
                AddEvenlySampledLabels(levels[index], selected.Count + allocation, order, selected);
            }
            return selected;
        }

        AddEvenlySampledLabels(frontier, maximumLabels, order, selected);
        foreach (var node in scene.Nodes.OrderByDescending(node => degree[node.Id]).ThenBy(node => order[node.Id]).ThenBy(node => node.Id, StringComparer.Ordinal)) {
            if (selected.Count == maximumLabels) break;
            selected.Add(node.Id);
        }
        return selected;
    }

    private static void AddEvenlySampledLabels(IEnumerable<GraphSceneNode> candidates, int maximumLabels, IReadOnlyDictionary<string, int> order, ISet<string> selected) {
        var source = candidates.Where(node => !selected.Contains(node.Id)).ToArray();
        var available = source.Length > 0 && source.All(node => node.HasExplicitPosition)
            ? source.OrderBy(node => Math.Atan2(node.Y - Height / 2, node.X - Width / 2)).ThenBy(node => Math.Abs(node.X - Width / 2) + Math.Abs(node.Y - Height / 2)).ThenBy(node => node.Id, StringComparer.Ordinal).ToArray()
            : source.OrderBy(node => order[node.Id]).ThenBy(node => node.Id, StringComparer.Ordinal).ToArray();
        var count = Math.Min(maximumLabels - selected.Count, available.Length);
        if (count <= 0) return;
        var rotation = StableUnit((available[0].Kind ?? available[0].Id) + ":static-label-rotation");
        for (var index = 0; index < count; index++) {
            var unit = ((index + 0.5) / count + rotation) % 1;
            var candidateIndex = Math.Min(available.Length - 1, (int)Math.Floor(unit * available.Length));
            selected.Add(available[candidateIndex].Id);
        }
    }

    private static GraphScene ProjectStaticScene(GraphScene scene, GraphSceneStage? stage) {
        scene.Validate();
        var visible = stage == null
            ? new HashSet<string>(scene.Nodes.Select(node => node.Id), StringComparer.Ordinal)
            : new HashSet<string>(stage.VisibleNodeIds, StringComparer.Ordinal);
        if (visible.Any(id => scene.Nodes.All(node => !string.Equals(node.Id, id, StringComparison.Ordinal)))) throw new InvalidOperationException("Graph scene stage contains a node that is not present in the rendered scene.");
        var projected = GraphScene.Create(scene.Id + (stage == null ? "-static" : "-stage-" + stage.Index), stage == null ? scene.Title : scene.Title + " - " + stage.Name);
        projected.Subtitle = scene.Subtitle;
        CopyDictionary(scene.Metadata, projected.Metadata);
        var frontier = stage == null ? new HashSet<string>(StringComparer.Ordinal) : new HashSet<string>(stage.FrontierNodeIds, StringComparer.Ordinal);
        var hiddenDescendants = stage == null ? new Dictionary<string, int>(StringComparer.Ordinal) : HiddenDescendantCounts(scene, visible, frontier);
        foreach (var source in scene.Nodes.Where(node => visible.Contains(node.Id))) projected.Nodes.Add(CopyNode(source, visible, hiddenDescendants));
        foreach (var source in scene.Edges.Where(edge => visible.Contains(edge.SourceNodeId) && visible.Contains(edge.TargetNodeId))) projected.Edges.Add(CopyEdge(source));
        // A hierarchy stage already is a deliberate clustering projection. Preserve explicit
        // cluster contracts without adding adaptive group hulls that compete with that story.
        foreach (var source in scene.Clusters) {
            var members = source.NodeIds.Where(visible.Contains).Distinct(StringComparer.Ordinal).ToArray();
            if (members.Length < 2) continue;
            var cluster = new GraphSceneCluster { Id = source.Id, Label = source.Label, Kind = source.Kind, Collapsed = false };
            cluster.NodeIds.AddRange(members);
            CopyDictionary(source.Metadata, cluster.Metadata);
            projected.Clusters.Add(cluster);
        }
        projected.Options.Layout.Mode = scene.Options.Layout.Mode;
        projected.Options.Layout.Direction = scene.Options.Layout.Direction;
        projected.Options.Layout.LevelSeparation = scene.Options.Layout.LevelSeparation;
        projected.Options.Layout.NodeSpacing = scene.Options.Layout.NodeSpacing;
        projected.Options.Layout.ComponentSpacing = scene.Options.Layout.ComponentSpacing;
        projected.Options.Layout.InferLevelsFromEdges = scene.Options.Layout.InferLevelsFromEdges;
        // A single hierarchy rank stops being readable well before it becomes a "large" graph.
        // Dense static stages use concentric hierarchy bands: parent order stays deterministic,
        // leaf-heavy levels can occupy several rings, and the full graph remains inspectable.
        if (projected.Nodes.Count >= 40) ApplyStaticHierarchyBands(projected);
        projected.Options.Disable(GraphSceneFeatures.RuntimePhysics | GraphSceneFeatures.Stabilization | GraphSceneFeatures.DragNodes | GraphSceneFeatures.IncrementalUpdates | GraphSceneFeatures.Manipulation);
        projected.Validate();
        return projected;
    }

    private static void ApplyStaticHierarchyBands(GraphScene scene) {
        var byId = scene.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var depths = new Dictionary<string, int>(StringComparer.Ordinal);
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in scene.Nodes) ResolveStaticDepth(node, byId, depths, visiting);
        var maximumDepth = Math.Max(1, depths.Values.DefaultIfEmpty(0).Max());
        var hierarchyOrder = BuildHierarchyOrder(scene);
        var levelStep = 224d / maximumDepth;
        foreach (var level in scene.Nodes.GroupBy(node => depths[node.Id]).OrderBy(group => group.Key)) {
            var nodes = level.OrderBy(node => hierarchyOrder[node.Id]).ThenBy(node => node.Id, StringComparer.Ordinal).ToArray();
            var depth = level.Key;
            foreach (var node in nodes) {
                node.Size = Math.Min(node.Size, StaticHierarchyNodeSize(scene.Nodes.Count, depth));
                if (node.Size <= 5) node.IconText = null;
            }
            if (depth == 0 && nodes.Length == 1) {
                nodes[0].X = Width / 2;
                nodes[0].Y = Height / 2;
                continue;
            }

            var centerRadius = Math.Max(22, levelStep * depth);
            var minimumArc = Math.Max(4, nodes.Min(node => node.Size) * 2 + 2);
            var capacity = Math.Max(1, (int)Math.Floor(Math.PI * 2 * centerRadius / minimumArc));
            var ringCount = Math.Max(1, (int)Math.Ceiling(nodes.Length / (double)capacity));
            var bandHalfWidth = Math.Max(0, levelStep * 0.4);
            var ringSpacing = ringCount == 1 ? 0 : Math.Min(minimumArc, bandHalfWidth * 2 / (ringCount - 1));
            var firstRadius = centerRadius - ringSpacing * (ringCount - 1) / 2;
            var offset = 0;
            for (var ring = 0; ring < ringCount; ring++) {
                var remaining = nodes.Length - offset;
                var remainingRings = ringCount - ring;
                var count = (int)Math.Ceiling(remaining / (double)remainingRings);
                var radius = Math.Max(18, firstRadius + ring * ringSpacing);
                var rotation = -Math.PI / 2 + (ring % 2 == 0 ? 0 : Math.PI / Math.Max(1, count));
                for (var index = 0; index < count; index++) {
                    var node = nodes[offset + index];
                    var angle = rotation + Math.PI * 2 * index / count;
                    node.X = Width / 2 + Math.Cos(angle) * radius;
                    node.Y = Height / 2 + Math.Sin(angle) * radius;
                }
                offset += count;
            }
        }
    }

    private static int ResolveStaticDepth(GraphSceneNode node, IReadOnlyDictionary<string, GraphSceneNode> byId, IDictionary<string, int> depths, ISet<string> visiting) {
        if (depths.TryGetValue(node.Id, out var known)) return known;
        if (!visiting.Add(node.Id)) return 0;
        var depth = string.IsNullOrWhiteSpace(node.ParentId) || !byId.TryGetValue(node.ParentId!, out var parent)
            ? 0
            : ResolveStaticDepth(parent, byId, depths, visiting) + 1;
        visiting.Remove(node.Id);
        depths[node.Id] = depth;
        return depth;
    }

    private static double StaticHierarchyNodeSize(int nodeCount, int depth) {
        if (nodeCount >= 1000) return depth switch { 0 => 16, 1 => 12, 2 => 8, 3 => 5, 4 => 3.2, _ => 2 };
        if (nodeCount >= 200) return depth switch { 0 => 16, 1 => 12, 2 => 9, _ => 5 };
        return depth switch { 0 => 16, 1 => 13, _ => 9 };
    }

    private static Dictionary<string, int> HiddenDescendantCounts(GraphScene scene, ISet<string> visible, ISet<string> frontier) {
        var children = scene.Nodes.ToDictionary(node => node.Id, _ => new List<string>(), StringComparer.Ordinal);
        foreach (var node in scene.Nodes) if (!string.IsNullOrWhiteSpace(node.ParentId)) children[node.ParentId!].Add(node.Id);
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var node in scene.Nodes.Where(node => frontier.Contains(node.Id))) {
            var count = 0;
            var pending = new Stack<string>(children[node.Id]);
            while (pending.Count > 0) {
                var id = pending.Pop();
                if (!visible.Contains(id)) count++;
                foreach (var child in children[id]) pending.Push(child);
            }
            if (count > 0) result[node.Id] = count;
        }
        return result;
    }

    private static GraphSceneNode CopyNode(GraphSceneNode source, ISet<string> visible, IReadOnlyDictionary<string, int> hiddenDescendants) {
        var node = new GraphSceneNode {
            Id = source.Id, Label = source.Label, Kind = source.Kind, GroupId = source.GroupId, Status = source.Status, Shape = source.Shape, Level = source.Level,
            ImageUrl = source.ImageUrl, ImageAlt = source.ImageAlt, IconText = source.IconText, SecondaryLabel = source.SecondaryLabel, BadgeText = source.BadgeText,
            Size = source.Size, Fixed = source.Fixed, Hidden = source.Hidden
        };
        if (!string.IsNullOrWhiteSpace(source.ParentId) && visible.Contains(source.ParentId!)) node.ParentId = source.ParentId;
        if (source.HasExplicitPosition) { node.X = source.X; node.Y = source.Y; }
        node.Style.BackgroundColor = source.Style.BackgroundColor; node.Style.BorderColor = source.Style.BorderColor; node.Style.LabelColor = source.Style.LabelColor; node.Style.LabelBackgroundColor = source.Style.LabelBackgroundColor; node.Style.Shadow = source.Style.Shadow;
        CopyDictionary(source.Metadata, node.Metadata);
        if (hiddenDescendants.TryGetValue(source.Id, out var hidden)) {
            node.BadgeText = "+" + hidden;
            node.SecondaryLabel = hidden + " hidden";
            node.Metadata["stageHiddenDescendants"] = hidden.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        return node;
    }

    private static GraphSceneEdge CopyEdge(GraphSceneEdge source) {
        var edge = new GraphSceneEdge {
            Id = source.Id, SourceNodeId = source.SourceNodeId, TargetNodeId = source.TargetNodeId, Label = source.Label, Kind = source.Kind, Status = source.Status,
            Weight = source.Weight, Length = source.Length, Directed = source.Directed, LayoutDirected = source.LayoutDirected, SourceArrow = source.SourceArrow, TargetArrow = source.TargetArrow,
            Shape = source.Shape, Curvature = source.Curvature, Dashed = source.Dashed, ShowLabel = source.ShowLabel
        };
        edge.Style.Color = source.Style.Color; edge.Style.LabelColor = source.Style.LabelColor; edge.Style.Width = source.Style.Width; edge.Style.DashPattern = source.Style.DashPattern; edge.Style.Physics = source.Style.Physics; edge.Style.Hidden = source.Style.Hidden;
        edge.RoutePoints.AddRange(source.RoutePoints);
        CopyDictionary(source.Metadata, edge.Metadata);
        return edge;
    }

    private static void CopyDictionary(IReadOnlyDictionary<string, string> source, IDictionary<string, string> target) {
        foreach (var pair in source) target[pair.Key] = pair.Value;
    }

    private static void CompositeTransparentPixels(byte[] rgba, byte red, byte green, byte blue) {
        for (var index = 0; index < rgba.Length; index += 4) {
            var alpha = rgba[index + 3];
            if (alpha == 255) continue;
            var inverse = 255 - alpha;
            rgba[index] = (byte)((rgba[index] * alpha + red * inverse) / 255);
            rgba[index + 1] = (byte)((rgba[index + 1] * alpha + green * inverse) / 255);
            rgba[index + 2] = (byte)((rgba[index + 2] * alpha + blue * inverse) / 255);
            rgba[index + 3] = 255;
        }
    }
}
