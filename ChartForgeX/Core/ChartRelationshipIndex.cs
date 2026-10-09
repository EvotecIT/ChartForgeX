using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Core;

// Authored order controls layout and ordinal styling; only explicit IDs identify relationships.
internal sealed class ChartRelationshipIndex {
    internal IReadOnlyList<ChartNode> Nodes { get; }
    internal IReadOnlyList<ChartFlowLink> FlowLinks { get; }
    internal IReadOnlyList<double> FlowIncomingValues { get; }
    internal IReadOnlyList<double> FlowOutgoingValues { get; }
    internal IReadOnlyList<ChartTreeLink> TreeLinks { get; }
    internal IReadOnlyList<double> HierarchyValues { get; }
    internal int Root { get; }
    private readonly Dictionary<string, int> _nodeIndexes;
    private readonly int[] _sources;
    private readonly int[] _targets;
    private readonly int[] _incomingLinks;

    private ChartRelationshipIndex(ChartNode[] nodes, ChartFlowLink[] sankeyLinks, ChartTreeLink[] treeLinks,
        Dictionary<string, int> nodeIndexes, int[] sources, int[] targets, int root, double[] hierarchyValues,
        double[]? flowIncomingValues = null, double[]? flowOutgoingValues = null) {
        Nodes = Array.AsReadOnly(nodes);
        FlowLinks = Array.AsReadOnly(sankeyLinks);
        FlowIncomingValues = Array.AsReadOnly(flowIncomingValues ?? Array.Empty<double>());
        FlowOutgoingValues = Array.AsReadOnly(flowOutgoingValues ?? Array.Empty<double>());
        TreeLinks = Array.AsReadOnly(treeLinks);
        HierarchyValues = Array.AsReadOnly(hierarchyValues);
        _nodeIndexes = nodeIndexes;
        _sources = sources;
        _targets = targets;
        Root = root;
        _incomingLinks = treeLinks.Length == 0 ? Array.Empty<int>() : Enumerable.Repeat(-1, nodes.Length).ToArray();
        for (var i = 0; i < treeLinks.Length; i++) _incomingLinks[targets[i]] = i;
    }

    internal int Source(int linkIndex) => _sources[linkIndex];
    internal int Target(int linkIndex) => _targets[linkIndex];
    internal int IncomingLink(int nodeIndex) => _incomingLinks[nodeIndex];
    internal bool ContainsNode(string id) => _nodeIndexes.ContainsKey(id);

    internal static ChartRelationshipIndex Sankey(IEnumerable<ChartNode> nodes, IEnumerable<ChartFlowLink> links) {
        var snapshot = SnapshotNodes(nodes, out var indexes);
        if (links == null) throw new ArgumentNullException(nameof(links));
        var flows = links.ToArray();
        if (flows.Length == 0) throw new ArgumentException("Sankey charts require at least one link.", nameof(links));
        var sources = new int[flows.Length];
        var targets = new int[flows.Length];
        var incoming = new double[snapshot.Length];
        var outgoing = new double[snapshot.Length];
        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < flows.Length; i++) {
            var link = flows[i];
            if (string.IsNullOrWhiteSpace(link.Id) || !ids.Add(link.Id)) throw new ArgumentException("Sankey link IDs must be non-empty and unique.", nameof(links));
            ValidateWeight(link.Value);
            Resolve(indexes, link.SourceId, link.TargetId, sources, targets, i);
            incoming[targets[i]] += link.Value;
            outgoing[sources[i]] += link.Value;
            if (double.IsInfinity(incoming[targets[i]]) || double.IsInfinity(outgoing[sources[i]]))
                throw new ArgumentException("Sankey node aggregates must remain finite.", nameof(links));
        }
        for (var i = 0; i < snapshot.Length; i++)
            if (incoming[i] == 0 && outgoing[i] == 0)
                throw new ArgumentException("Sankey nodes must be referenced by at least one flow.", nameof(nodes));
        TopologicalOrder(snapshot.Length, sources, targets, false, out _);
        return new ChartRelationshipIndex(snapshot, flows, Array.Empty<ChartTreeLink>(), indexes, sources, targets, -1, Array.Empty<double>(), incoming, outgoing);
    }

    internal static ChartRelationshipIndex Hierarchy(IEnumerable<ChartNode> nodes, IEnumerable<ChartTreeLink> links, bool aggregateLeaves) {
        var snapshot = SnapshotNodes(nodes, out var indexes);
        if (links == null) throw new ArgumentNullException(nameof(links));
        var branches = links.ToArray();
        if (branches.Length == 0) throw new ArgumentException("Hierarchy charts require at least one link.", nameof(links));
        var sources = new int[branches.Length];
        var targets = new int[branches.Length];
        var children = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < branches.Length; i++) {
            var link = branches[i];
            ValidateWeight(link.Value);
            Resolve(indexes, link.ParentId, link.ChildId, sources, targets, i);
            if (!children.Add(link.ChildId)) throw new ArgumentException("Hierarchy child IDs can only have one incoming link.", nameof(links));
        }
        var order = TopologicalOrder(snapshot.Length, sources, targets, true, out var root);
        var values = aggregateLeaves ? AggregateLeaves(snapshot.Length, branches, sources, targets, order) : Array.Empty<double>();
        return new ChartRelationshipIndex(snapshot, Array.Empty<ChartFlowLink>(), branches, indexes, sources, targets, root, values);
    }

    private static ChartNode[] SnapshotNodes(IEnumerable<ChartNode> nodes, out Dictionary<string, int> indexes) {
        if (nodes == null) throw new ArgumentNullException(nameof(nodes));
        var snapshot = nodes.ToArray();
        if (snapshot.Length == 0) throw new ArgumentException("Relationship charts require explicit nodes.", nameof(nodes));
        indexes = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < snapshot.Length; i++) {
            var node = snapshot[i];
            if (string.IsNullOrWhiteSpace(node.Id) || string.IsNullOrWhiteSpace(node.Label)) throw new ArgumentException("Nodes require a non-empty ID and label.", nameof(nodes));
            if (indexes.ContainsKey(node.Id)) throw new ArgumentException("Node IDs must be unique.", nameof(nodes));
            indexes.Add(node.Id, i);
        }
        return snapshot;
    }

    private static void ValidateWeight(double value) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0) throw new ArgumentException("Relationship weights must be finite and positive.", "links");
    }

    private static void Resolve(Dictionary<string, int> nodes, string source, string target, int[] sources, int[] targets, int index) {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target) || !nodes.TryGetValue(source, out var from) || !nodes.TryGetValue(target, out var to))
            throw new ArgumentException("Links must reference existing node IDs.", "links");
        if (from == to) throw new ArgumentException("Links must connect distinct nodes.", "links");
        sources[index] = from;
        targets[index] = to;
    }

    private static int[] TopologicalOrder(int count, int[] sources, int[] targets, bool hierarchy, out int root) {
        var outgoing = new List<int>[count];
        var incoming = new int[count];
        for (var i = 0; i < count; i++) outgoing[i] = new List<int>();
        for (var i = 0; i < sources.Length; i++) { outgoing[sources[i]].Add(targets[i]); incoming[targets[i]]++; }
        var queue = new Queue<int>();
        for (var i = 0; i < count; i++) if (incoming[i] == 0) queue.Enqueue(i);
        root = queue.Count == 1 ? queue.Peek() : -1;
        if (hierarchy && root < 0) throw new ArgumentException("Hierarchy charts require exactly one root node.", "links");
        var order = new int[count];
        var depths = new int[count];
        var visited = 0;
        while (queue.Count > 0) {
            var source = queue.Dequeue();
            order[visited++] = source;
            foreach (var target in outgoing[source]) {
                depths[target] = Math.Max(depths[target], depths[source] + 1);
                if (hierarchy && depths[target] > 512) throw new ArgumentException("Hierarchy depth exceeds the supported limit of 512.", "links");
                if (--incoming[target] == 0) queue.Enqueue(target);
            }
        }
        if (visited != count) throw new ArgumentException("Links must form an acyclic graph; hierarchy nodes must all connect to the root.", "links");
        return order;
    }

    private static double[] AggregateLeaves(int count, ChartTreeLink[] links, int[] sources, int[] targets, int[] order) {
        var values = new double[count];
        var children = new List<int>[count];
        for (var i = 0; i < count; i++) children[i] = new List<int>();
        for (var i = 0; i < links.Length; i++) { values[targets[i]] = links[i].Value; children[sources[i]].Add(targets[i]); }
        for (var i = order.Length - 1; i >= 0; i--) {
            var node = order[i];
            if (children[node].Count == 0) continue;
            var total = 0d;
            foreach (var child in children[node]) total += values[child];
            if (double.IsInfinity(total)) throw new ArgumentException("Sunburst leaf aggregates must remain finite.", nameof(links));
            values[node] = total;
        }
        return values;
    }
}
