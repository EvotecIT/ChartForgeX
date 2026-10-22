using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Topology;

namespace ChartForgeX.Mermaid;

public static partial class MermaidTopologyRendering {
    /// <summary>Projects state hierarchy and concurrent regions through native group ownership.</summary>
    private static TopologyChart CreateStateTopology(MermaidStateDocument document, MermaidTopologyRenderOptions options) {
        var direction = DiagramDirection(document.Direction, TopologyLayoutDirection.TopToBottom);
        var chart = CreateTopology(options, "mermaid-state", ResolveTitle(document, options, "Mermaid state diagram"), TopologyLayoutMode.Layered, direction);
        var concurrent = document.Regions.GroupBy(region => region.CompositeStateId, StringComparer.Ordinal)
            .Where(group => group.Count() > 1).ToDictionary(group => group.Key, group => group.OrderBy(region => region.Index).ToArray(), StringComparer.Ordinal);
        var regions = new Dictionary<string, string>(StringComparer.Ordinal);
        var usedIds = new HashSet<string>(document.States.Select(state => state.Id), StringComparer.Ordinal);
        foreach (var region in document.Regions.Where(region => concurrent.ContainsKey(region.CompositeStateId))) {
            var id = region.Id;
            while (!usedIds.Add(id)) id += "-region";
            regions.Add(region.Id, id);
        }
        foreach (var composite in document.States.Where(state => state.IsComposite || document.States.Exists(item => item.ParentId == state.Id))) {
            chart.AddAutoGroup(composite.Id, composite.Label ?? composite.Id, TopologyHealthStatus.Unknown, cssClass: "cfx-mermaid-state-composite");
            var group = chart.Groups[chart.Groups.Count - 1];
            group.ParentGroupId = NativeParent(composite);
            group.LayoutDirection = composite.Direction == null ? null : DiagramDirection(composite.Direction, direction);
        }
        foreach (var region in document.Regions.Where(region => regions.ContainsKey(region.Id))) {
            chart.AddAutoGroup(regions[region.Id], "Region " + (region.Index + 1).ToString(CultureInfo.InvariantCulture));
            var group = chart.Groups[chart.Groups.Count - 1];
            group.ParentGroupId = region.CompositeStateId;
            group.Presentation = TopologyGroupPresentation.Compartment;
            group.LayoutDirection = region.Direction == null ? null : DiagramDirection(region.Direction, direction);
        }
        foreach (var item in document.States) {
            if (chart.Groups.Exists(group => group.Id == item.Id)) continue;
            var localDirection = RegionStateDirection(document, item, direction);
            var pseudo = IsPseudoState(item);
            chart.AddAutoNode(item.Id, pseudo ? item.Kind == "start" ? "Start" : "End" : item.Label ?? item.Id,
                TopologyNodeKind.Generic, TopologyHealthStatus.Unknown, groupId: NativeParent(item),
                width: ToStateWidth(item, localDirection), height: ToStateHeight(item, localDirection), cssClass: "cfx-mermaid-state");
            var node = chart.Nodes[chart.Nodes.Count - 1];
            node.ShowStatusBadge = false;
            node.DisplayMode = pseudo ? TopologyNodeDisplayMode.Dot : TopologyNodeDisplayMode.Card;
            node.PreserveDisplayModeSize = true;
            node.Shape = ToStateShape(item);
            node.ShowLabel = !IsStateControl(item);
            node.Metadata["mermaid.id"] = item.Id;
            node.Metadata["mermaid.kind"] = item.Kind ?? "state";
            if (item.ParentId != null) node.Metadata["mermaid.parent"] = item.ParentId;
        }
        for (var index = 0; index < document.Transitions.Count; index++) {
            var item = document.Transitions[index];
            chart.AddEdge(EdgeId(index), item.SourceId, item.TargetId, item.Label, TopologyEdgeKind.Dependency,
                TopologyHealthStatus.Unknown, VisualLinkDirection.Forward, TopologyEdgeRouting.Orthogonal);
            var edge = chart.Edges[chart.Edges.Count - 1];
            edge.MinimumRankSpan = 2;
            edge.LineStyle = TopologyEdgeLineStyle.Solid;
        }
        ApplyStateStyles(chart, document);
        AddNotes(chart, document.Notes);
        return MermaidPresentation.Apply(chart, document, options.Presentation);

        string? NativeParent(MermaidStateNode state) {
            if (state.ParentId == null || !concurrent.TryGetValue(state.ParentId, out var parentRegions)) return state.ParentId;
            var regionId = state.RegionId ?? parentRegions[0].Id;
            if (!parentRegions.Any(region => region.Id == regionId) || !regions.TryGetValue(regionId, out var id))
                throw new ArgumentException("State '" + state.Id + "' references an unknown concurrent region.", nameof(document));
            return id;
        }
    }

    private static TopologyLayoutDirection RegionStateDirection(MermaidStateDocument document, MermaidStateNode node, TopologyLayoutDirection fallback) {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var current = node;
        while (current.ParentId != null && visited.Add(current.Id)) {
            var region = document.Regions.Find(item => item.Id == current.RegionId);
            if (region?.Direction != null) return DiagramDirection(region.Direction, fallback);
            var parent = document.States.Find(state => state.Id == current.ParentId);
            if (parent == null) break;
            if (parent.Direction != null) return DiagramDirection(parent.Direction, fallback);
            current = parent;
        }
        return fallback;
    }
}
