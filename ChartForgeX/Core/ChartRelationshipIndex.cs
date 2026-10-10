using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Core;

// Authored order controls layout and ordinal styling; only explicit IDs identify relationships.
internal sealed partial class ChartRelationshipIndex {
    internal IReadOnlyList<ChartNode> Nodes { get; }
    internal IReadOnlyList<ChartFlowLink> FlowLinks { get; }
    internal IReadOnlyList<double> FlowIncomingValues { get; }
    internal IReadOnlyList<double> FlowOutgoingValues { get; }
    internal IReadOnlyList<double> FlowEndpointValues { get; }
    internal IReadOnlyList<ChartTreeLink> TreeLinks { get; }
    internal IReadOnlyList<ChartHierarchyItem> HierarchyItems { get; }
    internal IReadOnlyList<double> HierarchyValues { get; }
    internal IReadOnlyList<int> Depths { get; }
    internal IReadOnlyList<int> Roots { get; }
    internal int Root { get; }
    private readonly Dictionary<string, int> _nodeIndexes;
    private readonly HashSet<string> _flowIds;
    private readonly int[] _sources;
    private readonly int[] _targets;
    private readonly int[] _incomingLinks;
    private readonly int[] _parents;
    private readonly IReadOnlyList<int>[] _children;
    private readonly int[] _hierarchyOrder;

    private ChartRelationshipIndex(ChartNode[] nodes, ChartFlowLink[] flowLinks, ChartTreeLink[] treeLinks,
        Dictionary<string, int> nodeIndexes, int[] sources, int[] targets, int root, double[] hierarchyValues,
        double[]? flowIncomingValues = null, double[]? flowOutgoingValues = null, double[]? flowEndpointValues = null,
        ChartHierarchyItem[]? hierarchyItems = null, int[]? depths = null, int[]? hierarchyOrder = null, HashSet<string>? flowIds = null) {
        Nodes = Array.AsReadOnly(nodes);
        FlowLinks = Array.AsReadOnly(flowLinks);
        FlowIncomingValues = Array.AsReadOnly(flowIncomingValues ?? Array.Empty<double>());
        FlowOutgoingValues = Array.AsReadOnly(flowOutgoingValues ?? Array.Empty<double>());
        FlowEndpointValues = Array.AsReadOnly(flowEndpointValues ?? Array.Empty<double>());
        TreeLinks = Array.AsReadOnly(treeLinks);
        HierarchyItems = Array.AsReadOnly(hierarchyItems ?? Array.Empty<ChartHierarchyItem>());
        _hierarchyOrder = hierarchyOrder ?? Array.Empty<int>();
        HierarchyValues = Array.AsReadOnly(hierarchyValues);
        Depths = Array.AsReadOnly(depths ?? Array.Empty<int>());
        _nodeIndexes = nodeIndexes;
        _flowIds = flowIds ?? new HashSet<string>(StringComparer.Ordinal);
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
    internal bool ContainsFlow(string id) => _flowIds.Contains(id);
    internal int Parent(int nodeIndex) => _parents[nodeIndex];
    internal IReadOnlyList<int> Children(int nodeIndex) => _children[nodeIndex];

    internal static ChartRelationshipIndex Sankey(IEnumerable<ChartNode> nodes, IEnumerable<ChartFlowLink> links) => Flow(nodes, links, chord: false);

    internal static ChartRelationshipIndex Chord(IEnumerable<ChartNode> nodes, IEnumerable<ChartFlowLink> links) => Flow(nodes, links, chord: true);

    private static ChartRelationshipIndex Flow(IEnumerable<ChartNode> nodes, IEnumerable<ChartFlowLink> links, bool chord) {
        var snapshot = SnapshotNodes(nodes, out var indexes, allowEmpty: chord);
        if (links == null) throw new ArgumentNullException(nameof(links));
        var flows = links.ToArray();
        if (!chord && flows.Length == 0) throw new ArgumentException("Sankey charts require at least one link.", nameof(links));
        var sources = new int[flows.Length];
        var targets = new int[flows.Length];
        var incoming = new double[snapshot.Length];
        var outgoing = new double[snapshot.Length];
        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < flows.Length; i++) {
            var link = flows[i];
            if (string.IsNullOrWhiteSpace(link.Id) || !ids.Add(link.Id)) throw new ArgumentException("Flow link IDs must be non-empty and unique.", nameof(links));
            if (chord) {
                if (double.IsNaN(link.Value) || double.IsInfinity(link.Value) || link.Value < 0)
                    throw new ArgumentException("Chord flow weights must be finite and non-negative.", nameof(links));
            } else ValidateWeight(link.Value);
            (sources[i], targets[i]) = Resolve(indexes, link.SourceId, link.TargetId, allowSelf: chord);
            incoming[targets[i]] += link.Value;
            outgoing[sources[i]] += link.Value;
            if (double.IsInfinity(incoming[targets[i]]) || double.IsInfinity(outgoing[sources[i]]))
                throw new ArgumentException("Flow node aggregates must remain finite.", nameof(links));
        }
        var endpoints = new double[snapshot.Length];
        for (var i = 0; i < endpoints.Length; i++) {
            endpoints[i] = chord ? incoming[i] + outgoing[i] : Math.Max(incoming[i], outgoing[i]);
            if (double.IsInfinity(endpoints[i])) throw new ArgumentException("Chord incoming plus outgoing endpoint aggregates must remain finite.", nameof(links));
        }
        var depths = Array.Empty<int>();
        if (!chord) {
            for (var i = 0; i < snapshot.Length; i++)
                if (incoming[i] == 0 && outgoing[i] == 0)
                    throw new ArgumentException("Sankey nodes must be referenced by at least one flow.", nameof(nodes));
            TopologicalOrder(snapshot.Length, sources, targets, false, out _, out depths);
        }
        return new ChartRelationshipIndex(snapshot, flows, Array.Empty<ChartTreeLink>(), indexes, sources, targets, -1, Array.Empty<double>(),
            flowIncomingValues: incoming, flowOutgoingValues: outgoing, flowEndpointValues: endpoints, depths: depths, flowIds: ids);
    }

    internal static ChartRelationshipIndex Hierarchy(IEnumerable<ChartNode> nodes, IEnumerable<ChartTreeLink> links) {
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
        TopologicalOrder(snapshot.Length, sources, targets, true, out var root, out var depths);
        return new ChartRelationshipIndex(snapshot, Array.Empty<ChartFlowLink>(), branches, indexes, sources, targets, root, Array.Empty<double>(), depths: depths);
    }

    private static ChartNode[] SnapshotNodes(IEnumerable<ChartNode> nodes, out Dictionary<string, int> indexes, string parameter = "nodes", bool allowEmpty = false) {
        if (nodes == null) throw new ArgumentNullException(parameter);
        var snapshot = nodes.ToArray();
        if (!allowEmpty && snapshot.Length == 0) throw new ArgumentException("Relationship charts require explicit nodes.", parameter);
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

    private static (int Source, int Target) Resolve(Dictionary<string, int> nodes, string source, string target, string parameter = "links", bool allowSelf = false) {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target) || !nodes.TryGetValue(source, out var from) || !nodes.TryGetValue(target, out var to))
            throw new ArgumentException("Relationships must reference existing node IDs.", parameter);
        if (!allowSelf && from == to) throw new ArgumentException("Relationships must connect distinct nodes.", parameter);
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
