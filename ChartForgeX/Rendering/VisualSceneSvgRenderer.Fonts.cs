using ChartForgeX.Svg;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualSceneSvgRenderer {
    private static void WriteProductFonts(SvgMarkupWriter writer, VisualScene scene) {
        var css = ChartFontAssets.ForScene(scene);
        if (css.Length > 0) writer.StartElement("style").Text(css).EndElement();
    }
}
