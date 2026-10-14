using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Topology;

namespace ChartForgeX.Mermaid;

public static partial class MermaidTopologyRendering {
    /// <summary>Converts a Mermaid mindmap to a topology chart with theme palette slots selected by branch or structural depth.</summary>
    /// <remarks>The root has its own section. Non-root sections cycle after eleven; the active theme palette may cycle sooner. Explicit topology colors override categorical slots.</remarks>
    public static TopologyChart ToTopologyChart(this MermaidMindMapDocument document, MermaidTopologyRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidTopologyRenderOptions();
        var title = document.Nodes.Count == 0 ? ResolveTitle(options, "Mermaid mindmap") : ResolveTitle(options, document.Nodes[0].Text);
        var chart = CreateTopology(options, "mermaid-mindmap", title, document.Header, TopologyLayoutMode.MindMap, TopologyLayoutDirection.LeftToRight);
        chart.DefaultRenderOptions = new TopologyRenderOptions().WithMindMapStyle();
        var sections = MindMapSections(document);
        foreach (var item in document.Nodes) {
            chart.AddAutoNode(item.Id, item.Text, item.ParentId == null ? TopologyNodeKind.Hub : TopologyNodeKind.Branch, TopologyHealthStatus.Unknown, width: item.ParentId == null ? 150 : 120, height: 58, symbol: item.ParentId == null ? "M" : null, cssClass: "cfx-mermaid-mindmap-node");
            var node = chart.Nodes[chart.Nodes.Count - 1];
            var section = sections[item.Id];
            node.PaletteIndex = section.Section + 1;
            node.Metadata["mermaid.shape"] = item.Shape ?? "default";
            node.Metadata["mermaid.level"] = section.Depth.ToString(CultureInfo.InvariantCulture);
            node.Metadata["mermaid.section"] = section.Section.ToString(CultureInfo.InvariantCulture);
            node.Metadata["mindmap.level"] = node.Metadata["mermaid.level"];
            if (item.ParentId == null) node.Metadata["mindmap.root"] = "true";
            else node.Metadata["mindmap.parentId"] = item.ParentId;
            if (item.Classes.Count > 0) node.Metadata["mermaid.classes"] = string.Join(",", item.Classes);
            if (item.Icons.Count > 0) node.Metadata["mermaid.icons"] = string.Join(",", item.Icons);
            if (item.ParentId != null) {
                chart.AddEdge("mindmap-edge-" + item.Id, item.ParentId, item.Id, null, TopologyEdgeKind.Dependency, TopologyHealthStatus.Unknown, VisualLinkDirection.Forward, TopologyEdgeRouting.Curved);
                var edge = chart.Edges[chart.Edges.Count - 1];
                edge.PaletteIndex = node.PaletteIndex;
                edge.Metadata["mermaid.section"] = node.Metadata["mermaid.section"];
            }
        }

        return MermaidPresentation.Apply(chart, document);
    }

    private static Dictionary<string, (int Depth, int Section)> MindMapSections(MermaidMindMapDocument document) {
        var nodes = new Dictionary<string, MermaidMindMapNode>(StringComparer.Ordinal);
        foreach (var node in document.Nodes) nodes.Add(node.Id, node);
        var branches = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var node in document.Nodes) {
            if (node.ParentId != null && nodes.TryGetValue(node.ParentId, out var parent) && parent.ParentId == null)
                branches.Add(node.Id, branches.Count % 11);
        }
        var sections = new Dictionary<string, (int Depth, int Section)>(StringComparer.Ordinal);
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in document.Nodes) Resolve(node);
        return sections;

        (int Depth, int Section) Resolve(MermaidMindMapNode node) {
            if (sections.TryGetValue(node.Id, out var found)) return found;
            if (!visiting.Add(node.Id)) throw new ArgumentException("Mindmap parent relationships contain a cycle.", nameof(document));
            var section = (Depth: 0, Section: -1);
            if (node.ParentId != null) {
                if (!nodes.TryGetValue(node.ParentId, out var parent)) throw new ArgumentException("Mindmap parent '" + node.ParentId + "' was not found.", nameof(document));
                var parentSection = Resolve(parent);
                section.Depth = parentSection.Depth + 1;
                section.Section = document.ColorBy == MermaidMindMapColorMode.Depth ? (section.Depth - 1) % 11
                    : parentSection.Depth == 0 ? branches[node.Id] : parentSection.Section;
            }
            visiting.Remove(node.Id);
            sections.Add(node.Id, section);
            return section;
        }
    }
}
