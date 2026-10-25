using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Topology;
using ChartForgeX.Primitives;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void LabelPosition(VisualArtifactInterchangeJsonWriter writer, TopologyLabelPosition? position) {
        if (position == null) return;
        writer.Property("labelPosition"); writer.StartObject(); Number(writer, "x", position.Point.X); Number(writer, "y", position.Point.Y);
        String(writer, "verticalAnchor", position.VerticalAnchor.ToString()); writer.EndObject();
    }
    private static TopologyLabelPosition? ReadLabelPosition(Dictionary<string, GeoJsonValue> root) {
        var item = OptionalObject(root, "labelPosition"); return item == null ? null : new TopologyLabelPosition(
            new ChartPoint(RequiredNumber(item, "x"), RequiredNumber(item, "y")), RequiredEnum<TopologyLabelVerticalAnchor>(item, "verticalAnchor"));
    }
    private static void MarkerStyle(VisualArtifactInterchangeJsonWriter writer, string name, TopologyMarkerStyle? style) {
        if (style == null) return;
        writer.Property(name); writer.StartObject(); Number(writer, "scale", style.Scale); Number(writer, "offset", style.Offset);
        OptionalNumber(writer, "strokeWidth", style.StrokeWidth); writer.EndObject();
    }
    private static TopologyMarkerStyle? ReadMarkerStyle(Dictionary<string, GeoJsonValue> root, string name) {
        var item = OptionalObject(root, name); return item == null ? null : new TopologyMarkerStyle {
            Scale = RequiredNumber(item, "scale"), Offset = RequiredNumber(item, "offset"), StrokeWidth = OptionalNumber(item, "strokeWidth")
        };
    }
}
