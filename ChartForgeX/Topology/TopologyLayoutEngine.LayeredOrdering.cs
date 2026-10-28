using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Topology;

internal static partial class TopologyLayoutEngine {
    private static List<LayerNodeGroup> OrderLayeredGroups(IEnumerable<IGrouping<int, TopologyNode>> groups, TopologyHierarchyView? view = null) {
        var ordered = new List<LayerNodeGroup>();
        var orderByNodeId = new Dictionary<string, double>(StringComparer.Ordinal);
        var index = 0;
        var explicitOrder = view?.NodeOrder.Select((id, ordinal) => (id, ordinal)).ToDictionary(item => item.id, item => item.ordinal, StringComparer.Ordinal);
        foreach (var group in groups) {
            var nodes = explicitOrder == null || explicitOrder.Count == 0
                ? group.OrderBy(node => ParentOrder(node, orderByNodeId)).ThenBy(node => node.Id, StringComparer.Ordinal).ToList()
                : group.OrderBy(node => ParentOrder(node, orderByNodeId))
                    .ThenBy(node => explicitOrder.TryGetValue(node.Id, out var ordinal) ? ordinal : int.MaxValue)
                    .ThenBy(node => node.Id, StringComparer.Ordinal).ToList();
            for (var i = 0; i < nodes.Count; i++) orderByNodeId[nodes[i].Id] = i;
            ordered.Add(new LayerNodeGroup(index, group.Key, nodes));
            index++;
        }

        return ordered;
    }

    private static double ParentOrder(TopologyNode node, IReadOnlyDictionary<string, double> orderByNodeId) {
        if (node.Metadata.TryGetValue("hierarchy.parentId", out var parentId) && orderByNodeId.TryGetValue(parentId, out var parentOrder)) return parentOrder;
        return double.MaxValue;
    }

    private sealed class LayerNodeGroup {
        public LayerNodeGroup(int index, int layer, List<TopologyNode> nodes) {
            Index = index;
            Layer = layer;
            Nodes = nodes;
        }

        public int Index { get; }
        public int Layer { get; }
        public List<TopologyNode> Nodes { get; }
    }
}
