using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void ContentClip(VisualArtifactInterchangeJsonWriter writer, TopologyPreparedContentClip? value) {
        if (value == null) return;
        writer.Property("contentClip"); writer.StartObject(); Rectangle(writer, "plot", value.Plot); Rectangle(writer, "clip", value.Clip); writer.EndObject();
    }
    private static TopologyPreparedContentClip? ReadContentClip(Dictionary<string, GeoJsonValue> root) {
        var value = OptionalObject(root, "contentClip"); return value == null ? null : new TopologyPreparedContentClip(Box("plot"), Box("clip"));
        ChartRect Box(string name) { var box = OptionalObject(value!, name) ?? throw new System.ArgumentException("A captured content clip requires its original plot and final clip.");
            return ReadRectangle(box); }
    }
}
