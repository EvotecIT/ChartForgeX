using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Topology;

internal static partial class TopologyLayoutEngine {
    /// <summary>Reserves each standard subtree's full breadth before centering its manager over its reports.</summary>
    private static void PlaceStandardHierarchyBranches(TopologyChart chart, ISet<string> automaticNodes, bool horizontal) {
        // Explicit positions and mixed packing policies retain their authored layout.
        if (chart.Nodes.Count == 0 || chart.Nodes.Any(node => !automaticNodes.Contains(node.Id)
            || !node.Metadata.TryGetValue("hierarchy.layoutPolicy", out var policy) || policy != nameof(TopologyHierarchyLayoutPolicy.Standard))) return;
        var byId = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var children = chart.Nodes.Where(node => ParentId(node) is string parent && byId.ContainsKey(parent))
            .GroupBy(node => ParentId(node)!, StringComparer.Ordinal).ToDictionary(group => group.Key,
                group => group.OrderBy(node => horizontal ? node.Y : node.X).ToArray(), StringComparer.Ordinal);
        var roots = chart.Nodes.Where(node => ParentId(node) is not string parent || !byId.ContainsKey(parent))
            .OrderBy(node => horizontal ? node.Y : node.X).ToArray();
        var spans = new Dictionary<string, double>(StringComparer.Ordinal);
        var gap = horizontal ? HierarchyBucketNodeGapY : HierarchyBucketGap;
        var start = Math.Max(24, chart.Viewport.Padding);
        var available = (horizontal ? chart.Viewport.Height : chart.Viewport.Width) - start * 2;
        var total = roots.Sum(Span) + Math.Max(0, roots.Length - 1) * gap;
        var cursor = start + (available - total) / 2;
        foreach (var root in roots) { Place(root, cursor); cursor += Span(root) + gap; }

        double Extent(TopologyNode node) => horizontal ? LayeredNodeHeight(chart, node) : LayeredNodeWidth(chart, node);
        double Span(TopologyNode node) {
            if (spans.TryGetValue(node.Id, out var span)) return span;
            var breadth = children.TryGetValue(node.Id, out var reports) ? reports.Sum(Span) + (reports.Length - 1) * gap : 0;
            return spans[node.Id] = Math.Max(Extent(node), breadth);
        }
        void Place(TopologyNode node, double left) {
            var center = left + Span(node) / 2;
            if (horizontal) node.Y = center - node.Height / 2;
            else node.X = center - node.Width / 2;
            if (!children.TryGetValue(node.Id, out var reports)) return;
            var breadth = reports.Sum(Span) + (reports.Length - 1) * gap;
            var childLeft = center - breadth / 2;
            foreach (var report in reports) { Place(report, childLeft); childLeft += Span(report) + gap; }
        }
    }
}
