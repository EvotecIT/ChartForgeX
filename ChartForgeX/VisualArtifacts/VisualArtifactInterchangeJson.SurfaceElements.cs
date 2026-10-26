using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Topology;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void SurfaceElements(VisualArtifactInterchangeJsonWriter writer, TopologySurfaceElementAppearance? elements) {
        if (elements == null) return;
        writer.Property("surfaceElements"); writer.StartObject();
        String(writer, "shape", elements.Shape.ToString()); OptionalEnum(writer, "displayMode", elements.DisplayMode);
        Number(writer, "opacity", elements.Opacity); OptionalNumber(writer, "groupOpacity", elements.GroupOpacity);
        writer.EndObject();
    }
    private static TopologySurfaceElementAppearance? ReadSurfaceElements(Dictionary<string, GeoJsonValue> node) {
        var item = OptionalObject(node, "surfaceElements"); if (item == null) return null;
        foreach (var key in item.Keys) if (key is not ("shape" or "displayMode" or "opacity" or "groupOpacity"))
            throw new ArgumentException("Unknown captured surface-element field.");
        return new TopologySurfaceElementAppearance(RequiredEnum<TopologyNodeShape>(item, "shape"),
            OptionalEnum<TopologyNodeDisplayMode>(item, "displayMode"), RequiredNumber(item, "opacity"), OptionalNumber(item, "groupOpacity"));
    }
}
