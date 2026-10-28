using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Topology;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void TopologyHierarchy(VisualArtifactInterchangeJsonWriter writer, VisualArtifactInterchangeTopologyHierarchy? hierarchy) {
        if (hierarchy == null) return;
        writer.Property("hierarchy"); writer.StartObject();
        OptionalString(writer, "sourceId", hierarchy.SourceId); Number(writer, "sourcePadding", hierarchy.SourcePadding);
        OptionalEnum(writer, "layoutDirection", hierarchy.LayoutDirection);
        HierarchyIds(writer, "collapsedNodeIds", hierarchy.CollapsedNodeIds); HierarchyIds(writer, "nodeOrder", hierarchy.NodeOrder);
        writer.Property("sourceNodes"); writer.StartArray();
        foreach (var state in hierarchy.SourceNodes) {
            writer.StartObject(); HierarchyIdentity(writer, state);
            Boolean(writer, "hasPositionOverride", state.HasPositionOverride); Boolean(writer, "preserveDisplayModeSize", state.PreserveDisplayModeSize);
            OptionalEnum(writer, "displayMode", state.DisplayMode); Boolean(writer, "showStatusBadge", state.ShowStatusBadge);
            HierarchyIds(writer, "portIds", state.PortIds); writer.EndObject();
        }
        writer.EndArray(); writer.Property("sourceGroups"); writer.StartArray();
        foreach (var state in hierarchy.SourceGroups) {
            writer.StartObject(); HierarchyIdentity(writer, state); Boolean(writer, "hasPositionOverride", state.HasPositionOverride); writer.EndObject();
        }
        writer.EndArray(); HierarchyIdentities(writer, "sourceEdges", hierarchy.SourceEdges);
        HierarchyIdentities(writer, "sourceScenarios", hierarchy.SourceScenarios); HierarchyIdentities(writer, "sourceAnnotations", hierarchy.SourceAnnotations);
        if (hierarchy.SourceAlignments.Count > 0) HierarchyIdentities(writer, "sourceAlignments", hierarchy.SourceAlignments);
        // One writer owns the parent and raw source: no reset of value, character or depth budgets.
        writer.Property("source"); WriteEnvelope(writer, hierarchy.Source); writer.EndObject();
    }
    private static void HierarchyIdentity(VisualArtifactInterchangeJsonWriter writer, VisualArtifactInterchangeTopologySourceIdentity state) {
        String(writer, "id", state.Id); String(writer, "sourceId", state.SourceId); OptionalString(writer, "cssClass", state.CssClass);
    }
    private static void HierarchyIdentities(VisualArtifactInterchangeJsonWriter writer, string name, IEnumerable<VisualArtifactInterchangeTopologySourceIdentity> states) {
        writer.Property(name); writer.StartArray(); foreach (var state in states) { writer.StartObject(); HierarchyIdentity(writer, state); writer.EndObject(); } writer.EndArray();
    }
    private static void HierarchyIds(VisualArtifactInterchangeJsonWriter writer, string name, IEnumerable<string> ids) {
        writer.Property(name); writer.StartArray(); foreach (var id in ids) writer.String(id); writer.EndArray();
    }
    private static VisualArtifactInterchangeTopologyHierarchy? ReadTopologyHierarchy(Dictionary<string, GeoJsonValue> root) {
        var item = OptionalObject(root, "hierarchy"); if (item == null) return null;
        var source = OptionalObject(item, "source") ?? throw new ArgumentException("A retained hierarchy requires its raw source.");
        // Reject recursively nested/cyclic source shapes before calling the shared envelope materializer.
        var sourceTopology = OptionalObject(source, "topology");
        if (sourceTopology != null && sourceTopology.ContainsKey("hierarchy")) throw new ArgumentException("A raw hierarchy source cannot contain another hierarchy source.");
        var hierarchy = new VisualArtifactInterchangeTopologyHierarchy {
            Source = ReadEnvelope(source), SourceId = OptionalString(item, "sourceId"), SourcePadding = RequiredNumber(item, "sourcePadding"),
            LayoutDirection = OptionalEnum<TopologyLayoutDirection>(item, "layoutDirection")
        };
        ReadHierarchyIds(item, "collapsedNodeIds", hierarchy.CollapsedNodeIds); ReadHierarchyIds(item, "nodeOrder", hierarchy.NodeOrder);
        foreach (var value in OptionalArray(item, "sourceNodes")) {
            var state = value.AsObject("raw source node");
            var node = new VisualArtifactInterchangeTopologySourceNode {
                Id = RequiredString(state, "id"), SourceId = RequiredString(state, "sourceId"), CssClass = OptionalString(state, "cssClass"),
                HasPositionOverride = OptionalBool(state, "hasPositionOverride") ?? false,
                PreserveDisplayModeSize = OptionalBool(state, "preserveDisplayModeSize") ?? false,
                DisplayMode = OptionalEnum<TopologyNodeDisplayMode>(state, "displayMode"), ShowStatusBadge = OptionalBool(state, "showStatusBadge") ?? true
            };
            ReadHierarchyIds(state, "portIds", node.PortIds); hierarchy.SourceNodes.Add(node);
        }
        foreach (var value in OptionalArray(item, "sourceGroups")) {
            var state = value.AsObject("raw source group"); hierarchy.SourceGroups.Add(new VisualArtifactInterchangeTopologySourceGroup {
                Id = RequiredString(state, "id"), SourceId = RequiredString(state, "sourceId"), CssClass = OptionalString(state, "cssClass"),
                HasPositionOverride = OptionalBool(state, "hasPositionOverride") ?? false });
        }
        ReadHierarchyIdentities(item, "sourceEdges", hierarchy.SourceEdges); ReadHierarchyIdentities(item, "sourceScenarios", hierarchy.SourceScenarios);
        ReadHierarchyIdentities(item, "sourceAnnotations", hierarchy.SourceAnnotations);
        ReadHierarchyIdentities(item, "sourceAlignments", hierarchy.SourceAlignments); return hierarchy;
    }
    private static void ReadHierarchyIdentities(Dictionary<string, GeoJsonValue> root, string name, ICollection<VisualArtifactInterchangeTopologySourceIdentity> target) {
        foreach (var value in OptionalArray(root, name)) { var item = value.AsObject("raw source identity"); target.Add(new VisualArtifactInterchangeTopologySourceIdentity {
            Id = RequiredString(item, "id"), SourceId = RequiredString(item, "sourceId"), CssClass = OptionalString(item, "cssClass") }); }
    }
    private static void ReadHierarchyIds(Dictionary<string, GeoJsonValue> root, string name, ICollection<string> target) {
        foreach (var value in OptionalArray(root, name)) target.Add(value.AsString(name));
    }
}
