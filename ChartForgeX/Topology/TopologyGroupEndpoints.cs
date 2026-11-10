using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ChartForgeX.Topology;

/// <summary>Resolves routing-only boundary geometry without adding semantic or visible nodes.</summary>
internal static class TopologyGroupEndpoints {
    private static readonly ConditionalWeakTable<TopologyChart, SharedEndpoints> Shared = new();

    /// <summary>
    /// The endpoints for read-only routing queries. Routing asks once per edge for every other edge, so the map is kept
    /// per chart and rebuilt only when a node or group, or the group geometry copied into it, changes.
    /// </summary>
    internal static IReadOnlyDictionary<string, TopologyNode> ResolveShared(TopologyChart chart) {
        var shared = Shared.GetOrCreateValue(chart);
        lock (shared) {
            if (shared.Endpoints == null || !shared.Matches(chart)) shared.Capture(chart, Resolve(chart));
            return shared.Endpoints!;
        }
    }

    internal static Dictionary<string, TopologyNode> Resolve(TopologyChart chart) {
        var endpoints = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        foreach (var group in chart.Groups) {
            if (endpoints.ContainsKey(group.Id)) continue;
            endpoints.Add(group.Id, new TopologyNode {
                Id = group.Id, Label = group.Label, GroupId = group.Id,
                X = group.X, Y = group.Y, Width = Math.Max(1, group.Width), Height = Math.Max(1, group.Height),
                Shape = TopologyNodeShape.Rectangle, ShowStatusBadge = false
            });
        }
        return endpoints;
    }

    /// <summary>Paints ancestors before descendants even when the caller supplied groups in another order.</summary>
    internal static IEnumerable<TopologyGroup> InPaintOrder(TopologyChart chart) {
        var groups = chart.Groups.ToDictionary(group => group.Id, StringComparer.Ordinal);
        return chart.Groups.OrderBy(Depth);

        int Depth(TopologyGroup group) {
            var depth = 0;
            var seen = new HashSet<string>(StringComparer.Ordinal) { group.Id };
            while (group.ParentGroupId != null && seen.Add(group.ParentGroupId) && groups.TryGetValue(group.ParentGroupId, out var parent)) {
                depth++; group = parent;
            }
            return depth;
        }
    }

    /// <summary>The last resolved map and exactly what it was built from.</summary>
    private sealed class SharedEndpoints {
        private TopologyNode[] _nodes = Array.Empty<TopologyNode>();
        private string[] _nodeIds = Array.Empty<string>();
        private TopologyGroup[] _groups = Array.Empty<TopologyGroup>();
        private string?[] _groupFacts = Array.Empty<string?>();
        private double[] _groupGeometry = Array.Empty<double>();

        public IReadOnlyDictionary<string, TopologyNode>? Endpoints { get; private set; }

        public void Capture(TopologyChart chart, Dictionary<string, TopologyNode> endpoints) {
            _nodes = chart.Nodes.ToArray();
            _nodeIds = _nodes.Select(node => node.Id).ToArray();
            _groups = chart.Groups.ToArray();
            _groupFacts = new string?[_groups.Length * 2];
            _groupGeometry = new double[_groups.Length * 4];
            for (var i = 0; i < _groups.Length; i++) {
                _groupFacts[i * 2] = _groups[i].Id;
                _groupFacts[i * 2 + 1] = _groups[i].Label;
                _groupGeometry[i * 4] = _groups[i].X;
                _groupGeometry[i * 4 + 1] = _groups[i].Y;
                _groupGeometry[i * 4 + 2] = _groups[i].Width;
                _groupGeometry[i * 4 + 3] = _groups[i].Height;
            }

            Endpoints = endpoints;
        }

        // Identities are compared by reference and geometry exactly, so any change rebuilds the map.
        public bool Matches(TopologyChart chart) {
            if (chart.Nodes.Count != _nodes.Length || chart.Groups.Count != _groups.Length) return false;
            for (var i = 0; i < _nodes.Length; i++) {
                if (!ReferenceEquals(chart.Nodes[i], _nodes[i]) || !ReferenceEquals(_nodes[i].Id, _nodeIds[i])) return false;
            }

            for (var i = 0; i < _groups.Length; i++) {
                var group = chart.Groups[i];
                if (!ReferenceEquals(group, _groups[i]) || !ReferenceEquals(group.Id, _groupFacts[i * 2]) || !ReferenceEquals(group.Label, _groupFacts[i * 2 + 1])
                    || !group.X.Equals(_groupGeometry[i * 4]) || !group.Y.Equals(_groupGeometry[i * 4 + 1])
                    || !group.Width.Equals(_groupGeometry[i * 4 + 2]) || !group.Height.Equals(_groupGeometry[i * 4 + 3])) return false;
            }

            return true;
        }
    }
}
