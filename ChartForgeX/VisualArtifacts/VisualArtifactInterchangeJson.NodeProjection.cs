using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void PreparedNodeProjection(VisualArtifactInterchangeJsonWriter writer, TopologyPreparedNodeProjection? projection) {
        if (projection == null) return;
        writer.Property("projection"); writer.StartObject();
        Number(writer, "offsetX", projection.Offset.X); Number(writer, "offsetY", projection.Offset.Y);
        Number(writer, "bodyX", projection.BodyOrigin.X); Number(writer, "bodyY", projection.BodyOrigin.Y);
        writer.EndObject();
    }
    private static TopologyPreparedNodeProjection? ReadPreparedNodeProjection(Dictionary<string, GeoJsonValue> basis) {
        var projection = OptionalObject(basis, "projection");
        return projection == null ? null : new TopologyPreparedNodeProjection(
            new ChartPoint(RequiredNumber(projection, "offsetX"), RequiredNumber(projection, "offsetY")),
            new ChartPoint(RequiredNumber(projection, "bodyX"), RequiredNumber(projection, "bodyY")));
    }
}
