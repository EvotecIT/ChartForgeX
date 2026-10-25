using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Topology;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void TopologyAlignments(VisualArtifactInterchangeJsonWriter writer, IReadOnlyList<VisualArtifactInterchangeTopologyAlignment> declarations) {
        if (declarations.Count == 0) return;
        writer.Property("alignments"); writer.StartArray();
        foreach (var declaration in declarations) {
            writer.StartObject(); String(writer, "id", declaration.Id); String(writer, "kind", declaration.Kind.ToString());
            writer.Property("nodeIds"); writer.StartArray(); foreach (var id in declaration.NodeIds) writer.String(id); writer.EndArray();
            Extensions(writer, declaration.Extensions); writer.EndObject();
        }
        writer.EndArray();
    }
    private static void ReadTopologyAlignments(Dictionary<string, GeoJsonValue> root, ICollection<VisualArtifactInterchangeTopologyAlignment> target) {
        var declarations = OptionalArray(root, "alignments");
        VisualArtifactInterchangeValidation.CheckAlignmentCount(declarations.Count);
        long members = 0;
        foreach (var value in declarations) {
            var item = value.AsObject("topology alignment"); var ids = OptionalArray(item, "nodeIds");
            members += ids.Count; VisualArtifactInterchangeValidation.CheckAlignmentMembers(ids.Count, members);
            var declaration = new VisualArtifactInterchangeTopologyAlignment { Id = RequiredString(item, "id"), Kind = RequiredEnum<TopologyAlignmentKind>(item, "kind") };
            foreach (var id in ids) declaration.NodeIds.Add(id.AsString("alignment node ID"));
            ReadExtensions(item, declaration.Extensions); target.Add(declaration);
        }
    }
}
