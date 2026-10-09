using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Core;

// Authored order controls layout and ordinal styling; only explicit IDs identify relationships.
internal sealed partial class ChartRelationshipIndex {
    internal IReadOnlyList<ChartNode> Nodes { get; }
    internal IReadOnlyList<ChartFlowLink> FlowLinks { get; }
    internal IReadOnlyList<ChartTreeLink> TreeLinks { get; }
    internal IReadOnlyList<ChartTreemapItem> TreemapItems { get; }
    internal IReadOnlyList<double> HierarchyValues { get; }
    internal IReadOnlyList<int> Depths { get; }
    internal IReadOnlyList<int> Roots { get; }
    internal int Root { get; }
    private readonly Dictionary<string, int> _nodeIndexes;
    private readonly int[] _sources;
    private readonly int[] _targets;
    private readonly int[] _incomingLinks;
    private readonly int[] _parents;
    private readonly IReadOnlyList<int>[] _children;

    private ChartRelationshipIndex(ChartNode[] nodes, ChartFlowLink[] flowLinks, ChartTreeLink[] treeLinks,
        Dictionary<string, int> nodeIndexes, int[] sources, int[] targets, int root, double[] hierarchyValues, ChartTreemapItem[]? treemapItems = null, int[]? depths = null) {
        Nodes = Array.AsReadOnly(nodes);
        FlowLinks = Array.AsReadOnly(flowLinks);
        TreeLinks = Array.AsReadOnly(treeLinks);
        TreemapItems = Array.AsReadOnly(treemapItems ?? Array.Empty<ChartTreemapItem>());
        HierarchyValues = Array.AsReadOnly(hierarchyValues);
        Depths = Array.AsReadOnly(depths ?? Array.Empty<int>());
        _nodeIndexes = nodeIndexes;
        _sources = sources;
        _targets = targets;
        Root = root;
        _incomingLinks = treeLinks.Length == 0 ? Array.Empty<int>() : Enumerable.Repeat(-1, nodes.Length).ToArray();
        for (var i = 0; i < treeLinks.Length; i++) _incomingLinks[targets[i]] = i;
        _parents = Enumerable.Repeat(-1, nodes.Length).ToArray();
        var children = ChildrenFor(nodes.Length, sources, targets);
        _children = children.Select(items => (IReadOnlyList<int>)items.AsReadOnly()).ToArray();
        for (var i = 0; i < targets.Length; i++) _parents[targets[i]] = sources[i];
        Roots = Array.AsReadOnly(Enumerable.Range(0, nodes.Length).Where(index => _parents[index] < 0).ToArray());
    }

    internal int Source(int linkIndex) => _sources[linkIndex];
    internal int Target(int linkIndex) => _targets[linkIndex];
    internal int IncomingLink(int nodeIndex) => _incomingLinks[nodeIndex];
    internal bool ContainsNode(string id) => _nodeIndexes.ContainsKey(id);
    internal int Parent(int nodeIndex) => _parents[nodeIndex];
    internal IReadOnlyList<int> Children(int nodeIndex) => _children[nodeIndex];

    internal static ChartRelationshipIndex Sankey(IEnumerable<ChartNode> nodes, IEnumerable<ChartFlowLink> links) {
        var snapshot = SnapshotNodes(nodes, out var indexes);
        if (links == null) throw new ArgumentNullException(nameof(links));
        var flows = links.ToArray();
        if (flows.Length == 0) throw new ArgumentException("Sankey charts require at least one link.", nameof(links));
        var sources = new int[flows.Length];
        var targets = new int[flows.Length];
        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < flows.Length; i++) {
            var link = flows[i];
            if (string.IsNullOrWhiteSpace(link.Id) || !ids.Add(link.Id)) throw new ArgumentException("Sankey link IDs must be non-empty and unique.", nameof(links));
            ValidateWeight(link.Value);
            (sources[i], targets[i]) = Resolve(indexes, link.SourceId, link.TargetId);
        }
        TopologicalOrder(snapshot.Length, sources, targets, false, out _, out var depths);
        return new ChartRelationshipIndex(snapshot, flows, Array.Empty<ChartTreeLink>(), indexes, sources, targets, -1, Array.Empty<double>(), depths: depths);
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
            (sources[i], targets[i]) = Resolve(indexes, link.ParentId, link.ChildId);
            if (!children.Add(link.ChildId)) throw new ArgumentException("Hierarchy child IDs can only have one incoming link.", nameof(links));
        }
        var order = TopologicalOrder(snapshot.Length, sources, targets, true, out var root, out var depths);
        var values = aggregateLeaves ? new double[snapshot.Length] : Array.Empty<double>();
        if (aggregateLeaves) {
            for (var i = 0; i < branches.Length; i++) values[targets[i]] = branches[i].Value;
            AggregateLeaves(values, sources, targets, order, nameof(links));
        }
        return new ChartRelationshipIndex(snapshot, Array.Empty<ChartFlowLink>(), branches, indexes, sources, targets, root, values, depths: depths);
    }

    private static ChartNode[] SnapshotNodes(IEnumerable<ChartNode> nodes, out Dictionary<string, int> indexes, string parameter = "nodes") {
        if (nodes == null) throw new ArgumentNullException(parameter);
        var snapshot = nodes.ToArray();
        if (snapshot.Length == 0) throw new ArgumentException("Relationship charts require explicit nodes.", parameter);
        indexes = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < snapshot.Length; i++) {
            var node = snapshot[i];
            if (string.IsNullOrWhiteSpace(node.Id) || string.IsNullOrWhiteSpace(node.Label)) throw new ArgumentException("Nodes require a non-empty ID and label.", parameter);
            if (indexes.ContainsKey(node.Id)) throw new ArgumentException("Node IDs must be unique.", parameter);
            indexes.Add(node.Id, i);
        }
        return snapshot;
    }

    private static void ValidateWeight(double value) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0) throw new ArgumentException("Relationship weights must be finite and positive.", "links");
    }

    private static (int Source, int Target) Resolve(Dictionary<string, int> nodes, string source, string target, string parameter = "links") {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target) || !nodes.TryGetValue(source, out var from) || !nodes.TryGetValue(target, out var to))
            throw new ArgumentException("Relationships must reference existing node IDs.", parameter);
        if (from == to) throw new ArgumentException("Relationships must connect distinct nodes.", parameter);
        return (from, to);
    }

    private static int[] TopologicalOrder(int count, int[] sources, int[] targets, bool hierarchy, out int root, out int[] depths, bool allowForest = false, string parameter = "links") {
        var outgoing = ChildrenFor(count, sources, targets);
        var incoming = new int[count];
        for (var i = 0; i < targets.Length; i++) incoming[targets[i]]++;
        var queue = new Queue<int>();
        for (var i = 0; i < count; i++) if (incoming[i] == 0) queue.Enqueue(i);
        root = queue.Count == 1 ? queue.Peek() : -1;
        if (hierarchy && !allowForest && root < 0) throw new ArgumentException("Hierarchy charts require exactly one root node.", parameter);
        var order = new int[count];
        depths = new int[count];
        var visited = 0;
        while (queue.Count > 0) {
            var source = queue.Dequeue();
            order[visited++] = source;
            foreach (var target in outgoing[source]) {
                depths[target] = Math.Max(depths[target], depths[source] + 1);
                if (hierarchy && depths[target] > 512) throw new ArgumentException("Hierarchy depth exceeds the supported limit of 512.", parameter);
                if (--incoming[target] == 0) queue.Enqueue(target);
            }
        }
        if (visited != count) throw new ArgumentException("Relationships must form an acyclic graph; hierarchy nodes must connect to their roots.", parameter);
        return order;
    }

    private static List<int>[] ChildrenFor(int count, int[] sources, int[] targets) {
        var children = new List<int>[count];
        for (var i = 0; i < count; i++) children[i] = new List<int>();
        for (var i = 0; i < sources.Length; i++) children[sources[i]].Add(targets[i]);
        return children;
    }

    private static void AggregateLeaves(double[] values, int[] sources, int[] targets, int[] order, string parameter) {
        var children = ChildrenFor(values.Length, sources, targets);
        for (var i = order.Length - 1; i >= 0; i--) {
            var node = order[i];
            if (children[node].Count == 0) continue;
            var total = 0d;
            foreach (var child in children[node]) total += values[child];
            if (double.IsInfinity(total)) throw new ArgumentException("Hierarchy leaf aggregates must remain finite.", parameter);
            values[node] = total;
        }
    }
}
