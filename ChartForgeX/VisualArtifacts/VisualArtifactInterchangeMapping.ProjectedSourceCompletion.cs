using System;
using System.Collections.Generic;

namespace ChartForgeX.VisualArtifacts;

public static partial class VisualArtifactInterchangeMapping {
    // Native preparation retains typed provenance only. Optional alias admission needs the complete portable envelope,
    // including resolved routes, and must not make native Compile or semantic-region wrapping serialize wire data.
    internal static VisualArtifactInterchangeEnvelope CompleteProjectedSourceIds(VisualArtifactInterchangeEnvelope source) {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (!Pending(source) && !(source.Topology?.Hierarchy is {} hierarchy && Pending(hierarchy.Source))) return source;
        var copy = VisualArtifactInterchangeSnapshot.Capture(source);
        var candidates = new List<ProjectedSourceId>();
        Gather(copy);
        if (copy.Topology?.Hierarchy is {} captured) Gather(captured.Source);
        // Every pending flag is already cleared on the detached copy, so the existing exact full-envelope
        // sizing helper can Serialize it without recursion and retain its normal optional-alias fallback.
        AddProjectedSourceIds(copy, candidates);
        return copy;

        static bool Pending(VisualArtifactInterchangeEnvelope item) {
            foreach (var node in item.Nodes) if (node.PendingProjectedSourceId != null) return true;
            return false;
        }
        void Gather(VisualArtifactInterchangeEnvelope item) {
            foreach (var node in item.Nodes) if (node.PendingProjectedSourceId is string original) {
                node.PendingProjectedSourceId = null;
                candidates.Add(new ProjectedSourceId(node.Extensions, original));
            }
        }
    }
}
