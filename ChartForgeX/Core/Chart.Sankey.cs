using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a dependency-free Sankey flow chart from weighted source-to-target links.
    /// </summary>
    public Chart AddSankey(string name, IEnumerable<ChartSankeyLink> links, ChartColor? color = null) {
        EnsureCanAddSeries();
        if (links == null) throw new ArgumentNullException(nameof(links));
        var materialized = links.ToList();
        if (materialized.Count == 0) throw new ArgumentException("Sankey charts require at least one link.", nameof(links));
        var nodeIndexes = new Dictionary<string, int>(StringComparer.Ordinal);
        var nodeLabels = new List<string>();
        var points = new List<ChartPoint>(materialized.Count * 2);
        foreach (var link in materialized) {
            if (string.Equals(link.Source, link.Target, StringComparison.Ordinal)) throw new ArgumentException("Sankey links must connect distinct source and target nodes.", nameof(links));
            var source = GetSankeyNodeIndex(link.Source, nodeIndexes, nodeLabels);
            var target = GetSankeyNodeIndex(link.Target, nodeIndexes, nodeLabels);
            points.Add(new ChartPoint(source, target));
            points.Add(new ChartPoint(link.Value, link.Value));
        }

        ValidateSankeyAcyclic(points, nodeLabels.Count, nameof(links));
        Options.SankeyNodeLabels.Clear();
        Options.SankeyNodeLabels.AddRange(nodeLabels);
        return Add(name, ChartSeriesKind.Sankey, points, color);
    }

    private static int GetSankeyNodeIndex(string label, Dictionary<string, int> nodeIndexes, List<string> nodeLabels) {
        if (nodeIndexes.TryGetValue(label, out var existing)) return existing;
        var index = nodeLabels.Count;
        nodeIndexes.Add(label, index);
        nodeLabels.Add(label);
        return index;
    }

    private static void ValidateSankeyAcyclic(IReadOnlyList<ChartPoint> points, int nodeCount, string parameterName) {
        var outgoing = new List<int>[nodeCount];
        for (var i = 0; i < outgoing.Length; i++) outgoing[i] = new List<int>();
        for (var i = 0; i + 1 < points.Count; i += 2) {
            var source = (int)Math.Round(points[i].X);
            var target = (int)Math.Round(points[i].Y);
            outgoing[source].Add(target);
        }

        var state = new int[nodeCount];
        for (var i = 0; i < state.Length; i++) Visit(i);

        void Visit(int node) {
            if (state[node] == 1) throw new ArgumentException("Sankey links must not contain cycles.", parameterName);
            if (state[node] == 2) return;
            state[node] = 1;
            foreach (var target in outgoing[node]) Visit(target);
            state[node] = 2;
        }
    }
}
