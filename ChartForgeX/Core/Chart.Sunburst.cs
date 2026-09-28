using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a dependency-free sunburst partition chart from parent-child links.
    /// </summary>
    public Chart AddSunburst(string name, IEnumerable<ChartTreeLink> links, ChartColor? color = null) {
        EnsureCanAddSeries();
        if (links == null) throw new ArgumentNullException(nameof(links));
        var materialized = links.ToList();
        if (materialized.Count == 0) throw new ArgumentException("Sunburst charts require at least one link.", nameof(links));
        var nodeIndexes = new Dictionary<string, int>(StringComparer.Ordinal);
        var nodeLabels = new List<string>();
        var children = new Dictionary<int, List<int>>();
        var parents = new Dictionary<int, int>();
        var points = new List<ChartPoint>(materialized.Count * 2);
        foreach (var link in materialized) {
            if (string.IsNullOrWhiteSpace(link.Parent)) throw new ArgumentException("Sunburst parent must not be empty.", nameof(links));
            if (string.IsNullOrWhiteSpace(link.Child)) throw new ArgumentException("Sunburst child must not be empty.", nameof(links));
            ChartGuards.Finite(link.Value, nameof(links));
            if (link.Value <= 0) throw new ArgumentOutOfRangeException(nameof(links), link.Value, "Sunburst link value must be positive.");
            if (string.Equals(link.Parent, link.Child, StringComparison.Ordinal)) throw new ArgumentException("Sunburst links must connect distinct parent and child nodes.", nameof(links));
            var parent = GetTreeNodeIndex(link.Parent, nodeIndexes, nodeLabels);
            var child = GetTreeNodeIndex(link.Child, nodeIndexes, nodeLabels);
            if (parents.ContainsKey(child)) throw new ArgumentException("Sunburst child nodes can only have one parent.", nameof(links));
            parents[child] = parent;
            if (!children.TryGetValue(parent, out var list)) {
                list = new List<int>();
                children[parent] = list;
            }

            list.Add(child);
            points.Add(new ChartPoint(parent, child));
            points.Add(new ChartPoint(link.Value, link.Value));
        }

        var roots = Enumerable.Range(0, nodeLabels.Count).Where(index => !parents.ContainsKey(index)).ToArray();
        if (roots.Length != 1) throw new ArgumentException("Sunburst charts require exactly one root node.", nameof(links));
        ValidateTreeAcyclic(roots[0], nodeLabels.Count, children, nameof(links));
        Options.TreeNodeLabels.Clear();
        Options.TreeNodeLabels.AddRange(nodeLabels);
        return Add(name, ChartSeriesKind.Sunburst, points, color);
    }
}
