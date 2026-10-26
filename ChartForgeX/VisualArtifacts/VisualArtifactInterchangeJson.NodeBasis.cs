using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void PreparedNodeBasis(VisualArtifactInterchangeJsonWriter writer, TopologyPreparedNodeBasis? basis) {
        if (basis == null) return;
        writer.Property("preparedNodeBasis"); writer.StartObject();
        Number(writer, "factor", basis.Factor); Number(writer, "width", basis.Width); Number(writer, "height", basis.Height);
        OptionalEnum(writer, "shape", basis.Shape); String(writer, "displayMode", basis.DisplayMode.ToString()); String(writer, "kind", basis.Kind.ToString());
        OptionalEnum(writer, "iconShape", basis.IconShape); OptionalString(writer, "iconId", basis.IconId); OptionalString(writer, "symbol", basis.Symbol);
        Number(writer, "glyphAnchorX", basis.GlyphAnchor.X); Number(writer, "glyphAnchorY", basis.GlyphAnchor.Y);
        Number(writer, "originalX", basis.OriginalBody.X); Number(writer, "originalY", basis.OriginalBody.Y);
        Number(writer, "originalWidth", basis.OriginalBody.Width); Number(writer, "originalHeight", basis.OriginalBody.Height);
        OptionalNumber(writer, "contentPadding", basis.ContentPadding); OptionalNumber(writer, "maximumTextWidth", basis.MaximumTextWidth);
        OptionalNumber(writer, "cornerRadius", basis.CornerRadius); OptionalNumber(writer, "glyphScale", basis.GlyphScale);
        PreparedNodeProjection(writer, basis.Projection);
        writer.EndObject();
    }
    private static TopologyPreparedNodeBasis? ReadPreparedNodeBasis(Dictionary<string, GeoJsonValue> node) {
        var basis = OptionalObject(node, "preparedNodeBasis");
        return basis == null ? null : new TopologyPreparedNodeBasis(RequiredNumber(basis, "factor"), RequiredNumber(basis, "width"), RequiredNumber(basis, "height"),
            OptionalEnum<TopologyNodeShape>(basis, "shape"), RequiredEnum<TopologyNodeDisplayMode>(basis, "displayMode"), RequiredEnum<TopologyNodeKind>(basis, "kind"),
            OptionalEnum<TopologyIconShape>(basis, "iconShape"), OptionalString(basis, "iconId"), OptionalString(basis, "symbol"),
            RequiredNumber(basis, "glyphAnchorX"), RequiredNumber(basis, "glyphAnchorY"),
            new ChartRect(RequiredNumber(basis, "originalX"), RequiredNumber(basis, "originalY"), RequiredNumber(basis, "originalWidth"), RequiredNumber(basis, "originalHeight")),
            OptionalNumber(basis, "contentPadding"), OptionalNumber(basis, "maximumTextWidth"),
            OptionalNumber(basis, "cornerRadius"), OptionalNumber(basis, "glyphScale"), ReadPreparedNodeProjection(basis));
    }
}
