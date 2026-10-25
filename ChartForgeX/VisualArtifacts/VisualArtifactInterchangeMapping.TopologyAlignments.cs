using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Topology;

namespace ChartForgeX.VisualArtifacts;

public static partial class VisualArtifactInterchangeMapping {
    private static void RestoreTopologyAlignments(TopologyChart chart, IEnumerable<VisualArtifactInterchangeTopologyAlignment> declarations,
        IReadOnlyDictionary<string, string>? nodeIds = null, IReadOnlyList<VisualArtifactInterchangeTopologySourceIdentity>? identities = null) {
        var index = 0;
        foreach (var source in declarations) {
            var native = new TopologyAlignment(identities == null ? source.Id : identities[index].SourceId, source.Kind,
                nodeIds == null ? source.NodeIds : source.NodeIds.Select(id => nodeIds[id]));
            RestoreTopologyMetadata(source.Extensions, native.Metadata); chart.Alignments.Add(native); index++;
        }
    }
}
