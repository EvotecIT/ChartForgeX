using System;
using System.Collections.Generic;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeValidation {
    internal static void CheckAlignmentCount(int count) => Count(count, MaximumAnnotations, "topology alignments");
    internal static void CheckAlignmentMembers(int count, long total) {
        Count(count, MaximumNodes, "alignment members");
        if (total > MaximumJsonValues) throw new ArgumentOutOfRangeException(nameof(total), total, "Alignment members exceed the shared JSON value budget.");
    }
    private static void ValidateTopologyAlignments(VisualArtifactInterchangeEnvelope envelope, ISet<string> nodeIds, ISet<string> entityIds) {
        if (envelope.Topology == null) return;
        var declarations = envelope.Topology.Alignments; CheckAlignmentCount(declarations.Count);
        long total = 0;
        foreach (var declaration in declarations) {
            if (declaration == null) throw new ArgumentException("Topology alignment declarations cannot be null.", nameof(envelope));
            total += declaration.NodeIds.Count; CheckAlignmentMembers(declaration.NodeIds.Count, total);
            RequiredId(declaration.Id, "alignment id"); UniqueEntityId(entityIds, declaration.Id, "alignment", envelope);
            Defined(declaration.Kind, "alignment kind", envelope);
            if (declaration.NodeIds.Count < 2) throw new ArgumentException("Topology alignments require at least two distinct existing nodes.", nameof(envelope));
            var members = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in declaration.NodeIds) {
                RequiredId(id, "alignment member id");
                if (!nodeIds.Contains(id) || !members.Add(id)) throw new ArgumentException("Topology alignment members must be distinct existing nodes.", nameof(envelope));
            }
            Extensions(declaration.Extensions, "alignment extensions");
        }
    }
}
