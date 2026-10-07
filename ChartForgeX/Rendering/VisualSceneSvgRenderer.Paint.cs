using System.Globalization;
using System.IO;
using ChartForgeX.Svg;

namespace ChartForgeX.Rendering;

internal static partial class VisualSceneSvgRenderer {
    private static void WriteGradientDefinition(SvgMarkupWriter writer, VisualSceneGradient gradient, string prefix, int index, VisualSvgOptions? options) {
        writer.StartElement("linearGradient").Attribute("id", GradientId(prefix, index)).Attribute("gradientUnits", "userSpaceOnUse")
            .Attribute("x1", gradient.Start.X).Attribute("y1", gradient.Start.Y).Attribute("x2", gradient.End.X).Attribute("y2", gradient.End.Y).EndStartElement();
        foreach (var stop in gradient.Stops) writer.StartElement("stop").Attribute("offset", stop.Offset).Attribute("stop-color", ResolvePaint(stop.Color, stop.Paint, options)).EndEmptyElement();
        writer.EndElement();
    }

    private static void WriteGradientShape(SvgMarkupWriter writer, VisualSceneGradient gradient, string prefix, int index, VisualSvgOptions? options) {
        var shape = gradient.Shape;
        if (shape is VisualSceneRectangle rect) writer.StartElement("rect").Attribute("x", rect.Bounds.X).Attribute("y", rect.Bounds.Y)
            .Attribute("width", rect.Bounds.Width).Attribute("height", rect.Bounds.Height).Attribute("rx", rect.Radius);
        else if (shape is VisualScenePath path) writer.StartElement("path").Attribute("d", PathData(path));
        else if (shape is VisualSceneSlice slice) writer.StartElement("path").Attribute("d", ChartSlicePathGeometry.BuildPath(slice.Cx, slice.Cy, slice.Outer, slice.Inner, slice.Start, slice.Start + slice.Sweep));
        else throw new System.NotSupportedException("Unsupported gradient shape.");
        Paint(writer, shape, prefix, index, "url(#" + GradientId(prefix, index) + ")", options);
        writer.EndEmptyElement();
    }

    private static string GradientId(string prefix, int index) => prefix + "-gradient-" + index.ToString(CultureInfo.InvariantCulture);

    private static void WritePathIdentity(BinaryWriter writer, VisualScenePath path) {
        writer.Write(path.Close); writer.Write(path.Commands.Count);
        writer.Write((int)path.Cap); writer.Write((int)path.Join); writer.Write(path.Dash?.Count ?? 0);
        if (path.Dash != null) foreach (var dash in path.Dash) writer.Write(dash);
        foreach (var command in path.Commands) {
            writer.Write((int)command.Kind); writer.Write(command.X); writer.Write(command.Y);
            writer.Write(command.Control1X); writer.Write(command.Control1Y); writer.Write(command.Control2X); writer.Write(command.Control2Y);
        }
    }

    private static void WriteGradientIdentity(BinaryWriter writer, VisualSceneGradient gradient) {
        var shape = gradient.Shape;
        Color(writer, shape.Fill); Color(writer, shape.Stroke); writer.Write(shape.StrokeWidth); PaintIdentity(writer, shape.Paint);
        if (shape is VisualSceneRectangle rect) { writer.Write(1); Rectangle(writer, rect.Bounds); writer.Write(rect.Radius); }
        else if (shape is VisualScenePath path) { writer.Write(2); WritePathIdentity(writer, path); }
        else if (shape is VisualSceneSlice slice) {
            writer.Write(3); writer.Write(slice.Cx); writer.Write(slice.Cy); writer.Write(slice.Outer);
            writer.Write(slice.Inner); writer.Write(slice.Start); writer.Write(slice.Sweep);
        } else throw new System.NotSupportedException("Unsupported gradient shape.");
        Point(writer, gradient.Start); Point(writer, gradient.End); writer.Write(gradient.Stops.Count);
        foreach (var stop in gradient.Stops) { writer.Write(stop.Offset); Color(writer, stop.Color); Text(writer, stop.Paint?.Value); }
    }
}
