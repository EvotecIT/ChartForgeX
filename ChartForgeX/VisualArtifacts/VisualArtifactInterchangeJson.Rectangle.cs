using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void RectangleValues(VisualArtifactInterchangeJsonWriter writer, ChartRect value) {
        Number(writer, "x", value.X); Number(writer, "y", value.Y); Number(writer, "width", value.Width); Number(writer, "height", value.Height);
    }
    private static void Rectangle(VisualArtifactInterchangeJsonWriter writer, string name, ChartRect? value) {
        if (value == null) return; writer.Property(name); writer.StartObject(); RectangleValues(writer, value.Value); writer.EndObject();
    }
    private static ChartRect ReadRectangle(Dictionary<string, GeoJsonValue> value) =>
        new(RequiredNumber(value, "x"), RequiredNumber(value, "y"), RequiredNumber(value, "width"), RequiredNumber(value, "height"));
    private static ChartRect? OptionalRectangle(Dictionary<string, GeoJsonValue> owner, string name) =>
        OptionalObject(owner, name) is {} value ? ReadRectangle(value) : (ChartRect?)null;
}
