using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void ViewportPadding(VisualArtifactInterchangeJsonWriter writer, ChartPadding? padding) {
        if (!padding.HasValue) return;
        var value = padding.Value;
        writer.Property("viewportPadding"); writer.StartObject();
        Number(writer, "left", value.Left); Number(writer, "top", value.Top);
        Number(writer, "right", value.Right); Number(writer, "bottom", value.Bottom);
        writer.EndObject();
    }
    private static ChartPadding? ReadViewportPadding(Dictionary<string, GeoJsonValue> topology) {
        var value = OptionalObject(topology, "viewportPadding");
        return value == null ? (ChartPadding?)null : new ChartPadding(RequiredNumber(value, "left"), RequiredNumber(value, "top"),
            RequiredNumber(value, "right"), RequiredNumber(value, "bottom"));
    }
}
