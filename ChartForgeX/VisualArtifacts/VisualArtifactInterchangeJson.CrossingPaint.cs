using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Topology;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void CrossingPaint(VisualArtifactInterchangeJsonWriter writer, TopologyCrossingPaint? policy) {
        if (policy == null) return;
        writer.Property("crossingPaint"); writer.StartObject();
        String(writer, "style", policy.Style.ToString()); Number(writer, "radius", policy.Radius);
        if (policy.CurveHandling != TopologyCrossingCurveHandling.Reject) String(writer, "curveHandling", policy.CurveHandling.ToString());
        if (policy.PreparedUnits != 1) Number(writer, "preparedUnits", policy.PreparedUnits);
        writer.EndObject();
    }
    private static TopologyCrossingPaint? ReadCrossingPaint(Dictionary<string, GeoJsonValue> root) {
        var item = OptionalObject(root, "crossingPaint");
        return item == null ? null : new TopologyCrossingPaint(RequiredEnum<TopologyCrossingPaintStyle>(item, "style"), RequiredNumber(item, "radius"), OptionalEnum<TopologyCrossingCurveHandling>(item, "curveHandling") ?? TopologyCrossingCurveHandling.Reject, OptionalNumber(item, "preparedUnits") ?? 1);
    }
}
