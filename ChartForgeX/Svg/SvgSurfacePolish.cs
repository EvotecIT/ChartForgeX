using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

internal static class SvgSurfacePolish {
    internal static void WriteScopedStrokeStyle(SvgMarkupWriter writer, string id) {
        writer.StartElement("style")
            .Text("#" + id + " .cfx-guide-stroke,#" + id + " .cfx-premium-stroke{vector-effect:non-scaling-stroke;shape-rendering:geometricPrecision}")
            .EndElement()
            .Line();
    }

    internal static void WriteSurfaceGradient(SvgMarkupWriter writer, string id, string suffix, ChartColor color) {
        var top = ChartSurfacePolish.GradientTop(color);
        var bottom = ChartSurfacePolish.GradientBottom(color);
        writer.StartElement("linearGradient")
            .Attribute("id", id + "-" + suffix)
            .Attribute("x1", "0")
            .Attribute("x2", "0")
            .Attribute("y1", "0")
            .Attribute("y2", "1")
            .EndStartElement()
            .StartElement("stop")
            .Attribute("offset", "0%")
            .Attribute("stop-color", top.ToHex())
            .Attribute("stop-opacity", top.A / 255.0)
            .EndEmptyElement()
            .StartElement("stop")
            .Attribute("offset", "100%")
            .Attribute("stop-color", bottom.ToHex())
            .Attribute("stop-opacity", bottom.A / 255.0)
            .EndEmptyElement()
            .EndElement()
            .Line();
    }
}
