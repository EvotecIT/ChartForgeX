using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ChartForgeX.Topology;

/// <summary>
/// Controls how hierarchy items are projected into a centered mind-map topology.
/// </summary>
public sealed class TopologyMindMapOptions {
    /// <summary>Gets or sets whether the chart should switch to mind-map layout.</summary>
    public bool ApplyMindMapLayout { get; set; } = true;

    /// <summary>Gets or sets the generated mind-map edge id prefix.</summary>
    public string EdgeIdPrefix { get; set; } = "mindmap";

    /// <summary>Gets or sets the generated branch edge kind.</summary>
    public TopologyEdgeKind EdgeKind { get; set; } = TopologyEdgeKind.Mapping;

    /// <summary>Gets or sets the generated branch edge health state.</summary>
    public TopologyHealthStatus EdgeStatus { get; set; } = TopologyHealthStatus.Healthy;

    /// <summary>Gets or sets the generated branch edge direction.</summary>
    public TopologyDirection EdgeDirection { get; set; } = TopologyDirection.None;

    /// <summary>Gets or sets the generated branch edge routing.</summary>
    public TopologyEdgeRouting EdgeRouting { get; set; } = TopologyEdgeRouting.Orthogonal;

    /// <summary>Gets or sets whether generated branch edges should render as quiet structural links.</summary>
    public bool MutedEdges { get; set; } = false;

    /// <summary>Gets or sets whether generated branch edges should inherit the parent node accent color when available.</summary>
    public bool UseParentColorForEdges { get; set; } = true;

    /// <summary>Gets or sets the default root node display mode.</summary>
    public TopologyNodeDisplayMode RootDisplayMode { get; set; } = TopologyNodeDisplayMode.Card;

    /// <summary>Gets or sets the default first-level branch node display mode.</summary>
    public TopologyNodeDisplayMode BranchDisplayMode { get; set; } = TopologyNodeDisplayMode.CompactCard;

    /// <summary>Gets or sets the default descendant node display mode.</summary>
    public TopologyNodeDisplayMode LeafDisplayMode { get; set; } = TopologyNodeDisplayMode.Pill;

    /// <summary>Gets or sets the default root node width.</summary>
    public double RootWidth { get; set; } = 260;

    /// <summary>Gets or sets the default root node height.</summary>
    public double RootHeight { get; set; } = 104;

    /// <summary>Gets or sets the default first-level branch node width.</summary>
    public double BranchWidth { get; set; } = 220;

    /// <summary>Gets or sets the default first-level branch node height.</summary>
    public double BranchHeight { get; set; } = 76;

    /// <summary>Gets or sets the default descendant node width.</summary>
    public double LeafWidth { get; set; } = 220;

    /// <summary>Gets or sets the default descendant node height.</summary>
    public double LeafHeight { get; set; } = 42;
}

/// <summary>
/// Fluent helpers for reusable mind-map topology diagrams.
/// </summary>
public static class TopologyMindMapExtensions {
    /// <summary>Adds parent-child items as a centered mind map with balanced left and right branches.</summary>
    public static TopologyChart AddMindMap(this TopologyChart chart, IEnumerable<TopologyHierarchyItem> items, TopologyMindMapOptions? options = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        if (items == null) throw new ArgumentNullException(nameof(items));
        options ??= new TopologyMindMapOptions();
        ValidateOptions(options);

        var materialized = items.ToList();
        var byId = new Dictionary<string, TopologyHierarchyItem>(StringComparer.Ordinal);
        foreach (var item in materialized) {
            if (byId.ContainsKey(item.Id)) throw new ArgumentException("Topology mind-map item ids must be unique.", nameof(items));
            byId.Add(item.Id, item);
        }

        foreach (var item in materialized) {
            if (!string.IsNullOrWhiteSpace(item.ParentId) && !byId.ContainsKey(item.ParentId!)) throw new ArgumentException("Topology mind-map parent '" + item.ParentId + "' was not found.", nameof(items));
        }

        var roots = materialized.Where(item => string.IsNullOrWhiteSpace(item.ParentId)).OrderBy(item => item.Id, StringComparer.Ordinal).ToList();
        if (roots.Count != 1) throw new ArgumentException("Topology mind maps require exactly one root item.", nameof(items));

        var existing = new HashSet<string>(chart.Nodes.Select(node => node.Id), StringComparer.Ordinal);
        var levels = ResolveLevels(materialized, byId);
        foreach (var item in materialized.OrderBy(item => levels[item.Id]).ThenBy(item => item.Id, StringComparer.Ordinal)) {
            if (!existing.Add(item.Id)) throw new ArgumentException("Topology node '" + item.Id + "' already exists.", nameof(items));
            var level = levels[item.Id];
            var isRoot = string.IsNullOrWhiteSpace(item.ParentId);
            var defaultStyleLevel = isRoot ? 0 : level;
            var kind = item.Kind == TopologyNodeKind.Generic && isRoot ? TopologyNodeKind.Hub : item.Kind;
            var width = item.Width ?? NodeWidthForLevel(options, defaultStyleLevel);
            var height = item.Height ?? NodeHeightForLevel(options, defaultStyleLevel);
            chart.AddAutoNode(item.Id, item.Label, kind, item.Status, item.GroupId, item.Subtitle, width: width, height: height, symbol: item.Symbol, color: item.Color, iconId: item.IconId);
            var node = chart.Nodes[chart.Nodes.Count - 1];
            node.BackgroundColor = item.BackgroundColor;
            node.DisplayMode = NodeDisplayForLevel(options, defaultStyleLevel);
            node.Metadata["layer"] = level.ToString(CultureInfo.InvariantCulture);
            node.Metadata["mindmap.level"] = node.Metadata["layer"];
            if (isRoot) node.Metadata["mindmap.root"] = "true";
            if (!string.IsNullOrWhiteSpace(item.ParentId)) node.Metadata["mindmap.parentId"] = item.ParentId!;
            foreach (var pair in item.Metadata) {
                if (IsMindMapBuilderMetadata(pair.Key)) continue;
                node.Metadata[pair.Key] = pair.Value;
            }
        }

        foreach (var item in materialized.OrderBy(item => item.Id, StringComparer.Ordinal)) {
            if (string.IsNullOrWhiteSpace(item.ParentId)) continue;
            chart.AddEdge(EdgeId(options.EdgeIdPrefix, item.ParentId!, item.Id), item.ParentId!, item.Id, kind: options.EdgeKind, status: options.EdgeStatus, direction: options.EdgeDirection, routing: options.EdgeRouting);
            var edge = chart.Edges[chart.Edges.Count - 1];
            edge.IsMuted = options.MutedEdges;
            if (options.UseParentColorForEdges && byId.TryGetValue(item.ParentId!, out var parent) && !string.IsNullOrWhiteSpace(parent.Color)) edge.Color = parent.Color;
            edge.Metadata["mindmap.relationship"] = "parent-child";
        }

        if (options.ApplyMindMapLayout) chart.WithLayout(TopologyLayoutMode.MindMap);
        return chart;
    }

    private static void ValidateOptions(TopologyMindMapOptions options) {
        TopologyHierarchyItem.Required(options.EdgeIdPrefix, nameof(options.EdgeIdPrefix));
        TopologyModelGuards.EnumDefined(options.EdgeKind, nameof(options.EdgeKind));
        TopologyModelGuards.EnumDefined(options.EdgeStatus, nameof(options.EdgeStatus));
        TopologyModelGuards.EnumDefined(options.EdgeDirection, nameof(options.EdgeDirection));
        TopologyModelGuards.EnumDefined(options.EdgeRouting, nameof(options.EdgeRouting));
        TopologyModelGuards.EnumDefined(options.RootDisplayMode, nameof(options.RootDisplayMode));
        TopologyModelGuards.EnumDefined(options.BranchDisplayMode, nameof(options.BranchDisplayMode));
        TopologyModelGuards.EnumDefined(options.LeafDisplayMode, nameof(options.LeafDisplayMode));
        TopologyModelGuards.PositiveFinite(options.RootWidth, nameof(options.RootWidth));
        TopologyModelGuards.PositiveFinite(options.RootHeight, nameof(options.RootHeight));
        TopologyModelGuards.PositiveFinite(options.BranchWidth, nameof(options.BranchWidth));
        TopologyModelGuards.PositiveFinite(options.BranchHeight, nameof(options.BranchHeight));
        TopologyModelGuards.PositiveFinite(options.LeafWidth, nameof(options.LeafWidth));
        TopologyModelGuards.PositiveFinite(options.LeafHeight, nameof(options.LeafHeight));
    }

    private static Dictionary<string, int> ResolveLevels(IReadOnlyList<TopologyHierarchyItem> items, IReadOnlyDictionary<string, TopologyHierarchyItem> byId) {
        var levels = new Dictionary<string, int>(StringComparer.Ordinal);
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in items) ResolveLevel(item, byId, levels, visiting);
        return levels;
    }

    private static int ResolveLevel(TopologyHierarchyItem item, IReadOnlyDictionary<string, TopologyHierarchyItem> byId, Dictionary<string, int> levels, HashSet<string> visiting) {
        if (levels.TryGetValue(item.Id, out var cached)) return cached;
        if (!visiting.Add(item.Id)) throw new ArgumentException("Topology mind map contains a parent cycle at '" + item.Id + "'.");
        int? parentLevel = null;
        if (!string.IsNullOrWhiteSpace(item.ParentId)) parentLevel = ResolveLevel(byId[item.ParentId!], byId, levels, visiting);
        var level = item.Level ?? (parentLevel.HasValue ? parentLevel.Value + 1 : 0);
        visiting.Remove(item.Id);
        levels[item.Id] = level;
        return level;
    }

    private static TopologyNodeDisplayMode NodeDisplayForLevel(TopologyMindMapOptions options, int level) =>
        level <= 0 ? options.RootDisplayMode : level == 1 ? options.BranchDisplayMode : options.LeafDisplayMode;

    private static double NodeWidthForLevel(TopologyMindMapOptions options, int level) =>
        level <= 0 ? options.RootWidth : level == 1 ? options.BranchWidth : options.LeafWidth;

    private static double NodeHeightForLevel(TopologyMindMapOptions options, int level) =>
        level <= 0 ? options.RootHeight : level == 1 ? options.BranchHeight : options.LeafHeight;

    private static bool IsMindMapBuilderMetadata(string key) =>
        string.Equals(key, "layer", StringComparison.Ordinal) ||
        string.Equals(key, "mindmap.level", StringComparison.Ordinal) ||
        string.Equals(key, "mindmap.parentId", StringComparison.Ordinal) ||
        string.Equals(key, "mindmap.root", StringComparison.Ordinal);

    private static string EdgeId(string prefix, string parentId, string childId) =>
        TopologyHierarchyItem.Required(prefix, nameof(prefix)) + ":" + parentId + ":" + childId;
}
