using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Topology;

/// <summary>
/// Opt-in layout policy that replaces runs of same-kind leaf nodes under one parent with a single titled card listing
/// the members as compact rows, and staggers long rows of children into two offset rows.
/// </summary>
/// <remarks>A leaf is a node whose edges all connect to one other node. Leaves are grouped when they share their
/// parent, kind and group. Nodes named by scenarios, alignments, annotations or label anchors stay as they are.</remarks>
public sealed class TopologySiblingGrouping {
    private int _minimumCount = 3;
    private int _staggerCount = 9;
    private int _maximumRows = 8;
    private int _collapseCount = 12;

    /// <summary>Gets or sets how many same-kind leaves a parent needs before they become one card. The default is three.</summary>
    public int MinimumCount {
        get => _minimumCount;
        set => _minimumCount = value >= 2 ? value : throw new ArgumentOutOfRangeException(nameof(value), value, "Sibling groups need at least two members.");
    }

    /// <summary>Gets or sets how many children in one layered row are staggered into two offset rows. The default is nine; zero turns staggering off.</summary>
    public int StaggerCount {
        get => _staggerCount;
        set => _staggerCount = value == 0 || value >= 4 ? value : throw new ArgumentOutOfRangeException(nameof(value), value, "Staggering needs at least four children, or zero to turn it off.");
    }

    /// <summary>Gets or sets how many member rows a card lists before it summarises the rest. The default is eight.</summary>
    public int MaximumRows {
        get => _maximumRows;
        set => _maximumRows = value >= 1 ? value : throw new ArgumentOutOfRangeException(nameof(value), value, "Cards list at least one member row.");
    }

    /// <summary>Gets or sets how many members make a card start collapsed to its header and count. The default is twelve; zero keeps every card expanded.</summary>
    /// <remarks>A collapsed card reserves only its header in the layout. Interactive output expands it on demand over its neighbours.</remarks>
    public int CollapseCount {
        get => _collapseCount;
        set => _collapseCount = value == 0 || value >= 2 ? value : throw new ArgumentOutOfRangeException(nameof(value), value, "Collapsing needs at least two members, or zero to turn it off.");
    }

    /// <summary>Gets or sets an optional card title. It receives the shared kind and the members; null uses the shared symbol or the kind name.</summary>
    public Func<TopologyNodeKind, IReadOnlyList<TopologyNode>, string>? TitleFormatter { get; set; }

    internal TopologySiblingGrouping Clone() => new() { MinimumCount = MinimumCount, StaggerCount = StaggerCount, MaximumRows = MaximumRows, CollapseCount = CollapseCount, TitleFormatter = TitleFormatter };
}

/// <summary>One member row of a sibling group card, captured before the member is removed from the layout.</summary>
internal sealed class TopologySiblingMember {
    internal TopologySiblingMember(string id, string label, string? subtitle, string? badge, TopologyHealthStatus status, TopologyNodeKind kind,
        string? iconId, string? symbol, string? color, string? href, string? tooltip) {
        Id = id; Label = label; Subtitle = subtitle; Badge = badge; Status = status; Kind = kind;
        IconId = iconId; Symbol = symbol; Color = color; Href = href; Tooltip = tooltip;
    }
    internal TopologySiblingMember(TopologyNode node) {
        Id = node.Id; Label = node.Label; Subtitle = node.Subtitle; Badge = node.Badge; Status = node.Status;
        Kind = node.Kind; IconId = node.IconId; Symbol = node.Symbol; Color = node.Color; Href = node.Href; Tooltip = node.Tooltip;
    }
    internal string Id { get; }
    internal string Label { get; }
    internal string? Subtitle { get; }
    internal string? Badge { get; }
    internal TopologyHealthStatus Status { get; }
    internal TopologyNodeKind Kind { get; }
    internal string? IconId { get; }
    internal string? Symbol { get; }
    internal string? Color { get; }
    internal string? Href { get; }
    internal string? Tooltip { get; }
}

/// <summary>The captured members and row geometry of one sibling group card.</summary>
internal sealed class TopologySiblingCard {
    internal const double HeaderHeight = 36, RowHeight = 22, BottomPadding = 8, MinimumWidth = 210, MaximumWidth = 340;
    internal TopologySiblingCard(string parentId, IReadOnlyList<TopologySiblingMember> members, int maximumRows, int collapseCount) {
        ParentId = parentId; Members = members; Collapsed = collapseCount > 0 && members.Count >= collapseCount;
        VisibleCount = members.Count <= maximumRows ? members.Count : maximumRows - 1;
    }
    internal TopologySiblingCard(string parentId, IReadOnlyList<TopologySiblingMember> members, int visibleCount, bool collapsed) {
        ParentId = parentId; Members = members; VisibleCount = visibleCount; Collapsed = collapsed;
    }
    internal string ParentId { get; }
    internal IReadOnlyList<TopologySiblingMember> Members { get; }
    /// <summary>Whether the card starts collapsed: only its header is laid out and painted.</summary>
    internal bool Collapsed { get; }
    /// <summary>Members listed as rows; any others are summarised in one final row.</summary>
    internal int VisibleCount { get; }
    internal int RowCount => VisibleCount + (VisibleCount < Members.Count ? 1 : 0);
    internal double ExpandedHeight => HeaderHeight + RowCount * RowHeight + BottomPadding;
    internal double LayoutHeight => Collapsed ? HeaderHeight : ExpandedHeight;
}

internal static class TopologySiblingGroupingLayout {
    internal static void Apply(TopologyChart chart, TopologyView? view = null, TopologyRenderOptions? options = null) {
        var policy = chart.SiblingGrouping;
        if (policy == null || chart.LayoutMode is TopologyLayoutMode.Regions or TopologyLayoutMode.Manual or TopologyLayoutMode.Geographic) return;
        var nodes = chart.Nodes.GroupBy(node => node.Id, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var pinned = PinnedIds(chart, view, options);
        // Leaves whose links differ in direction or kind keep separate cards, so no relationship is lost to the aggregate edge.
        var buckets = new Dictionary<(string Parent, TopologyNodeKind Kind, string? Group, string Links), List<TopologyNode>>();
        foreach (var leaf in Leaves(chart, nodes, pinned, node => (node.DisplayMode ?? options?.NodeDisplayMode) == TopologyNodeDisplayMode.Hidden)) {
            // A view that filters by health judges every member by its own status, so only same-status leaves may share a card.
            var byStatus = view?.HealthStatuses.Count > 0;
            var links = string.Join(";", leaf.Edges.Select(edge => (edge.SourceNodeId == leaf.Parent ? "out" : "in") + ":" + edge.Direction + ":" + edge.Kind
                + (byStatus ? ":" + edge.Status : string.Empty)).OrderBy(text => text, StringComparer.Ordinal)) + (byStatus ? "|" + leaf.Node.Status : string.Empty);
            var key = (leaf.Parent, leaf.Node.Kind, leaf.Node.GroupId, links);
            if (!buckets.TryGetValue(key, out var list)) buckets.Add(key, list = new List<TopologyNode>());
            list.Add(leaf.Node);
        }
        foreach (var bucket in buckets.Where(pair => pair.Value.Count >= policy.MinimumCount).OrderBy(pair => chart.Nodes.IndexOf(pair.Value[0]))) {
            Collapse(chart, policy, bucket.Key.Parent, bucket.Key.Kind, bucket.Value);
        }
    }

    private static void Collapse(TopologyChart chart, TopologySiblingGrouping policy, string parentId, TopologyNodeKind kind, List<TopologyNode> members) {
        var memberIds = new HashSet<string>(members.Select(member => member.Id), StringComparer.Ordinal);
        var edges = chart.Edges.Where(edge => memberIds.Contains(edge.SourceNodeId) || memberIds.Contains(edge.TargetNodeId)).ToArray();
        var edgeSet = new HashSet<TopologyEdge>(edges);
        var first = members[0];
        var title = policy.TitleFormatter?.Invoke(kind, members.AsReadOnly()) ?? Shared(members, node => node.Symbol) ?? kind.ToString();
        var card = new TopologySiblingCard(parentId, members.Select(member => new TopologySiblingMember(member)).ToArray(), policy.MaximumRows, policy.CollapseCount);
        var id = UniqueId(chart, parentId + "~" + kind.ToString().ToLowerInvariant() + "~siblings");
        var node = new TopologyNode {
            Id = id, Label = string.IsNullOrWhiteSpace(title) ? kind.ToString() : title, Kind = kind, GroupId = first.GroupId,
            Status = Worst(members.Select(member => member.Status)), IconId = Shared(members, member => member.IconId),
            Symbol = Shared(members, member => member.Symbol), Color = Shared(members, member => member.Color),
            DisplayMode = TopologyNodeDisplayMode.Card, PreserveDisplayModeSize = true,
            Width = CardWidth(chart, card), Height = card.LayoutHeight, X = first.X, Y = first.Y, SiblingGroup = card
        };
        // Metadata every member shares, such as an authored layer, describes the card too.
        foreach (var entry in first.Metadata)
            if (members.All(member => member.Metadata.TryGetValue(entry.Key, out var value) && value == entry.Value)) node.Metadata[entry.Key] = entry.Value;
        var index = chart.Nodes.IndexOf(first);
        chart.Nodes.RemoveAll(candidate => memberIds.Contains(candidate.Id));
        chart.Nodes.Insert(Math.Min(index, chart.Nodes.Count), node);
        // One aggregate edge per direction and kind the members use, so a leaf linked both ways keeps both relationships.
        var families = edges.GroupBy(edge => (Outward: edge.SourceNodeId == parentId, edge.Direction, edge.Kind)).ToArray();
        var insertAt = chart.Edges.FindIndex(edgeSet.Contains);
        foreach (var family in families) {
            var template = family.First();
            var outward = family.Key.Outward;
            var aggregate = new TopologyEdge {
                Id = UniqueEdgeId(chart, id + "~edge" + (families.Length > 1 ? "-" + (outward ? "out" : "in") + "-" + template.Kind.ToString().ToLowerInvariant() + "-" + template.Direction.ToString().ToLowerInvariant() : string.Empty)),
                SourceNodeId = outward ? parentId : id, TargetNodeId = outward ? id : parentId,
                Kind = template.Kind, Direction = template.Direction, Routing = template.Routing, LineStyle = template.LineStyle,
                Status = Worst(family.Select(candidate => candidate.Status)), Color = Shared(family, candidate => candidate.Color),
                IsMuted = family.All(candidate => candidate.IsMuted),
                Evidence = family.All(candidate => candidate.Evidence == template.Evidence) ? template.Evidence : TopologyEdgeEvidence.Unspecified,
                UplinkEnd = family.All(candidate => candidate.UplinkEnd == template.UplinkEnd) ? template.UplinkEnd : TopologyEdgeEnd.None,
                // Whatever every member link says identically, the aggregate says too.
                EvidenceText = Shared(family, candidate => candidate.EvidenceText), Label = Shared(family, candidate => candidate.Label),
                SecondaryLabel = Shared(family, candidate => candidate.SecondaryLabel), Tooltip = Shared(family, candidate => candidate.Tooltip),
                SourceLabel = Shared(family, candidate => candidate.SourceLabel), TargetLabel = Shared(family, candidate => candidate.TargetLabel)
            };
            // Authored styling every member link agrees on carries over; values that differ fall back to the defaults.
            if (SharedValue(family, candidate => candidate.StrokeWidth) is { } strokeWidth) aggregate.StrokeWidth = strokeWidth;
            if (SharedValue(family, candidate => candidate.Opacity) is { } opacity) aggregate.Opacity = opacity;
            if (SharedValue(family, candidate => candidate.SourceMarker) is { } sourceMarker) aggregate.SourceMarker = sourceMarker;
            if (SharedValue(family, candidate => candidate.TargetMarker) is { } targetMarker) aggregate.TargetMarker = targetMarker;
            if (SharedValue(family, candidate => (TopologyEdgeEmphasis?)candidate.Emphasis) is { } emphasis) aggregate.Emphasis = emphasis;
            if (SharedValue(family, candidate => (TopologyEdgePort?)candidate.SourcePort) is { } sourcePort) aggregate.SourcePort = sourcePort;
            if (SharedValue(family, candidate => (TopologyEdgePort?)candidate.TargetPort) is { } targetPort) aggregate.TargetPort = targetPort;
            if (SharedValue(family, candidate => candidate.PreferredLength) is { } preferredLength) aggregate.PreferredLength = preferredLength;
            if (SharedValue(family, candidate => (int?)candidate.MinimumRankSpan) is { } rankSpan) aggregate.MinimumRankSpan = rankSpan;
            if (SharedValue(family, candidate => (int?)candidate.RoutingPriority) is { } priority) aggregate.RoutingPriority = priority;
            aggregate.LabelColor = Shared(family, candidate => candidate.LabelColor);
            aggregate.LabelBackgroundColor = Shared(family, candidate => candidate.LabelBackgroundColor);
            aggregate.Href = Shared(family, candidate => candidate.Href);
            aggregate.CssClass = Shared(family, candidate => candidate.CssClass);
            if (Shared(family, candidate => Signature(candidate.DashPattern.Select(length => length.ToString("R", System.Globalization.CultureInfo.InvariantCulture)))) != null)
                aggregate.DashPattern.AddRange(template.DashPattern);
            if (template.SourceMarkerStyle != null && Shared(family, candidate => MarkerKey(candidate.SourceMarkerStyle)) != null) aggregate.SourceMarkerStyle = template.SourceMarkerStyle.Clone();
            if (template.TargetMarkerStyle != null && Shared(family, candidate => MarkerKey(candidate.TargetMarkerStyle)) != null) aggregate.TargetMarkerStyle = template.TargetMarkerStyle.Clone();
            foreach (var entry in template.Metadata)
                if (family.All(candidate => candidate.Metadata.TryGetValue(entry.Key, out var value) && value == entry.Value)) aggregate.Metadata[entry.Key] = entry.Value;
            foreach (var entry in template.Metrics)
                if (family.All(candidate => candidate.Metrics.TryGetValue(entry.Key, out var value) && value == entry.Value)) aggregate.Metrics[entry.Key] = entry.Value;
            if (Shared(family, candidate => Signature(candidate.Stripes.Select(stripe => stripe.Color + "\u001f" + stripe.Label))) != null)
                foreach (var stripe in template.Stripes) aggregate.Stripes.Add(stripe.Clone());
            if (Shared(family, candidate => Signature(candidate.Chips.Select(chip => chip.Text + "\u001f" + chip.Tone + "\u001f" + chip.IconId))) != null)
                foreach (var chip in template.Chips) aggregate.Chips.Add(chip.Clone());
            chart.Edges.Insert(insertAt++, aggregate);
        }
        chart.Edges.RemoveAll(edgeSet.Contains);
    }

    private static double CardWidth(TopologyChart chart, TopologySiblingCard card) {
        double Measure(string? text, double size, bool bold) => string.IsNullOrWhiteSpace(text) ? 0 : TopologyRenderPrimitives.EstimateTextWidth(text!, size, bold, chart.TextMeasurement);
        var rows = card.Members.Take(card.VisibleCount).Select(member => Measure(member.Label, TopologySiblingCardText.NameSize, true)
            + (string.IsNullOrWhiteSpace(member.Subtitle) ? 0 : 8 + Measure(member.Subtitle, TopologySiblingCardText.SecondarySize, false))
            + (string.IsNullOrWhiteSpace(member.Badge) ? 0 : 14 + Measure(member.Badge, TopologySiblingCardText.ChipSize, true)));
        var content = rows.DefaultIfEmpty(0).Max() + TopologySiblingCardText.RowTextLeft + 26;
        return Math.Max(TopologySiblingCard.MinimumWidth, Math.Min(TopologySiblingCard.MaximumWidth, Math.Ceiling(content)));
    }

    // A leaf has at least one edge and every edge goes to the same other node, which is its parent.
    private static IEnumerable<(TopologyNode Node, string Parent, IReadOnlyList<TopologyEdge> Edges)> Leaves(TopologyChart chart, IReadOnlyDictionary<string, TopologyNode> nodes, ISet<string> pinned, Func<TopologyNode, bool>? hidden = null) {
        var incident = new Dictionary<string, List<TopologyEdge>>(StringComparer.Ordinal);
        foreach (var edge in chart.Edges) {
            foreach (var end in new[] { edge.SourceNodeId, edge.TargetNodeId }.Distinct(StringComparer.Ordinal)) {
                if (!incident.TryGetValue(end, out var list)) incident.Add(end, list = new List<TopologyEdge>());
                list.Add(edge);
            }
        }
        foreach (var node in chart.Nodes) {
            // A hidden leaf is an authored routing anchor; grouping must not turn it into a visible row.
            if (hidden != null && hidden(node)) continue;
            if (pinned.Contains(node.Id) || node.TableContent != null || node.SiblingGroup != null || !incident.TryGetValue(node.Id, out var edges)) continue;
            if (chart.Groups.Any(group => string.Equals(group.Id, node.Id, StringComparison.Ordinal))) continue;
            var parents = edges.Select(edge => edge.SourceNodeId == node.Id ? edge.TargetNodeId : edge.SourceNodeId).Distinct(StringComparer.Ordinal).ToArray();
            if (parents.Length != 1 || parents[0] == node.Id || !nodes.ContainsKey(parents[0])) continue;
            if (edges.Any(edge => edge.Waypoints.Count > 0 || edge.PathGeometry != null)) continue;
            yield return (node, parents[0], edges);
        }
    }

    private static HashSet<string> PinnedIds(TopologyChart chart, TopologyView? view = null, TopologyRenderOptions? options = null) {
        var pinned = new HashSet<string>(StringComparer.Ordinal);
        // An edge step needs its edge to survive, so both of its ends stay out of any card.
        var edges = chart.Edges.GroupBy(edge => edge.Id, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        foreach (var scenario in chart.Scenarios) foreach (var step in scenario.Steps) {
            pinned.Add(step.Id);
            if (step.Kind == TopologyScenarioStepKind.Edge && edges.TryGetValue(step.Id, out var edge)) { pinned.Add(edge.SourceNodeId); pinned.Add(edge.TargetNodeId); }
        }
        foreach (var alignment in chart.Alignments) foreach (var id in alignment.NodeIds) pinned.Add(id);
        foreach (var note in chart.Annotations) if (note.TargetId != null) pinned.Add(note.TargetId);
        foreach (var edge in chart.Edges) if (edge.LabelAnchorNodeId != null) pinned.Add(edge.LabelAnchorNodeId);
        // A view selects authored identities, so the nodes it names, focuses on or reaches through named edges stay as they are.
        if (view != null) {
            foreach (var id in view.NodeIds.Concat(view.FocusNodeIds)) pinned.Add(id);
            foreach (var id in view.EdgeIds) if (edges.TryGetValue(id, out var edge)) { pinned.Add(edge.SourceNodeId); pinned.Add(edge.TargetNodeId); }
        }
        // Highlighted and selected identities are read back against the prepared chart, so they stay as authored.
        if (options != null) {
            foreach (var id in options.HighlightNodeIds.Concat(options.SelectedNodeIds)) pinned.Add(id);
            foreach (var id in options.HighlightEdgeIds.Concat(options.SelectedEdgeIds))
                if (edges.TryGetValue(id, out var edge)) { pinned.Add(edge.SourceNodeId); pinned.Add(edge.TargetNodeId); }
        }
        return pinned;
    }

    /// <summary>Lays a parent's many leaf children out as two offset rows, so every child keeps a clear line to its parent.</summary>
    /// <remarks>Layered layout wraps a wide rank into rows stacked straight under each other, which routes the lower row
    /// through the upper one. Offsetting the lower row by half a slot leaves each of its links a free corridor.</remarks>
    internal static void Stagger(TopologyChart chart) {
        var policy = chart.SiblingGrouping;
        if (policy == null || policy.StaggerCount == 0 || chart.LayoutMode != TopologyLayoutMode.Layered) return;
        var vertical = chart.LayoutDirection is TopologyLayoutDirection.TopToBottom or TopologyLayoutDirection.BottomToTop;
        var sign = chart.LayoutDirection is TopologyLayoutDirection.BottomToTop or TopologyLayoutDirection.RightToLeft ? -1 : 1;
        double Rank(TopologyNode node) => vertical ? node.Y : node.X;
        double Along(TopologyNode node) => vertical ? node.X : node.Y;
        double Depth(TopologyNode node) => vertical ? node.Height : node.Width;
        double Breadth(TopologyNode node) => vertical ? node.Width : node.Height;
        void Place(TopologyNode node, double along, double rank) { if (vertical) { node.X = along; node.Y = rank; } else { node.Y = along; node.X = rank; } }
        var nodes = chart.Nodes.GroupBy(node => node.Id, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var pinned = PinnedIds(chart);
        foreach (var family in Leaves(chart, nodes, pinned, node => (node.DisplayMode ?? chart.RenderOptions?.NodeDisplayMode) == TopologyNodeDisplayMode.Hidden).GroupBy(leaf => leaf.Parent, StringComparer.Ordinal).ToArray()) {
            var parent = nodes[family.Key];
            var children = family.Select(leaf => leaf.Node).Where(node => sign * (Rank(node) - Rank(parent)) > 0)
                .OrderBy(node => sign * Rank(node)).ThenBy(Along).ToArray();
            if (children.Length < policy.StaggerCount) continue;
            var breadth = children.Max(Breadth); var depth = children.Max(Depth) + 28;
            var pitch = breadth + 36;
            var first = sign > 0 ? children.Min(Rank) : children.Max(Rank);
            var center = vertical ? parent.X + parent.Width / 2 : parent.Y + parent.Height / 2;
            var start = center - (pitch * (children.Length - 1) / 2 + breadth) / 2;
            var low = start; var high = start + pitch * (children.Length - 1) / 2 + breadth;
            var rankLow = Math.Min(first, first + sign * depth); var rankHigh = Math.Max(first, first + sign * depth) + children.Max(Depth);
            // Another family in the same band keeps the layered placement rather than being overlapped.
            if (chart.Nodes.Any(other => !children.Contains(other) && Rank(other) + Depth(other) > rankLow && Rank(other) < rankHigh
                && Along(other) + Breadth(other) > low && Along(other) < high)) continue;
            var oldExtent = children.Max(node => sign * Rank(node));
            var newExtent = sign * (first + sign * depth);
            var shift = newExtent - oldExtent;
            if (shift > 0) foreach (var other in chart.Nodes) {
                if (children.Contains(other) || sign * Rank(other) <= oldExtent + .5) continue;
                if (vertical) other.Y += sign * shift; else other.X += sign * shift;
            }
            for (var index = 0; index < children.Length; index++) {
                Place(children[index], start + index * pitch / 2, first + (index % 2 == 1 ? sign * depth : 0));
                children[index].Metadata["siblingGroup.staggerRow"] = (index % 2).ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (index % 2 == 0) continue;
                // The offset row joins the parent's bus above the near row instead of turning at its own midpoint.
                foreach (var edge in chart.Edges) {
                    if (edge.SourceNodeId != children[index].Id && edge.TargetNodeId != children[index].Id) continue;
                    if (edge.Routing != TopologyEdgeRouting.Orthogonal || edge.HasRouteLaneOverride) continue;
                    edge.RouteLane = -sign * depth / 2; edge.HasRouteLaneOverride = true;
                }
            }
        }
    }

    private static TopologyHealthStatus Worst(IEnumerable<TopologyHealthStatus> statuses) {
        var all = statuses.ToArray();
        foreach (var status in new[] { TopologyHealthStatus.Critical, TopologyHealthStatus.Warning, TopologyHealthStatus.Healthy })
            if (all.Contains(status)) return status;
        return all.All(status => status == TopologyHealthStatus.Disabled) && all.Length > 0 ? TopologyHealthStatus.Disabled : TopologyHealthStatus.Unknown;
    }

    private static string? MarkerKey(TopologyMarkerStyle? style) => style == null ? null
        : string.Join(":", style.Scale.ToString("R", System.Globalization.CultureInfo.InvariantCulture), style.Offset.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            style.StrokeWidth?.ToString("R", System.Globalization.CultureInfo.InvariantCulture) ?? "-");

    private static T? SharedValue<T>(IEnumerable<TopologyEdge> edges, Func<TopologyEdge, T?> value) where T : struct {
        var values = edges.Select(value).Distinct().ToArray();
        return values.Length == 1 ? values[0] : null;
    }

    private static string? Signature(IEnumerable<string> parts) { var text = string.Join("\u001e", parts); return text.Length == 0 ? null : text; }

    private static string? Shared<T>(IEnumerable<T> items, Func<T, string?> value) {
        var values = items.Select(value).Distinct(StringComparer.Ordinal).ToArray();
        return values.Length == 1 && !string.IsNullOrWhiteSpace(values[0]) ? values[0] : null;
    }

    private static string UniqueId(TopologyChart chart, string id) {
        var candidate = id;
        for (var index = 2; chart.Nodes.Any(node => node.Id == candidate) || chart.Groups.Any(group => group.Id == candidate); index++) candidate = id + index;
        return candidate;
    }

    private static string UniqueEdgeId(TopologyChart chart, string id) {
        var candidate = id;
        for (var index = 2; chart.Edges.Any(edge => edge.Id == candidate); index++) candidate = id + index;
        return candidate;
    }
}

internal static class TopologySiblingCardText {
    internal const double TitleSize = 11.5, NameSize = 10.5, SecondarySize = 9.5, ChipSize = 8.5, RowTextLeft = 34;
}

public sealed partial class TopologyChart {
    /// <summary>Gets or sets the opt-in sibling grouping policy. Null, the default, keeps every node as authored.</summary>
    public TopologySiblingGrouping? SiblingGrouping { get; set; }
}

public sealed partial class TopologyNode {
    internal TopologySiblingCard? SiblingGroup { get; set; }
}
