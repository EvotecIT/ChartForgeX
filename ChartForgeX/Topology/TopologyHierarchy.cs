using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ChartForgeX.Topology;

/// <summary>
/// Describes one reusable hierarchy item that can be promoted into topology nodes and parent-child edges.
/// </summary>
public sealed class TopologyHierarchyItem {
    /// <summary>Creates a hierarchy item.</summary>
    public TopologyHierarchyItem(string id, string label, string? parentId = null) {
        Id = Required(id, nameof(id));
        Label = Required(label, nameof(label));
        ParentId = string.IsNullOrWhiteSpace(parentId) ? null : parentId!.Trim();
    }

    /// <summary>Gets the stable item id.</summary>
    public string Id { get; }

    /// <summary>Gets the item label.</summary>
    public string Label { get; }

    /// <summary>Gets or sets the parent item id.</summary>
    public string? ParentId { get; set; }

    /// <summary>Gets or sets an explicit hierarchy level. When null, the builder infers it from the parent chain.</summary>
    public int? Level { get; set; }

    /// <summary>Gets or sets the node kind used for the item.</summary>
    public TopologyNodeKind Kind { get; set; } = TopologyNodeKind.Generic;

    /// <summary>Gets or sets the item health state.</summary>
    public TopologyHealthStatus Status { get; set; } = TopologyHealthStatus.Unknown;

    /// <summary>Gets or sets the optional subtitle.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Gets or sets the optional visual symbol.</summary>
    public string? Symbol { get; set; }

    /// <summary>Gets or sets an optional icon id from a topology icon catalog.</summary>
    public string? IconId { get; set; }

    /// <summary>Gets or sets an optional group id.</summary>
    public string? GroupId { get; set; }

    /// <summary>Gets or sets an optional accent color.</summary>
    public string? Color { get; set; }

    /// <summary>Gets or sets an optional node background color.</summary>
    public string? BackgroundColor { get; set; }

    /// <summary>Gets or sets the preferred node width.</summary>
    public double? Width { get; set; }

    /// <summary>Gets or sets the preferred node height.</summary>
    public double? Height { get; set; }

    /// <summary>Gets arbitrary item metadata copied to the generated node.</summary>
    public Dictionary<string, string> Metadata { get; } = new();

    /// <summary>Adds metadata and returns the same item.</summary>
    public TopologyHierarchyItem WithMetadata(string key, string value) {
        Metadata[Required(key, nameof(key))] = value ?? string.Empty;
        return this;
    }

    internal static string Required(string value, string name) {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Topology hierarchy values cannot be empty.", name);
        return value.Trim();
    }
}

/// <summary>
/// Controls how hierarchy items are projected into a topology chart.
/// </summary>
public sealed class TopologyHierarchyOptions {
    /// <summary>Gets or sets the lowest primary visible level. Null starts at the root level.</summary>
    public int? MinLevel { get; set; }

    /// <summary>Gets or sets the highest visible level. Null includes the full hierarchy.</summary>
    public int? MaxLevel { get; set; }

    /// <summary>Gets or sets whether ancestors below <see cref="MinLevel"/> are kept as context breadcrumbs.</summary>
    public bool IncludeAncestorContext { get; set; } = true;

    /// <summary>Gets or sets the level used for root items when levels are inferred.</summary>
    public int RootLevel { get; set; }

    /// <summary>Gets or sets whether the chart should switch to layered layout.</summary>
    public bool ApplyLayeredLayout { get; set; } = true;

    /// <summary>Gets or sets the layered flow direction.</summary>
    public TopologyLayoutDirection LayoutDirection { get; set; } = TopologyLayoutDirection.TopToBottom;

    /// <summary>Gets or sets the default generated node display mode.</summary>
    public TopologyNodeDisplayMode? NodeDisplayMode { get; set; }

    /// <summary>Gets or sets the default node width.</summary>
    public double NodeWidth { get; set; } = 128;

    /// <summary>Gets or sets the default node height.</summary>
    public double NodeHeight { get; set; } = 60;

    /// <summary>Gets or sets the generated hierarchy edge kind.</summary>
    public TopologyEdgeKind EdgeKind { get; set; } = TopologyEdgeKind.Membership;

    /// <summary>Gets or sets the generated hierarchy edge health state.</summary>
    public TopologyHealthStatus EdgeStatus { get; set; } = TopologyHealthStatus.Unknown;

    /// <summary>Gets or sets the generated hierarchy edge direction.</summary>
    public TopologyDirection EdgeDirection { get; set; } = TopologyDirection.Forward;

    /// <summary>Gets or sets the generated hierarchy edge routing.</summary>
    public TopologyEdgeRouting EdgeRouting { get; set; } = TopologyEdgeRouting.Orthogonal;

    /// <summary>Gets or sets the generated hierarchy edge id prefix.</summary>
    public string EdgeIdPrefix { get; set; } = "hierarchy";
}

/// <summary>
/// Describes one team member for reusable people and organization diagrams.
/// </summary>
public sealed class TopologyTeamMember {
    /// <summary>Creates a team member item.</summary>
    public TopologyTeamMember(string id, string name, string? role = null) {
        Id = TopologyHierarchyItem.Required(id, nameof(id));
        Name = TopologyHierarchyItem.Required(name, nameof(name));
        Role = string.IsNullOrWhiteSpace(role) ? null : role!.Trim();
    }

    /// <summary>Gets the stable member id.</summary>
    public string Id { get; }

    /// <summary>Gets the member display name.</summary>
    public string Name { get; }

    /// <summary>Gets or sets the member role or title.</summary>
    public string? Role { get; set; }

    /// <summary>Gets or sets the manager or parent member id.</summary>
    public string? ParentId { get; set; }

    /// <summary>Gets or sets an explicit member level. Null is inferred from the team root and parent chain.</summary>
    public int? Level { get; set; }

    /// <summary>Gets or sets the member health state.</summary>
    public TopologyHealthStatus Status { get; set; } = TopologyHealthStatus.Unknown;

    /// <summary>Gets or sets an optional icon id from a topology icon catalog.</summary>
    public string? IconId { get; set; }

    /// <summary>Gets arbitrary member metadata copied to the generated person node.</summary>
    public Dictionary<string, string> Metadata { get; } = new();
}

/// <summary>
/// Controls how team members are projected into a topology chart.
/// </summary>
public sealed class TopologyTeamOptions {
    /// <summary>Gets or sets whether a root team node should be generated.</summary>
    public bool IncludeTeamNode { get; set; } = true;

    /// <summary>Gets or sets the team root node kind.</summary>
    public TopologyNodeKind TeamNodeKind { get; set; } = TopologyNodeKind.Team;

    /// <summary>Gets or sets the team root icon id.</summary>
    public string? TeamIconId { get; set; } = "team";

    /// <summary>Gets or sets the member icon id.</summary>
    public string? MemberIconId { get; set; } = "person";

    /// <summary>Gets or sets the highest visible level. Level 0 is the generated team node when included.</summary>
    public int? MaxLevel { get; set; }

    /// <summary>Gets or sets the lowest primary visible level. Level 0 is the generated team node when included.</summary>
    public int? MinLevel { get; set; }

    /// <summary>Gets or sets whether ancestors below <see cref="MinLevel"/> are kept as context breadcrumbs.</summary>
    public bool IncludeAncestorContext { get; set; } = true;

    /// <summary>Gets or sets the default node display mode.</summary>
    public TopologyNodeDisplayMode? NodeDisplayMode { get; set; } = TopologyNodeDisplayMode.Card;

    /// <summary>Gets or sets the layered flow direction.</summary>
    public TopologyLayoutDirection LayoutDirection { get; set; } = TopologyLayoutDirection.TopToBottom;

    /// <summary>Gets or sets the generated relationship edge kind.</summary>
    public TopologyEdgeKind EdgeKind { get; set; } = TopologyEdgeKind.Ownership;
}

/// <summary>
/// Fluent helpers for reusable hierarchy and team topology diagrams.
/// </summary>
public static class TopologyHierarchyExtensions {
    /// <summary>Adds parent-child hierarchy items to a topology chart.</summary>
    public static TopologyChart AddHierarchy(this TopologyChart chart, IEnumerable<TopologyHierarchyItem> items, TopologyHierarchyOptions? options = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        if (items == null) throw new ArgumentNullException(nameof(items));
        options ??= new TopologyHierarchyOptions();
        ValidateOptions(options);
        var materialized = items.ToList();
        var byId = new Dictionary<string, TopologyHierarchyItem>(StringComparer.Ordinal);
        foreach (var item in materialized) {
            if (byId.ContainsKey(item.Id)) throw new ArgumentException("Topology hierarchy item ids must be unique.", nameof(items));
            byId.Add(item.Id, item);
        }

        foreach (var item in materialized) {
            if (!string.IsNullOrWhiteSpace(item.ParentId) && !byId.ContainsKey(item.ParentId!)) throw new ArgumentException("Topology hierarchy parent '" + item.ParentId + "' was not found.", nameof(items));
        }

        var existing = new HashSet<string>(chart.Nodes.Select(node => node.Id), StringComparer.Ordinal);
        var levels = ResolveLevels(materialized, byId, options.RootLevel);
        var primaryIds = ResolvePrimaryIds(materialized, levels, options);
        var visibleIds = ResolveVisibleIds(primaryIds, byId, options);
        var visible = materialized.Where(item => visibleIds.Contains(item.Id)).OrderBy(item => levels[item.Id]).ThenBy(item => item.Id, StringComparer.Ordinal).ToList();

        foreach (var item in visible) {
            if (!existing.Add(item.Id)) throw new ArgumentException("Topology node '" + item.Id + "' already exists.", nameof(items));
            chart.AddAutoNode(item.Id, item.Label, item.Kind, item.Status, item.GroupId, item.Subtitle, width: item.Width ?? options.NodeWidth, height: item.Height ?? options.NodeHeight, symbol: item.Symbol, color: item.Color, iconId: item.IconId);
            var node = chart.Nodes[chart.Nodes.Count - 1];
            node.BackgroundColor = item.BackgroundColor;
            if (options.NodeDisplayMode.HasValue) node.DisplayMode = options.NodeDisplayMode.Value;
            node.Metadata["layer"] = levels[item.Id].ToString(CultureInfo.InvariantCulture);
            node.Metadata["hierarchy.level"] = node.Metadata["layer"];
            node.Metadata["hierarchy.context"] = primaryIds.Contains(item.Id) ? "primary" : "ancestor";
            if (!string.IsNullOrWhiteSpace(item.ParentId)) node.Metadata["hierarchy.parentId"] = item.ParentId!;
            foreach (var pair in item.Metadata) node.Metadata[pair.Key] = pair.Value;
        }

        foreach (var item in visible) {
            if (string.IsNullOrWhiteSpace(item.ParentId) || !visibleIds.Contains(item.ParentId!)) continue;
            chart.AddEdge(EdgeId(options.EdgeIdPrefix, item.ParentId!, item.Id), item.ParentId!, item.Id, kind: options.EdgeKind, status: options.EdgeStatus, direction: options.EdgeDirection, routing: options.EdgeRouting);
            var edge = chart.Edges[chart.Edges.Count - 1];
            ApplyHierarchyPorts(edge, options.LayoutDirection, options.ApplyLayeredLayout);
            edge.Metadata["hierarchy.relationship"] = "parent-child";
        }

        if (options.ApplyLayeredLayout) chart.WithLayout(TopologyLayoutMode.Layered, options.LayoutDirection);
        return chart;
    }

    /// <summary>Adds a reusable team or org diagram to a topology chart.</summary>
    public static TopologyChart AddTeam(this TopologyChart chart, string teamId, string teamLabel, IEnumerable<TopologyTeamMember> members, TopologyTeamOptions? options = null) {
        if (members == null) throw new ArgumentNullException(nameof(members));
        options ??= new TopologyTeamOptions();
        ValidateOptions(options);
        var rootId = TopologyHierarchyItem.Required(teamId, nameof(teamId));
        var hierarchy = new List<TopologyHierarchyItem>();
        if (options.IncludeTeamNode) hierarchy.Add(new TopologyHierarchyItem(rootId, teamLabel) { Level = 0, Kind = options.TeamNodeKind, IconId = options.TeamIconId, Status = TopologyHealthStatus.Healthy, Symbol = "TM" });
        foreach (var member in members) {
            var parent = string.IsNullOrWhiteSpace(member.ParentId) && options.IncludeTeamNode ? rootId : member.ParentId;
            var item = new TopologyHierarchyItem(member.Id, member.Name, parent) { Level = member.Level, Kind = TopologyNodeKind.Person, Status = member.Status, Subtitle = member.Role, IconId = member.IconId ?? options.MemberIconId, Symbol = "U", Width = 164, Height = 64 };
            foreach (var pair in member.Metadata) item.Metadata[pair.Key] = pair.Value;
            hierarchy.Add(item);
        }

        return chart.AddHierarchy(hierarchy, new TopologyHierarchyOptions { MinLevel = options.MinLevel, MaxLevel = options.MaxLevel, IncludeAncestorContext = options.IncludeAncestorContext, NodeDisplayMode = options.NodeDisplayMode, LayoutDirection = options.LayoutDirection, EdgeKind = options.EdgeKind, EdgeStatus = TopologyHealthStatus.Healthy, EdgeDirection = TopologyDirection.Forward, NodeWidth = 164, NodeHeight = 64 });
    }

    private static void ValidateOptions(TopologyHierarchyOptions options) {
        if (options.MinLevel.HasValue && options.MaxLevel.HasValue && options.MinLevel.Value > options.MaxLevel.Value) throw new ArgumentOutOfRangeException(nameof(options.MinLevel), options.MinLevel.Value, "Minimum hierarchy level cannot be greater than maximum hierarchy level.");
        TopologyModelGuards.PositiveFinite(options.NodeWidth, nameof(options.NodeWidth));
        TopologyModelGuards.PositiveFinite(options.NodeHeight, nameof(options.NodeHeight));
        TopologyModelGuards.EnumDefined(options.LayoutDirection, nameof(options.LayoutDirection));
        TopologyModelGuards.EnumDefined(options.EdgeKind, nameof(options.EdgeKind));
        TopologyModelGuards.EnumDefined(options.EdgeStatus, nameof(options.EdgeStatus));
        TopologyModelGuards.EnumDefined(options.EdgeDirection, nameof(options.EdgeDirection));
        TopologyModelGuards.EnumDefined(options.EdgeRouting, nameof(options.EdgeRouting));
        if (options.NodeDisplayMode.HasValue) TopologyModelGuards.EnumDefined(options.NodeDisplayMode.Value, nameof(options.NodeDisplayMode));
        TopologyHierarchyItem.Required(options.EdgeIdPrefix, nameof(options.EdgeIdPrefix));
    }

    private static void ValidateOptions(TopologyTeamOptions options) {
        if (options.MinLevel.HasValue && options.MaxLevel.HasValue && options.MinLevel.Value > options.MaxLevel.Value) throw new ArgumentOutOfRangeException(nameof(options.MinLevel), options.MinLevel.Value, "Minimum team level cannot be greater than maximum team level.");
        TopologyModelGuards.EnumDefined(options.TeamNodeKind, nameof(options.TeamNodeKind));
        TopologyModelGuards.EnumDefined(options.LayoutDirection, nameof(options.LayoutDirection));
        TopologyModelGuards.EnumDefined(options.EdgeKind, nameof(options.EdgeKind));
        if (options.NodeDisplayMode.HasValue) TopologyModelGuards.EnumDefined(options.NodeDisplayMode.Value, nameof(options.NodeDisplayMode));
    }

    private static HashSet<string> ResolvePrimaryIds(IReadOnlyList<TopologyHierarchyItem> items, IReadOnlyDictionary<string, int> levels, TopologyHierarchyOptions options) {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in items) {
            var level = levels[item.Id];
            if (options.MinLevel.HasValue && level < options.MinLevel.Value) continue;
            if (options.MaxLevel.HasValue && level > options.MaxLevel.Value) continue;
            ids.Add(item.Id);
        }

        return ids;
    }

    private static HashSet<string> ResolveVisibleIds(HashSet<string> primaryIds, IReadOnlyDictionary<string, TopologyHierarchyItem> byId, TopologyHierarchyOptions options) {
        var ids = new HashSet<string>(primaryIds, StringComparer.Ordinal);
        if (!options.IncludeAncestorContext || !options.MinLevel.HasValue) return ids;
        foreach (var id in primaryIds.ToList()) AddAncestors(id, byId, ids);
        return ids;
    }

    private static void AddAncestors(string id, IReadOnlyDictionary<string, TopologyHierarchyItem> byId, HashSet<string> ids) {
        if (!byId.TryGetValue(id, out var item) || string.IsNullOrWhiteSpace(item.ParentId)) return;
        if (ids.Add(item.ParentId!)) AddAncestors(item.ParentId!, byId, ids);
    }

    private static void ApplyHierarchyPorts(TopologyEdge edge, TopologyLayoutDirection direction, bool preMirror) {
        switch (direction) {
            case TopologyLayoutDirection.LeftToRight:
                edge.SourcePort = TopologyEdgePort.Right;
                edge.TargetPort = TopologyEdgePort.Left;
                break;
            case TopologyLayoutDirection.RightToLeft:
                edge.SourcePort = preMirror ? TopologyEdgePort.Right : TopologyEdgePort.Left;
                edge.TargetPort = preMirror ? TopologyEdgePort.Left : TopologyEdgePort.Right;
                break;
            case TopologyLayoutDirection.BottomToTop:
                edge.SourcePort = preMirror ? TopologyEdgePort.Bottom : TopologyEdgePort.Top;
                edge.TargetPort = preMirror ? TopologyEdgePort.Top : TopologyEdgePort.Bottom;
                break;
            default:
                edge.SourcePort = TopologyEdgePort.Bottom;
                edge.TargetPort = TopologyEdgePort.Top;
                break;
        }
    }

    private static Dictionary<string, int> ResolveLevels(IReadOnlyList<TopologyHierarchyItem> items, IReadOnlyDictionary<string, TopologyHierarchyItem> byId, int rootLevel) {
        var levels = new Dictionary<string, int>(StringComparer.Ordinal);
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in items) ResolveLevel(item, byId, levels, visiting, rootLevel);
        return levels;
    }

    private static int ResolveLevel(TopologyHierarchyItem item, IReadOnlyDictionary<string, TopologyHierarchyItem> byId, Dictionary<string, int> levels, HashSet<string> visiting, int rootLevel) {
        if (levels.TryGetValue(item.Id, out var cached)) return cached;
        if (!visiting.Add(item.Id)) throw new ArgumentException("Topology hierarchy contains a parent cycle at '" + item.Id + "'.");
        int level;
        if (item.Level.HasValue) level = item.Level.Value;
        else if (string.IsNullOrWhiteSpace(item.ParentId)) level = rootLevel;
        else {
            if (!byId.TryGetValue(item.ParentId!, out var parent)) throw new ArgumentException("Topology hierarchy parent '" + item.ParentId + "' was not found.");
            level = ResolveLevel(parent, byId, levels, visiting, rootLevel) + 1;
        }

        visiting.Remove(item.Id);
        levels[item.Id] = level;
        return level;
    }

    private static string EdgeId(string prefix, string parentId, string childId) => TopologyHierarchyItem.Required(prefix, nameof(prefix)) + ":" + parentId + ":" + childId;
}
