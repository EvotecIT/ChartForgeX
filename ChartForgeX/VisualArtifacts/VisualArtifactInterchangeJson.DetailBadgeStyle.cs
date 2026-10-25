using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Topology;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void DetailBadgeStyle(VisualArtifactInterchangeJsonWriter writer, TopologyDetailBadgeStyle? style) {
        if (style == null) return;
        writer.Property("badgeStyle"); writer.StartObject();
        OptionalString(writer, "fill", PathColor(style.Fill)); OptionalString(writer, "stroke", PathColor(style.Stroke));
        Number(writer, "strokeWidth", style.StrokeWidth); Number(writer, "cornerRadius", style.CornerRadius);
        Number(writer, "horizontalPadding", style.HorizontalPadding); Number(writer, "minimumHeight", style.MinimumHeight); Number(writer, "gap", style.Gap);
        writer.EndObject();
    }
    private static TopologyDetailBadgeStyle? ReadDetailBadgeStyle(Dictionary<string, GeoJsonValue> root) {
        var item = OptionalObject(root, "badgeStyle");
        return item == null ? null : new TopologyDetailBadgeStyle {
            Fill = ReadPathColor(item, "fill"), Stroke = ReadPathColor(item, "stroke"),
            StrokeWidth = RequiredNumber(item, "strokeWidth"), CornerRadius = RequiredNumber(item, "cornerRadius"),
            HorizontalPadding = RequiredNumber(item, "horizontalPadding"), MinimumHeight = RequiredNumber(item, "minimumHeight"), Gap = RequiredNumber(item, "gap")
        };
    }
}
