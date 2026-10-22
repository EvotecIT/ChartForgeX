using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Topology;
using ChartForgeX.Typography;

namespace ChartForgeX.Mermaid;

public static partial class MermaidTopologyRendering {
    private static void AddClassNamespaceGroups(TopologyChart chart, MermaidClassDocument document) {
        var declared = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in document.Namespaces) {
            chart.AddAutoGroup(item.Id, item.Label);
            chart.Groups[chart.Groups.Count - 1].ParentGroupId = item.ParentId;
            declared.Add(item.Id);
        }
        // A typed class may refer to an implicit namespace without an explicit declaration object.
        foreach (var item in document.Classes) {
            if (item.Namespace == null) continue;
            string? parent = null;
            foreach (var segment in item.Namespace.Split('.')) {
                var id = parent == null ? segment : parent + "." + segment;
                if (declared.Add(id)) {
                    chart.AddAutoGroup(id, segment);
                    chart.Groups[chart.Groups.Count - 1].ParentGroupId = parent;
                }
                parent = id;
            }
        }
    }

    private static void ApplyClassNotation(TopologyChart chart, MermaidClassDocument document) {
        foreach (var item in document.Classes) {
            var node = chart.Nodes.Find(candidate => candidate.Id == item.Id);
            if (node == null) continue;
            node.Href = item.Href;
            node.Tooltip = item.Tooltip;
            node.Details.Clear();
            foreach (var member in item.Members) {
                var detail = new TopologyNodeDetail { Text = member.DisplayText };
                if (member.IsAbstract || member.IsStatic) detail.TextStyle = new TextStyleOverride { Italic = member.IsAbstract, Underline = member.IsStatic };
                detail.Metadata["mermaid.member.kind"] = member.IsMethod ? "method" : "attribute";
                if (member.Visibility.HasValue) detail.Metadata["mermaid.member.visibility"] = member.Visibility.Value.ToString();
                if (member.IsAbstract) detail.Metadata["mermaid.member.abstract"] = "true";
                if (member.IsStatic) detail.Metadata["mermaid.member.static"] = "true";
                node.Details.Add(detail);
            }
        }
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in chart.Nodes) ids.Add(item.Id);
        foreach (var item in chart.Groups) ids.Add(item.Id);
        for (var index = 0; index < document.Relationships.Count; index++) {
            var relation = document.Relationships[index];
            if (!relation.SourceIsInterface && !relation.TargetIsInterface) continue;
            var edge = chart.Edges.Find(candidate => candidate.Id == EdgeId(index));
            if (edge == null) continue;
            var rawId = relation.SourceIsInterface ? relation.SourceId : relation.TargetId;
            var concreteId = relation.SourceIsInterface ? relation.TargetId : relation.SourceId;
            var concrete = chart.Nodes.Find(node => node.Id == concreteId);
            var id = "mermaid-interface-" + index.ToString(CultureInfo.InvariantCulture);
            while (!ids.Add(id)) id += "-interface";
            chart.AddAutoNode(id, rawId, TopologyNodeKind.Generic, TopologyHealthStatus.Unknown,
                groupId: concrete?.GroupId, width: 20, height: 20, cssClass: "cfx-mermaid-class-interface");
            var target = chart.Nodes[chart.Nodes.Count - 1];
            target.Shape = TopologyNodeShape.Circle;
            target.ShowLabel = false;
            target.ShowStatusBadge = false;
            target.PreserveDisplayModeSize = true;
            target.Metadata["mermaid.id"] = rawId;
            target.Metadata["mermaid.kind"] = "interface";
            if (relation.SourceIsInterface) { edge.SourceNodeId = id; edge.SourceLabel = rawId; }
            else { edge.TargetNodeId = id; edge.TargetLabel = rawId; }
        }
    }
}
