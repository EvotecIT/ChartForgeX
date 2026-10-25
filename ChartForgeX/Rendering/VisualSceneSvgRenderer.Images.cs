using ChartForgeX.Svg;
using ChartForgeX.Primitives;
using System.Globalization;

namespace ChartForgeX.Rendering;

internal static partial class VisualSceneSvgRenderer {
    private static void WriteImageResource(SvgMarkupWriter writer, VisualSceneImageResource resource, string prefix, int index) {
        var bounds = resource.Bounds;
        writer.StartElement("image").Attribute("x", bounds.X).Attribute("y", bounds.Y)
            .Attribute("width", bounds.Width).Attribute("height", bounds.Height)
            .Attribute("href", resource.Href).Attribute("preserveAspectRatio", resource.PreserveAspectRatio);
        if (resource.Opacity != 1) writer.Attribute("opacity", resource.Opacity);
        Semantics(writer, resource, prefix, index); writer.EndEmptyElement();
    }

    private static void WriteImageAlphaBounds(SvgMarkupWriter writer, VisualSceneImage image) {
        var bounds = image.AlphaBounds ?? new ChartRect(0, 0, 0, 0);
        string Number(double value) => value.ToString("0", CultureInfo.InvariantCulture);
        // Closed private native image fact: source pixel-cell x/y/width/height and complete source dimensions.
        writer.Attribute("data-cfx-image-alpha-bounds", Number(bounds.X) + " " + Number(bounds.Y) + " " + Number(bounds.Width) + " "
            + Number(bounds.Height) + " " + Number(image.Image.Width) + " " + Number(image.Image.Height));
    }

    private static void SkipImageFallback(VisualScene scene, ref int index) {
        var depth = 1;
        while (++index < scene.Nodes.Count) {
            if (scene.Nodes[index] is VisualSceneGroup) depth++;
            else if (scene.Nodes[index] is VisualSceneEndGroup && --depth == 0) return;
        }
        throw new System.InvalidOperationException("The image resource fallback group was not closed.");
    }
}
