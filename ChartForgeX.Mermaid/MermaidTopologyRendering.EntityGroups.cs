using ChartForgeX.Topology;

namespace ChartForgeX.Mermaid;

public static partial class MermaidTopologyRendering {
    private static void ApplyEntityGroups(TopologyChart chart, MermaidEntityRelationshipDocument document) {
        var fallback = DiagramDirection(document.Direction, TopologyLayoutDirection.LeftToRight);
        foreach (var item in document.Groups) {
            chart.AddAutoGroup(item.Id, item.Label);
            var group = chart.Groups[chart.Groups.Count - 1];
            group.ParentGroupId = item.ParentId;
            group.LayoutDirection = item.Direction == null ? null : DiagramDirection(item.Direction, fallback);
        }
        foreach (var item in document.Entities) {
            var node = chart.Nodes.Find(candidate => candidate.Id == item.Id);
            if (node != null) node.GroupId = item.GroupId;
        }
    }
}
