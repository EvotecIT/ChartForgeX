using System.IO;
using ChartForgeX.Core;
using ChartForgeX.Svg;

namespace ChartForgeX.Rendering;

internal static partial class VisualSceneSvgRenderer {
    private static void WriteChartFillDefinition(SvgMarkupWriter writer, VisualSceneGradient node,
        string prefix, int index, VisualSvgOptions? options) {
        var fill = node.AuthoredFill!;
        var id = GradientId(prefix, index);
        if (fill.ImageData is { } image) {
            var width = fill.Kind == ChartFillKind.Pattern ? fill.TileWidth / node.FillBounds.Width : 1;
            var height = fill.Kind == ChartFillKind.Pattern ? fill.TileHeight / node.FillBounds.Height : 1;
            writer.StartElement("pattern").Attribute("id", id).Attribute("patternUnits", "objectBoundingBox")
                .Attribute("patternContentUnits", "objectBoundingBox").Attribute("x", 0).Attribute("y", 0)
                .Attribute("width", width).Attribute("height", height).EndStartElement();
            writer.StartElement("image").Attribute("x", 0).Attribute("y", 0).Attribute("width", width).Attribute("height", height)
                .Attribute("preserveAspectRatio", "none").Attribute("href", image.DataUri).EndEmptyElement();
        } else if (fill.Kind == ChartFillKind.RadialGradient) {
            writer.StartElement("radialGradient").Attribute("id", id).Attribute("gradientUnits", "objectBoundingBox")
                .Attribute("cx", fill.Start.X).Attribute("cy", fill.Start.Y).Attribute("r", fill.Radius).EndStartElement();
            WriteGradientStops(writer, node.Stops, options);
        } else {
            writer.StartElement("linearGradient").Attribute("id", id).Attribute("gradientUnits", "objectBoundingBox")
                .Attribute("x1", fill.Start.X).Attribute("y1", fill.Start.Y)
                .Attribute("x2", fill.End.X).Attribute("y2", fill.End.Y).EndStartElement();
            WriteGradientStops(writer, node.Stops, options);
        }
        writer.EndElement();
    }

    private static void WriteChartFillIdentity(BinaryWriter writer, VisualSceneGradient node) {
        if (node.AuthoredFill is not { } fill) return;
        writer.Write("authored-chart-fill"); writer.Write((int)fill.Kind); writer.Write(fill.Radius);
        writer.Write(fill.TileWidth); writer.Write(fill.TileHeight); Rectangle(writer, node.FillBounds);
        if (fill.ImageData is { } image) {
            writer.Write(image.Image.Width); writer.Write(image.Image.Height); writer.Write(image.Image.Pixels);
        }
    }
}
