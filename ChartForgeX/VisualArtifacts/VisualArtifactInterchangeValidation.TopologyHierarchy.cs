using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Topology;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeValidation {
    internal static void RejectNestedTopologyHierarchy(VisualArtifactInterchangeEnvelope envelope) {
        var hierarchy = envelope.Topology?.Hierarchy; if (hierarchy == null) return;
        if (hierarchy.Source == null || ReferenceEquals(hierarchy.Source, envelope) || hierarchy.Source.Topology?.Hierarchy != null)
            throw new ArgumentException("A hierarchy requires one raw authored source and cannot recursively contain hierarchy sources.", nameof(envelope));
    }
    private static void ValidateTopologyHierarchy(VisualArtifactInterchangeTopologyHierarchy? hierarchy, VisualArtifactInterchangeEnvelope envelope) {
        if (hierarchy == null) return;
        var source = hierarchy.Source;
        Count(source.Nodes.Count, MaximumNodes, "raw source nodes"); Count(source.Groups.Count, MaximumGroups, "raw source groups");
        Count(source.Edges.Count, MaximumEdges, "raw source edges"); Count(source.Scenarios.Count, MaximumScenarios, "raw source scenarios");
        Count(source.Annotations.Count, MaximumAnnotations, "raw source annotations");
        if (source.Family != VisualArtifactInterchangeFamily.Topology || source.Topology == null || source.Topology.LayoutMode != TopologyLayoutMode.Layered
            || source.Extensions.ContainsKey("chartforgex.geometry"))
            throw new ArgumentException("A hierarchy source must be a raw authored layered topology, not prepared scene geometry.", nameof(envelope));
        OptionalText(hierarchy.SourceId, "raw source chart id"); Finite(hierarchy.SourcePadding, "raw source padding");
        if (hierarchy.SourcePadding < 0) throw new ArgumentOutOfRangeException(nameof(envelope), "Raw source padding cannot be negative.");
        if (source.Topology!.ViewportPadding is ChartForgeX.Primitives.ChartPadding rawPadding &&
            (rawPadding.Left != hierarchy.SourcePadding || rawPadding.Top != hierarchy.SourcePadding ||
                rawPadding.Right != hierarchy.SourcePadding || rawPadding.Bottom != hierarchy.SourcePadding))
            throw new ArgumentException("A raw hierarchy source viewport padding must agree with its retained uniform authored padding.", nameof(envelope));
        if (hierarchy.LayoutDirection.HasValue) Defined(hierarchy.LayoutDirection.Value, "hierarchy direction", envelope);
        ValidateHierarchyIdentities(hierarchy.SourceNodes, source.Nodes.Select(node => node.Id), "nodes", envelope);
        ValidateHierarchyIdentities(hierarchy.SourceGroups, source.Groups.Select(group => group.Id), "groups", envelope);
        ValidateHierarchyIdentities(hierarchy.SourceEdges, source.Edges.Select(edge => edge.Id), "edges", envelope, allowRepeatedSourceIds: true);
        ValidateHierarchyIdentities(hierarchy.SourceScenarios, source.Scenarios.Select(scenario => scenario.Id), "scenarios", envelope);
        ValidateHierarchyIdentities(hierarchy.SourceAnnotations, source.Annotations.Select(annotation => annotation.Id), "annotations", envelope);
        CheckAlignmentCount(hierarchy.SourceAlignments.Count);
        ValidateHierarchyIdentities(hierarchy.SourceAlignments, source.Topology!.Alignments.Select(alignment => alignment.Id), "alignments", envelope);
        ValidateHierarchyPathIds(hierarchy, envelope);
        var hierarchyIds = new HashSet<string>(hierarchy.SourceNodes.Where((state, index) => source.Nodes[index].Extensions.ContainsKey("hierarchy.level"))
            .Select(state => state.SourceId), StringComparer.Ordinal);
        ValidateHierarchyIds(hierarchy.CollapsedNodeIds, hierarchyIds, "collapsed node", envelope);
        ValidateHierarchyIds(hierarchy.NodeOrder, hierarchyIds, "ordered node", envelope);
        for (var index = 0; index < hierarchy.SourceNodes.Count; index++) {
            var state = hierarchy.SourceNodes[index]; var node = source.Nodes[index];
            if (state.DisplayMode.HasValue) Defined(state.DisplayMode.Value, "raw source display mode", envelope);
            if (state.PortIds.Count != node.Ports.Count) throw new ArgumentException("Raw source port identities must cover every source port.", nameof(envelope));
            var ports = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in state.PortIds) { RequiredId(id, "raw port id"); if (!ports.Add(id)) throw new ArgumentException("Raw source port IDs must be unique within a node.", nameof(envelope)); }
        }
    }
    private static void ValidateHierarchyPathIds(VisualArtifactInterchangeTopologyHierarchy hierarchy, VisualArtifactInterchangeEnvelope envelope) {
        if (hierarchy.Source.Topology!.PathDecorations.Count == 0) return;
        var ids = new HashSet<string>(hierarchy.SourceNodes.Select(state => state.SourceId)
            .Concat(hierarchy.SourceGroups.Select(state => state.SourceId)).Concat(hierarchy.SourceEdges.Select(state => state.SourceId))
            .Concat(hierarchy.SourceAnnotations.Select(state => state.SourceId)).Concat(hierarchy.SourceAlignments.Select(state => state.SourceId)), StringComparer.Ordinal);
        if (hierarchy.SourceId != null) ids.Add(hierarchy.SourceId);
        foreach (var path in hierarchy.Source.Topology.PathDecorations) {
            if (path == null || !ids.Add(path.Id)) throw new ArgumentException("Raw hierarchy path decoration IDs collide with restored authored identities.", nameof(envelope));
        }
    }
    private static void ValidateHierarchyIdentities<T>(IReadOnlyList<T> states, IEnumerable<string> sourceIds, string name, VisualArtifactInterchangeEnvelope envelope, bool allowRepeatedSourceIds = false)
        where T : VisualArtifactInterchangeTopologySourceIdentity {
        var ids = sourceIds.ToArray();
        if (states.Count != ids.Length) throw new ArgumentException("Raw hierarchy state must cover all " + name + ".", nameof(envelope));
        var originals = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < states.Count; index++) {
            var state = states[index] ?? throw new ArgumentException("Raw hierarchy identity states cannot be null.", nameof(envelope));
            RequiredId(state.Id, "raw portable id"); RequiredId(state.SourceId, "raw source id"); OptionalText(state.CssClass, "raw source CSS class");
            if (state.Id != ids[index] || !originals.Add(state.SourceId) && !allowRepeatedSourceIds) throw new ArgumentException("Raw hierarchy " + name + " must retain complete, unique source identities in source order.", nameof(envelope));
        }
    }
    private static void ValidateHierarchyIds(IReadOnlyList<string> values, ISet<string> known, string name, VisualArtifactInterchangeEnvelope envelope) {
        Count(values.Count, MaximumNodes, "hierarchy " + name + " IDs"); var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in values) {
            RequiredId(value, name + " id"); if (!known.Contains(value) || !seen.Add(value)) throw new ArgumentException("Hierarchy " + name + " IDs must be unique existing source hierarchy IDs.", nameof(envelope));
        }
    }
}
